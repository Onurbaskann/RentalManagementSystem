using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services;
using KiraTakip.Services.Reservations;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class ReservationAvailabilityTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ReservationAvailabilityTests(DatabaseFixture fixture)
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
    public async Task Conflict_ShouldUseHalfOpenIntervalsAndAllowEditExclusion()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(
            seed.FirstUnitId,
            seed.FirstTenantId,
            ReservationStatus.Confirmed,
            new DateTime(2026, 8, 17, 9, 0, 0),
            new DateTime(2026, 8, 17, 10, 0, 0));
        var repository = new ReservationRepository(_context);

        Assert.False(await repository.IsConflictAsync(
            seed.FirstUnitId,
            new DateTime(2026, 8, 17, 10, 0, 0),
            new DateTime(2026, 8, 17, 11, 0, 0)));
        Assert.True(await repository.IsConflictAsync(
            seed.FirstUnitId,
            new DateTime(2026, 8, 17, 9, 30, 0),
            new DateTime(2026, 8, 17, 10, 30, 0)));
        Assert.False(await repository.IsConflictAsync(
            seed.FirstUnitId,
            reservation.StartDate,
            reservation.EndDate,
            reservation.Id));
    }

    [Fact]
    public async Task Conflict_ShouldOnlyBeBlockedByConfirmedReservations()
    {
        var seed = await SeedAsync();
        var repository = new ReservationRepository(_context);
        var statuses = new[]
        {
            ReservationStatus.PendingApproval,
            ReservationStatus.Completed,
            ReservationStatus.Cancelled,
            ReservationStatus.Rejected
        };

        foreach (var status in statuses)
        {
            var startDate = new DateTime(2026, 8, 18, 9, 0, 0).AddHours((int)status * 2);
            await AddReservationAsync(
                seed.FirstUnitId,
                seed.FirstTenantId,
                status,
                startDate,
                startDate.AddHours(1));

            Assert.False(await repository.IsConflictAsync(
                seed.FirstUnitId,
                startDate.AddMinutes(15),
                startDate.AddMinutes(45)));
        }
    }

    [Fact]
    public async Task Calendar_ShouldCombineScopeAndProtectTenantPrivacy()
    {
        var seed = await SeedAsync();
        var startDate = new DateTime(2026, 8, 19, 9, 0, 0);
        await AddReservationAsync(seed.FirstUnitId, seed.FirstTenantId, ReservationStatus.Confirmed, startDate, startDate.AddHours(1), "Gizli başlık");
        await AddReservationAsync(seed.SecondUnitId, seed.SecondTenantId, ReservationStatus.PendingApproval, startDate.AddHours(2), startDate.AddHours(3), "Diğer gizli başlık");
        await AddReservationAsync(seed.FirstUnitId, seed.FirstTenantId, ReservationStatus.Completed, startDate.AddHours(-2), startDate.AddHours(-1), "Tamamlanan rezervasyon");
        await AddReservationAsync(seed.OutsideUnitId, seed.SecondTenantId, ReservationStatus.Confirmed, startDate, startDate.AddHours(1));
        await AddReservationAsync(seed.FirstUnitId, seed.FirstTenantId, ReservationStatus.Cancelled, startDate.AddHours(4), startDate.AddHours(5));

        var repository = new ReservationRepository(_context);
        var query = new ReservationCalendarRepositoryQuery(
            new DateTime(2026, 8, 17),
            new DateTime(2026, 8, 24),
            null,
            [seed.FirstPropertyId],
            [seed.SecondUnitId]);

        var internalItems = await repository.GetCalendarItemsAsync(query);
        var tenantItems = await repository.GetTenantCalendarItemsAsync(seed.FirstTenantId, query);

        Assert.Equal(3, internalItems.Count);
        Assert.Contains(internalItems, item => item.UnitId == seed.FirstUnitId && item.Status == ReservationStatus.Confirmed);
        Assert.Contains(internalItems, item => item.UnitId == seed.SecondUnitId && item.Status == ReservationStatus.PendingApproval);
        Assert.Contains(internalItems, item => item.UnitId == seed.FirstUnitId && item.Status == ReservationStatus.Completed);
        Assert.DoesNotContain(internalItems, item => item.UnitId == seed.OutsideUnitId);
        Assert.Equal(3, tenantItems.Count);
        Assert.Contains(tenantItems, item => item.UnitId == seed.FirstUnitId && item.IsOwnedByCurrentTenant);
        Assert.Contains(tenantItems, item => item.UnitId == seed.SecondUnitId && !item.IsOwnedByCurrentTenant);
        Assert.Contains(tenantItems, item => item.UnitId == seed.FirstUnitId && item.Status == ReservationStatus.Completed);
        Assert.Null(typeof(TenantReservationCalendarItemDto).GetProperty("Title"));
        Assert.Null(typeof(TenantReservationCalendarItemDto).GetProperty("TenantDisplayName"));
        Assert.Null(typeof(TenantReservationCalendarItemDto).GetProperty("Description"));
    }

    [Fact]
    public async Task AvailabilityService_ShouldSeparatePolicyConflictAndScopeResults()
    {
        var seed = await SeedAsync();
        var startDate = new DateTime(2026, 8, 20, 9, 0, 0);
        var reservation = await AddReservationAsync(
            seed.FirstUnitId,
            seed.FirstTenantId,
            ReservationStatus.Confirmed,
            startDate,
            startDate.AddHours(1));
        var service = new ReservationService(
            new ReservationRepository(_context),
            null!,
            null!,
            null!,
            new UnitRepository(_context),
            null!,
            ReservationBusinessRulesTestFactory.Create(),
            null!,
            new KiraTakip.Infrastructure.Persistence.EfCoreConcurrencyViolationDetector());
        var scope = new ReservationAccessScopeInput([seed.FirstPropertyId], []);

        var policyResult = await service.CheckAvailabilityAsync(
            new CheckReservationAvailabilityInput(
                seed.FirstUnitId,
                startDate.AddHours(2),
                startDate.AddHours(2).AddMinutes(5),
                null,
                scope));
        var conflictResult = await service.CheckAvailabilityAsync(
            new CheckReservationAvailabilityInput(
                seed.FirstUnitId,
                startDate.AddMinutes(15),
                startDate.AddMinutes(45),
                null,
                scope));
        var editResult = await service.CheckAvailabilityAsync(
            new CheckReservationAvailabilityInput(
                seed.FirstUnitId,
                startDate,
                startDate.AddHours(1),
                reservation.Id,
                scope));

        Assert.False(policyResult.IsAvailable);
        Assert.Equal("RESERVATION_DURATION_TOO_SHORT", policyResult.Code);
        Assert.False(conflictResult.IsAvailable);
        Assert.Equal("RESERVATION_TIME_CONFLICT", conflictResult.Code);
        Assert.True(editResult.IsAvailable);
        Assert.Equal("RESERVATION_AVAILABLE", editResult.Code);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CheckAvailabilityAsync(new CheckReservationAvailabilityInput(
                seed.OutsideUnitId,
                startDate,
                startDate.AddHours(1),
                null,
                scope)));
        Assert.Equal(ErrorType.Forbidden, exception.ErrorType);
        Assert.Equal("RESERVATION_OUT_OF_SCOPE", exception.Code);
    }

    private async Task<AvailabilitySeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstProperty = new Property { Name = $"Takvim A {suffix}" };
        var secondProperty = new Property { Name = $"Takvim B {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Salon {suffix}",
            Code = $"SAL-{suffix}",
            Usage = UnitTypeUsage.Reservable
        };
        var firstTenant = new Tenant { TenantNo = $"TA-{suffix}", Name = $"Kiracı A {suffix}" };
        var secondTenant = new Tenant { TenantNo = $"TB-{suffix}", Name = $"Kiracı B {suffix}" };
        _context.AddRange(firstProperty, secondProperty, unitType, firstTenant, secondTenant);
        await _context.SaveChangesAsync();

        var firstUnit = CreateUnit(firstProperty.Id, unitType.Id, $"A1 {suffix}");
        var secondUnit = CreateUnit(secondProperty.Id, unitType.Id, $"B1 {suffix}");
        var outsideUnit = CreateUnit(secondProperty.Id, unitType.Id, $"B2 {suffix}");
        _context.AddRange(firstUnit, secondUnit, outsideUnit);
        await _context.SaveChangesAsync();

        return new AvailabilitySeed(
            firstProperty.Id,
            firstUnit.Id,
            secondUnit.Id,
            outsideUnit.Id,
            firstTenant.Id,
            secondTenant.Id);
    }

    private async Task<Reservation> AddReservationAsync(
        int unitId,
        int tenantId,
        ReservationStatus status,
        DateTime startDate,
        DateTime endDate,
        string? title = null)
    {
        var reservation = new Reservation
        {
            UnitId = unitId,
            TenantId = tenantId,
            StartDate = startDate,
            EndDate = endDate,
            TotalDurationMinutes = (int)(endDate - startDate).TotalMinutes,
            Status = status,
            Title = title
        };
        _context.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    private static Unit CreateUnit(int propertyId, int unitTypeId, string name)
        => new()
        {
            PropertyId = propertyId,
            UnitTypeId = unitTypeId,
            Name = name,
            Area = 20m
        };

    private sealed record AvailabilitySeed(
        int FirstPropertyId,
        int FirstUnitId,
        int SecondUnitId,
        int OutsideUnitId,
        int FirstTenantId,
        int SecondTenantId);
}
