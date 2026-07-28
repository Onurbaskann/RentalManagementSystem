using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IChargeReminderService
{
    Task<int> GetDebtorCountAsync(
        ChargeReminderScopeInput input,
        CancellationToken cancellationToken = default);
    Task SendDebtRemindersAsync(
        ChargeReminderScopeInput input,
        CancellationToken cancellationToken = default);
}
