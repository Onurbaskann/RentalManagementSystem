using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.ChargeReminder;

namespace KiraTakip.Services.Interfaces.Charges;

public interface IChargeReminderService
{
    Task<int> GetDebtorCountAsync(
        ChargeReminderScopeInput input,
        CancellationToken cancellationToken = default);
    Task SendDebtRemindersAsync(
        ChargeReminderScopeInput input,
        CancellationToken cancellationToken = default);
}
