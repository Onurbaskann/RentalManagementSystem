namespace KiraTakip.Models.Dtos.ChargeReminder;

public record ChargeReminderScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetPendingChargeRemindersInput(
    DateTime DueDateLimit,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

