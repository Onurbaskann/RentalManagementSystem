using KiraTakip.Domain.Financial;

namespace KiraTakip.Domain.Reservations;

public static class ReservationRatePolicy
{
    public static bool IsValid(
        int freeDurationMinutes,
        int billingPeriodMinutes,
        decimal periodRate,
        decimal kdvRate)
        => freeDurationMinutes >= 0
            && billingPeriodMinutes > 0
            && periodRate >= 0
            && VatPolicy.IsValidRate(kdvRate);
}
