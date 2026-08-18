namespace KiraTakip.Models.Settings;

public sealed class OperationalPolicySettings
{
    public int PaymentLinkValidityHours { get; init; } = 168;
    public int PaymentReminderDaysBefore { get; init; } = 5;
    public int PaymentReminderCooldownDays { get; init; } = 7;
    public int InvitationValidityDays { get; init; } = 7;
    public int InvitationResendCooldownMinutes { get; init; } = 60;
    public int LeaseExpiringSoonStatusDays { get; init; } = 30;
    public int DashboardExpiringLeaseLookaheadDays { get; init; } = 60;
    public int BankMatchingAmountTolerancePercent { get; init; } = 2;
    public int BankMatchingDateToleranceDays { get; init; } = 15;
}
