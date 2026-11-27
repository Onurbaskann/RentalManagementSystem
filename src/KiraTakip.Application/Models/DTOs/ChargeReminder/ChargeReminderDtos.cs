namespace KiraTakip.Models.Dtos.ChargeReminder;

public record ChargeReminderScopeInput(SpaceAccessScope Scope);

public record GetPendingChargeRemindersInput(
    DateTime DueDateLimit,
    SpaceAccessScope? Scope = null);
