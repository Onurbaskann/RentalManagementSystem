using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Reservation;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Services.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class SpaceReservationRuleTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public SpaceReservationRuleTests(DatabaseFixture fixture)
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
    public async Task SaveRateRule_ShouldRejectTargetSpaceWithExistingRule_AndToggleOutsideScope()
    {
        var seed = await SeedAsync();
        var service = CreateReservationService();
        var globalScope = new SpaceAccessScope(true, []);

        var duplicateException = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.SaveRateRuleAsync(new SaveReservationRateRuleInput(
                seed.SecondRuleId,
                seed.FirstSpace.Id,
                seed.ChargeTypeId,
                0,
                60,
                100,
                20,
                null,
                true,
                globalScope)));
        Assert.Equal(nameof(SaveReservationRateRuleInput.SpaceId), duplicateException.Field);
        Assert.Equal("Bu alan için zaten rezervasyon kuralı mevcut.", duplicateException.Message);
        _context.ChangeTracker.Clear();
        var unchanged = await _context.Set<ReservationRateOverride>().AsNoTracking().SingleAsync(r => r.Id == seed.SecondRuleId);
        Assert.NotEqual(seed.FirstSpace.Id, unchanged.SpaceId);

        var scopeException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ToggleRateRuleStatusAsync(new ToggleRateRuleStatusInput(
                seed.SecondRuleId,
                new SpaceAccessScope(false, [seed.FirstSpace.Path]))));
        Assert.Equal(ErrorType.Forbidden, scopeException.ErrorType);
        Assert.Equal("RESERVATION_RATE_SPACE_FORBIDDEN", scopeException.Code);
    }

    [Fact]
    public async Task ToggleRateRule_ShouldGuardMissingRule()
    {
        var service = CreateReservationService();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ToggleRateRuleStatusAsync(new ToggleRateRuleStatusInput(
                int.MaxValue,
                new SpaceAccessScope(true, []))));

        Assert.Equal(ErrorType.NotFound, exception.ErrorType);
    }

    private ReservationService CreateReservationService()
    {
        return new(new ReservationRepository(_context),
                   new ReservationRateOverrideRepository(_context),
                   null!,
                   new ChargeTypeRepository(_context),
                   null!,
                   ReservationBusinessRulesTestFactory.Create(),
                   new UnitOfWork(_context),
                   new KiraTakip.Infrastructure.Persistence.EfCoreConcurrencyViolationDetector());
    }

    private async Task<RuleSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var chargeType = new ChargeType
        {
            Name = $"Alan Kural Borç Tipi {suffix}",
            Code = $"SRT_{suffix}",
            Behavior = ChargeTypeBehavior.ReservationSpecific
        };
        _context.ChargeTypes.Add(chargeType);
        await _context.SaveChangesAsync();

        var root = await SpaceTestSeed.AddRootAsync(_context, $"Kural Kök {suffix}");
        var first = await SpaceTestSeed.AddChildAsync(_context, root, $"Birinci Alan {suffix}", 10);
        var second = await SpaceTestSeed.AddChildAsync(_context, root, $"İkinci Alan {suffix}", 10);
        var firstRule = NewRule(first.Id, chargeType.Id);
        var secondRule = NewRule(second.Id, chargeType.Id);
        _context.AddRange(firstRule, secondRule);
        await _context.SaveChangesAsync();

        return new RuleSeed(chargeType.Id, first, secondRule.Id);
    }

    private static ReservationRateOverride NewRule(int spaceId, int chargeTypeId) => new()
    {
        SpaceId = spaceId,
        ChargeTypeId = chargeTypeId,
        FreeDurationMinutes = 0,
        BillingPeriodMinutes = 60,
        PeriodRate = 100,
        KdvRate = 20
    };

    private sealed record RuleSeed(int ChargeTypeId, Space FirstSpace, int SecondRuleId);
}
