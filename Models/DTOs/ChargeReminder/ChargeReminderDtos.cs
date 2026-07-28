namespace KiraTakip.Models.Dtos;

public record ChargeReminderScopeInput(
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

public record GetPendingChargeRemindersInput(
    DateTime DueDateLimit,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null);

