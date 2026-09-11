using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Reservations;

namespace KiraTakip.Tests;

internal static class ReservationBusinessRulesTestFactory
{
    public static IReservationBusinessRules Create(
        DateTimeOffset? utcNow = null,
        ReservationPolicySettings? settings = null)
        => new ReservationBusinessRules(
            CreatePolicyProvider(settings),
            new FixedTimeProvider(utcNow ?? new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero)));

    public static IReservationPolicyProvider CreatePolicyProvider(
        ReservationPolicySettings? settings = null)
        => new StaticReservationPolicyProvider(settings ?? new ReservationPolicySettings());

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StaticReservationPolicyProvider(ReservationPolicySettings settings)
        : IReservationPolicyProvider
    {
        public ReservationPolicySettings Current { get; } = settings;

        public Task RefreshAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
