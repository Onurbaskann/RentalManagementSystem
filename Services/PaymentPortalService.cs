using KiraTakip.Models.Dtos;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PaymentPortalService(
    IPaymentLinkService paymentLinkService,
    ITenantRepository tenantRepository,
    IChargeRepository chargeRepository,
    IOperationalPolicyProvider operationalPolicyProvider) : IPaymentPortalService
{

    public async Task<PaymentPortalResultDto> GetAsync(
        GetPaymentPortalInput input,
        CancellationToken cancellationToken = default)
    {
        var validation = await paymentLinkService.ValidateAsync(
            new ValidatePaymentLinkInput(input.Token),
            cancellationToken);

        if (!validation.Success)
        {
            return new PaymentPortalResultDto(
                false,
                validation.Reason ?? "Geçersiz veya süresi dolmuş ödeme bağlantısı.",
                string.Empty,
                []);
        }

        var tenant = await tenantRepository.GetByIdAsync(validation.TenantId);
        if (tenant == null)
        {
            return new PaymentPortalResultDto(
                false,
                "Kiracı bulunamadı.",
                string.Empty,
                []);
        }

        var dueDateLimit = DateTime.Today.AddDays(
            operationalPolicyProvider.Current.PaymentReminderDaysBefore);
        var charges = await chargeRepository.GetPaymentPortalChargesAsync(
            tenant.Id,
            dueDateLimit,
            cancellationToken);

        return new PaymentPortalResultDto(
            true,
            null,
            tenant.Name,
            charges);
    }
}
