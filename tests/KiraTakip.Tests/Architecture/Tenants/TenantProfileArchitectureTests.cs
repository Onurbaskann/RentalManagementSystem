using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services;
using KiraTakip.Services.Interfaces;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Tenants;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Tenant;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class TenantProfileArchitectureTests : IDisposable
{
    private readonly TenantCurrentUserContext _currentUser = new();
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public TenantProfileArchitectureTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext(_currentUser);
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task Profile_ShouldReturnCurrentTenant()
    {
        var seed = await SeedAsync();
        _currentUser.TenantId = seed.FirstTenantId;
        var service = CreateService(_context);

        var profile = await service.GetProfileAsync(
            new GetTenantProfileInput(seed.FirstTenantId));

        Assert.Equal(seed.FirstTenantId, profile.Id);
        Assert.Equal(seed.FirstTenantName, profile.Name);
    }

    [Fact]
    public async Task Profile_ShouldHideAnotherTenantThroughGlobalFilter()
    {
        var seed = await SeedAsync();
        _currentUser.TenantId = seed.FirstTenantId;
        var service = CreateService(_context);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetProfileAsync(new GetTenantProfileInput(seed.SecondTenantId)));

        Assert.Equal("Tenant.ProfileNotFound", exception.Code);
        Assert.Equal(ErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Profile_ShouldGuardMissingTenant()
    {
        var seed = await SeedAsync();
        _currentUser.TenantId = seed.FirstTenantId;
        var service = CreateService(_context);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetProfileAsync(new GetTenantProfileInput(int.MaxValue)));

        Assert.Equal("Tenant.ProfileNotFound", exception.Code);
        Assert.Equal(ErrorType.NotFound, exception.ErrorType);
    }

    private static TenantService CreateService(ApplicationDbContext context)
    {
        return new(new TenantRepository(context),
                   null!,
                   null!,
                   null!);
    }

    private async Task<TenantProfileSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstTenant = new Tenant
        {
            TenantNo = $"TPR1-{suffix}",
            Name = $"Birinci Profil Kiracısı {suffix}"
        };
        var secondTenant = new Tenant
        {
            TenantNo = $"TPR2-{suffix}",
            Name = $"İkinci Profil Kiracısı {suffix}"
        };

        _context.Tenants.AddRange(firstTenant, secondTenant);
        await _context.SaveChangesAsync();

        return new TenantProfileSeed(
            firstTenant.Id,
            firstTenant.Name,
            secondTenant.Id);
    }

    private sealed class TenantCurrentUserContext : ICurrentUserContext
    {
        public int? TenantId { get; set; }
        public string? UserId => "tenant-profile-test-user";
        public UserType? UserType => KiraTakip.Models.Enums.UserType.Tenant;
        public bool IsKiraciUser => true;
    }

    private sealed record TenantProfileSeed(
        int FirstTenantId,
        string FirstTenantName,
        int SecondTenantId);
}
