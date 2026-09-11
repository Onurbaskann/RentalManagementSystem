using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services;
using KiraTakip.Services.Reservations;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class ReservationConcurrentDecisionTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task ConflictingPendingRequests_ShouldNotBothBeApprovedConcurrently()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        int propertyId;
        int unitTypeId;
        int unitId;
        int tenantId;
        int firstReservationId;
        int secondReservationId;
        string actorId;
        byte[] firstVersion;
        byte[] secondVersion;

        await using (var setup = fixture.CreateContext())
        {
            var property = new Property { Name = $"Paralel Karar {suffix}" };
            var unitType = new UnitType
            {
                Name = $"Paralel Salon {suffix}",
                Code = $"PK-{suffix}",
                Usage = UnitTypeUsage.Reservable
            };
            var tenant = new Tenant { TenantNo = $"PK-{suffix}", Name = $"Paralel Kiracı {suffix}" };
            setup.AddRange(property, unitType, tenant);
            await setup.SaveChangesAsync();
            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitTypeId = unitType.Id,
                Name = $"Toplantı Salonu {suffix}",
                Area = 20m
            };
            setup.Add(unit);
            await setup.SaveChangesAsync();
            var first = CreatePending(unit.Id, tenant.Id);
            var second = CreatePending(unit.Id, tenant.Id);
            setup.AddRange(first, second);
            await setup.SaveChangesAsync();

            propertyId = property.Id;
            unitTypeId = unitType.Id;
            unitId = unit.Id;
            tenantId = tenant.Id;
            firstReservationId = first.Id;
            secondReservationId = second.Id;
            firstVersion = first.RowVersion.ToArray();
            secondVersion = second.RowVersion.ToArray();
            actorId = await setup.Users.Select(user => user.Id).FirstAsync();
        }

        try
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstTask = ApproveAsync(firstReservationId, firstVersion, ready.Task);
            var secondTask = ApproveAsync(secondReservationId, secondVersion, ready.Task);
            ready.SetResult();
            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Single(results, result => result == "approved");
            Assert.Single(results, result => result == "RESERVATION_APPROVAL_TIME_CONFLICT");

            await using var verify = fixture.CreateContext();
            Assert.Equal(1, await verify.Reservations.CountAsync(reservation =>
                (reservation.Id == firstReservationId || reservation.Id == secondReservationId)
                && reservation.Status == ReservationStatus.Confirmed));
        }
        finally
        {
            await using var cleanup = fixture.CreateContext();
            cleanup.Reservations.RemoveRange(cleanup.Reservations.Where(reservation =>
                reservation.Id == firstReservationId || reservation.Id == secondReservationId));
            await cleanup.SaveChangesAsync();
            cleanup.Units.Remove(await cleanup.Units.SingleAsync(unit => unit.Id == unitId));
            await cleanup.SaveChangesAsync();
            cleanup.Tenants.Remove(await cleanup.Tenants.SingleAsync(tenant => tenant.Id == tenantId));
            cleanup.UnitTypes.Remove(await cleanup.UnitTypes.SingleAsync(unitType => unitType.Id == unitTypeId));
            cleanup.Properties.Remove(await cleanup.Properties.SingleAsync(property => property.Id == propertyId));
            await cleanup.SaveChangesAsync();
        }

        async Task<string> ApproveAsync(int reservationId, byte[] rowVersion, Task start)
        {
            await start;
            await using var context = fixture.CreateContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var service = new ReservationService(
                new ReservationRepository(context),
                null!, null!, null!, null!, null!,
                ReservationBusinessRulesTestFactory.Create(),
                new UnitOfWork(context),
                new KiraTakip.Infrastructure.Persistence.EfCoreConcurrencyViolationDetector());
            try
            {
                await service.ApproveAsync(new ApproveReservationInput(
                    reservationId,
                    rowVersion,
                    actorId,
                    new ReservationAccessScopeInput([propertyId], [])));
                await transaction.CommitAsync();
                return "approved";
            }
            catch (BusinessException exception)
            {
                await transaction.RollbackAsync();
                return exception.Code ?? "business-error";
            }
        }
    }

    private static Reservation CreatePending(int unitId, int tenantId)
        => new()
        {
            UnitId = unitId,
            TenantId = tenantId,
            StartDate = new DateTime(2026, 10, 1, 9, 0, 0),
            EndDate = new DateTime(2026, 10, 1, 10, 0, 0),
            TotalDurationMinutes = 60,
            Status = ReservationStatus.PendingApproval,
            Title = "Paralel karar testi"
        };
}
