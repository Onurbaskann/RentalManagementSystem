using KiraTakip.Authorization;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.Controllers;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class TenantReservationArchitectureTests : IDisposable
{
    private static readonly DateTime CurrentTime = new(2026, 7, 27, 12, 0, 0);
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public TenantReservationArchitectureTests(DatabaseFixture fixture)
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
    public void Controller_ShouldRequireTenantId()
    {
        var attributes = typeof(TenantReservationController)
            .GetCustomAttributes(typeof(RequireKiraciIdAttribute), inherit: true);

        Assert.Single(attributes);
    }

    [Fact]
    public async Task TenantList_ShouldApplyExplicitTenantScope()
    {
        var seed = await SeedAsync();
        var repository = new ReservationRepository(_context);

        var reservations = await repository.GetTenantListAsync(
            seed.FirstTenantId);

        var reservation = Assert.Single(reservations);
        Assert.Equal(seed.FirstTenantId, reservation.TenantId);
        Assert.Equal(seed.FirstReservationId, reservation.Id);
    }

    [Fact]
    public async Task TenantList_ShouldNotDeriveCompletedStatusFromCurrentTime()
    {
        var seed = await SeedAsync();
        var repository = new ReservationRepository(_context);

        var reservations = await repository.GetTenantListAsync(
            seed.FirstTenantId);

        Assert.Equal(ReservationStatus.Confirmed, Assert.Single(reservations).Status);

        var storedStatus = await _context.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Id == seed.FirstReservationId)
            .Select(reservation => reservation.Status)
            .SingleAsync();
        Assert.Equal(ReservationStatus.Confirmed, storedStatus);
    }

    [Fact]
    public async Task Service_ShouldGuardMissingTenant()
    {
        var service = new ReservationService(
            new ReservationRepository(_context),
            null!,
            null!,
            null!,
            null!,
            new TenantRepository(_context),
            ReservationBusinessRulesTestFactory.Create(),
            null!,
            new KiraTakip.Infrastructure.Persistence.EfCoreConcurrencyViolationDetector());

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.GetTenantReservationsAsync(
                new GetTenantReservationsInput(
                    int.MaxValue,
                    new ReservationAccessScopeInput())));

        Assert.Equal("TENANT_RESERVATION_TENANT_NOT_FOUND", exception.Code);
        Assert.Equal(ErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task TenantList_ShouldHonorEmptyUnitScope()
    {
        var seed = await SeedAsync();
        var unitId = await _context.Reservations
            .Where(reservation => reservation.Id == seed.FirstReservationId)
            .Select(reservation => reservation.UnitId)
            .SingleAsync();
        var directUnitReservations = await new ReservationRepository(_context).GetTenantListAsync(
            seed.FirstTenantId,
            [],
            [unitId]);
        Assert.Single(directUnitReservations);

        var reservations = await new ReservationRepository(_context).GetTenantListAsync(
            seed.FirstTenantId,
            [],
            []);

        Assert.Empty(reservations);
    }
    private async Task<TenantReservationSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstTenant = new Tenant
        {
            TenantNo = $"TR1-{suffix}",
            Name = $"Birinci Rezervasyon Kiracısı {suffix}"
        };
        var secondTenant = new Tenant
        {
            TenantNo = $"TR2-{suffix}",
            Name = $"İkinci Rezervasyon Kiracısı {suffix}"
        };
        var property = new Property { Name = $"Rezervasyon Taşınmazı {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Rezervasyon Birim Türü {suffix}",
            Code = $"TR_{suffix}",
            Usage = UnitTypeUsage.Reservable
        };

        _context.AddRange(firstTenant, secondTenant, property, unitType);
        await _context.SaveChangesAsync();

        var firstUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Birinci Birim {suffix}",
            Area = 10
        };
        var secondUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"İkinci Birim {suffix}",
            Area = 10
        };
        _context.AddRange(firstUnit, secondUnit);
        await _context.SaveChangesAsync();

        var firstReservation = CreateReservation(
            firstUnit.Id,
            firstTenant.Id,
            CurrentTime.AddHours(-2),
            CurrentTime.AddHours(-1));
        var secondReservation = CreateReservation(
            secondUnit.Id,
            secondTenant.Id,
            CurrentTime.AddHours(1),
            CurrentTime.AddHours(2));
        _context.AddRange(firstReservation, secondReservation);
        await _context.SaveChangesAsync();

        return new TenantReservationSeed(
            firstTenant.Id,
            firstReservation.Id);
    }

    private static Reservation CreateReservation(
        int unitId,
        int tenantId,
        DateTime startDate,
        DateTime endDate)
        => new()
        {
            UnitId = unitId,
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            TotalDurationMinutes = 60,
            PaidDurationMinutes = 60,
            UnitRate = 100,
            RateAmount = 100,
            TotalAmount = 100,
            Status = ReservationStatus.Confirmed
        };

    private sealed record TenantReservationSeed(
        int FirstTenantId,
        int FirstReservationId);
}
