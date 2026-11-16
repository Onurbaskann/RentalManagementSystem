using KiraTakip.Data;
using KiraTakip.Infrastructure.Persistence;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Leases;
using KiraTakip.Services.Leases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class LeaseExpirationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public LeaseExpirationServiceTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task Expire_ShouldEndOnlyActiveLeasesPastEndDate()
    {
        var seed = await SeedAsync();
        var today = DateTime.Today;
        var expired = await AddLeaseAsync(seed, LeaseStatus.Active, today.AddDays(-1));
        var endsToday = await AddLeaseAsync(seed, LeaseStatus.Active, today);
        var terminated = await AddLeaseAsync(seed, LeaseStatus.Terminated, today.AddDays(-10));
        var draft = await AddLeaseAsync(seed, LeaseStatus.Draft, today.AddDays(-10));
        var service = CreateService();

        var candidates = await service.FindCandidatesAsync(1000);
        Assert.Contains(expired.Id, candidates);
        Assert.DoesNotContain(endsToday.Id, candidates);
        Assert.DoesNotContain(terminated.Id, candidates);
        Assert.DoesNotContain(draft.Id, candidates);

        Assert.True(await service.ExpireAsync(expired.Id));
        Assert.False(await service.ExpireAsync(expired.Id));
        Assert.False(await service.ExpireAsync(endsToday.Id));
        Assert.False(await service.ExpireAsync(terminated.Id));
        Assert.False(await service.ExpireAsync(draft.Id));

        await _context.Entry(expired).ReloadAsync();
        Assert.Equal(LeaseStatus.Ended, expired.Status);
        var log = Assert.Single(await _context.SozlesmeIslemGecmisleri
            .Where(activity => activity.LeaseId == expired.Id)
            .ToListAsync());
        Assert.Equal(LeaseActivityType.Expiration, log.ActivityType);
        Assert.Equal(LeaseStatus.Active, (await _context.Leases.FindAsync(endsToday.Id))!.Status);
        Assert.Equal(LeaseStatus.Terminated, (await _context.Leases.FindAsync(terminated.Id))!.Status);
        Assert.Equal(LeaseStatus.Draft, (await _context.Leases.FindAsync(draft.Id))!.Status);
    }

    private LeaseExpirationService CreateService()
        => new(
            new LeaseRepository(_context),
            new UnitOfWork(_context),
            new EfCoreConcurrencyViolationDetector());

    private async Task<Lease> AddLeaseAsync(
        (int UnitId, int TenantId) seed,
        LeaseStatus status,
        DateTime endDate)
    {
        var lease = new Lease
        {
            UnitId = seed.UnitId,
            TenantId = seed.TenantId,
            LeaseNo = $"LE-{Guid.NewGuid():N}"[..20],
            Status = status,
            StartDate = endDate.AddMonths(-6),
            EndDate = endDate
        };
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();
        return lease;
    }

    private async Task<(int UnitId, int TenantId)> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Sona Erme {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Sona Erme Tipi {suffix}",
            Code = $"SE_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { TenantNo = $"SE-{suffix}", Name = $"Sona Erme Kiracı {suffix}" };
        _context.AddRange(property, unitType, tenant);
        await _context.SaveChangesAsync();
        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Sona Erme Birimi {suffix}",
            Area = 50
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return (unit.Id, tenant.Id);
    }
}
