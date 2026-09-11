using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class ReservationCompletionTests : IDisposable
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 13, 7, 0, 0, TimeSpan.Zero);
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ReservationCompletionTests(DatabaseFixture fixture)
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
    public async Task Candidates_ShouldRespectGraceBoundaryStatusAndBatchSize()
    {
        var seed = await SeedAsync();
        var beforeBoundary = await AddReservationAsync(seed, ReservationStatus.Confirmed, new DateTime(2026, 8, 13, 9, 46, 0));
        var exactBoundary = await AddReservationAsync(seed, ReservationStatus.Confirmed, new DateTime(2026, 8, 13, 9, 45, 0));
        var afterGrace = await AddReservationAsync(seed, ReservationStatus.Confirmed, new DateTime(2026, 8, 13, 9, 44, 0));
        await AddReservationAsync(seed, ReservationStatus.PendingApproval, new DateTime(2026, 8, 13, 9, 0, 0));
        await AddReservationAsync(seed, ReservationStatus.Cancelled, new DateTime(2026, 8, 13, 9, 0, 0));
        await AddReservationAsync(seed, ReservationStatus.Rejected, new DateTime(2026, 8, 13, 9, 0, 0));
        var service = CreateService();

        var candidates = await service.FindCandidatesAsync(
            new FindReservationCompletionCandidatesInput(10));
        var firstCandidate = await service.FindCandidatesAsync(
            new FindReservationCompletionCandidatesInput(1));

        Assert.DoesNotContain(beforeBoundary.Id, candidates);
        Assert.Contains(exactBoundary.Id, candidates);
        Assert.Contains(afterGrace.Id, candidates);
        Assert.Equal([afterGrace.Id], firstCandidate);
    }

    [Fact]
    public async Task Completion_ShouldPersistTimestampAndBeIdempotent()
    {
        var seed = await SeedAsync();
        var reservation = await AddReservationAsync(
            seed,
            ReservationStatus.Confirmed,
            new DateTime(2026, 8, 13, 9, 45, 0));
        var service = CreateService();

        Assert.True(await service.CompleteAsync(new CompleteReservationInput(reservation.Id)));
        Assert.Equal(ReservationStatus.Completed, reservation.Status);
        Assert.Equal(new DateTime(2026, 8, 13, 10, 0, 0), reservation.CompletedAt);
        Assert.Equal("system", reservation.UpdatedBy);
        Assert.NotNull(reservation.UpdatedAt);
        Assert.False(await service.CompleteAsync(new CompleteReservationInput(reservation.Id)));
    }

    private ReservationCompletionService CreateService()
        => new(
            new ReservationRepository(_context),
            ReservationBusinessRulesTestFactory.Create(UtcNow),
            ReservationBusinessRulesTestFactory.CreatePolicyProvider(
                new ReservationPolicySettings { CompletionGraceMinutes = 15 }),
            new UnitOfWork(_context));

    private async Task<(int UnitId, int TenantId)> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Tamamlama {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Tamamlama Salonu {suffix}",
            Code = $"TS-{suffix}",
            Usage = UnitTypeUsage.Reservable
        };
        var tenant = new Tenant { TenantNo = $"TC-{suffix}", Name = $"Tamamlama Kiracısı {suffix}" };
        _context.AddRange(property, unitType, tenant);
        await _context.SaveChangesAsync();
        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Salon {suffix}",
            Area = 20m
        };
        _context.Add(unit);
        await _context.SaveChangesAsync();
        return (unit.Id, tenant.Id);
    }

    private async Task<Reservation> AddReservationAsync(
        (int UnitId, int TenantId) seed,
        ReservationStatus status,
        DateTime endDate)
    {
        var reservation = new Reservation
        {
            UnitId = seed.UnitId,
            TenantId = seed.TenantId,
            StartDate = endDate.AddHours(-1),
            EndDate = endDate,
            TotalDurationMinutes = 60,
            Status = status,
            Title = "Tamamlama testi"
        };
        _context.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }
}

[Collection("Database collection")]
public class ReservationCompletionConcurrencyTests(DatabaseFixture fixture)
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 13, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MultipleInstances_ShouldCompleteReservationOnlyOnce()
    {
        int reservationId;
        int propertyId;
        int unitTypeId;
        int unitId;
        int tenantId;
        await using (var setup = fixture.CreateContext())
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var property = new Property { Name = $"Çoklu Tamamlama {suffix}" };
            var unitType = new UnitType
            {
                Name = $"Çoklu Salon {suffix}",
                Code = $"CS-{suffix}",
                Usage = UnitTypeUsage.Reservable
            };
            var tenant = new Tenant { TenantNo = $"CT-{suffix}", Name = $"Çoklu Kiracı {suffix}" };
            setup.AddRange(property, unitType, tenant);
            await setup.SaveChangesAsync();
            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitTypeId = unitType.Id,
                Name = $"Salon {suffix}",
                Area = 20m
            };
            setup.Add(unit);
            await setup.SaveChangesAsync();
            var reservation = new Reservation
            {
                UnitId = unit.Id,
                TenantId = tenant.Id,
                StartDate = new DateTime(2026, 8, 13, 8, 0, 0),
                EndDate = new DateTime(2026, 8, 13, 9, 45, 0),
                TotalDurationMinutes = 105,
                Status = ReservationStatus.Confirmed,
                Title = "Çoklu instance testi"
            };
            setup.Add(reservation);
            await setup.SaveChangesAsync();
            reservationId = reservation.Id;
            propertyId = property.Id;
            unitTypeId = unitType.Id;
            unitId = unit.Id;
            tenantId = tenant.Id;
        }

        try
        {
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = CompleteAsync(start.Task);
            var second = CompleteAsync(start.Task);
            start.SetResult();
            var results = await Task.WhenAll(first, second);

            Assert.Single(results, completed => completed);
            Assert.Single(results, completed => !completed);
        }
        finally
        {
            await using var cleanup = fixture.CreateContext();
            cleanup.Reservations.Remove(await cleanup.Reservations.SingleAsync(item => item.Id == reservationId));
            await cleanup.SaveChangesAsync();
            cleanup.Units.Remove(await cleanup.Units.SingleAsync(item => item.Id == unitId));
            await cleanup.SaveChangesAsync();
            cleanup.Tenants.Remove(await cleanup.Tenants.SingleAsync(item => item.Id == tenantId));
            cleanup.UnitTypes.Remove(await cleanup.UnitTypes.SingleAsync(item => item.Id == unitTypeId));
            cleanup.Properties.Remove(await cleanup.Properties.SingleAsync(item => item.Id == propertyId));
            await cleanup.SaveChangesAsync();
        }

        async Task<bool> CompleteAsync(Task start)
        {
            await start;
            await using var context = fixture.CreateContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var service = new ReservationCompletionService(
                new ReservationRepository(context),
                ReservationBusinessRulesTestFactory.Create(UtcNow),
                ReservationBusinessRulesTestFactory.CreatePolicyProvider(
                    new ReservationPolicySettings { CompletionGraceMinutes = 15 }),
                new UnitOfWork(context));
            var completed = await service.CompleteAsync(new CompleteReservationInput(reservationId));
            await transaction.CommitAsync();
            return completed;
        }
    }
}
