using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Settings;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace KiraTakip.Services;

public class ChargeReminderService(
    IChargeRepository chargeRepository,
    IUnitOfWork unitOfWork,
    IPaymentLinkService paymentLinkService,
    IMailService mailService,
    IRazorViewToStringRenderer renderer,
    ILogger<ChargeReminderService> logger,
    IOptions<PaymentLinkSettings> paymentOptions,
    IOptions<SmtpSettings> smtpOptions) : IChargeReminderService
{
    private readonly PaymentLinkSettings paymentSettings = paymentOptions.Value;
    private readonly SmtpSettings smtpSettings = smtpOptions.Value;

    public async Task<int> GetDebtorCountAsync(
        ChargeReminderScopeInput input,
        CancellationToken cancellationToken = default)
    {
        var dueDateLimit = DateTime.Today.AddDays(paymentSettings.ReminderDaysBefore);
        var debts = await chargeRepository.GetPendingReminderChargesAsync(
            new GetPendingChargeRemindersInput(
                dueDateLimit,
                input.PropertyIds,
                input.UnitIds),
            cancellationToken);

        return debts.GroupBy(charge => charge.TenantId).Count();
    }

    public async Task SendDebtRemindersAsync(
        ChargeReminderScopeInput input,
        CancellationToken cancellationToken = default)
    {
        var successfulSends = 0;
        var skippedDuringCooldown = 0;
        var failedSends = 0;

        Guard.Against(
            string.IsNullOrWhiteSpace(paymentSettings.Secret) || paymentSettings.Secret.Length < 32,
            "PaymentLink:Secret ayarı yapılmamış veya çok kısa (en az 32 karakter olmalıdır).");
        Guard.Against(
            paymentSettings.TokenTtlHours <= 0,
            "PaymentLink:TokenTtlHours sıfırdan büyük olmalıdır.");
        Guard.Against(
            string.IsNullOrWhiteSpace(smtpSettings.Host) || string.IsNullOrWhiteSpace(smtpSettings.From),
            "SMTP sunucu ayarları (Smtp:Host veya Smtp:From) yapılandırılmamış.");

        var today = DateTime.Today;
        var dueDateLimit = today.AddDays(paymentSettings.ReminderDaysBefore);
        var cooldownThreshold = today.AddDays(-paymentSettings.ReminderCooldownDays);
        var debts = await chargeRepository.GetPendingReminderChargesAsync(
            new GetPendingChargeRemindersInput(
                dueDateLimit,
                input.PropertyIds,
                input.UnitIds),
            cancellationToken);
        var groups = debts.GroupBy(charge => charge.TenantId).ToList();

        foreach (var group in groups)
        {
            var tenant = group.First().Tenant;
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Email))
            {
                logger.LogWarning(
                    "Kiracı {KiraciId} için geçerli e-posta adresi bulunamadı. Atlanıyor.",
                    group.Key);
                failedSends++;
                continue;
            }

            var debtsOutsideCooldown = group
                .Where(charge => charge.LastReminderDate == null
                    || charge.LastReminderDate.Value.Date <= cooldownThreshold)
                .ToList();

            if (!debtsOutsideCooldown.Any())
            {
                logger.LogInformation(
                    "Kiracı {KiraciId} için son hatırlatmalar bekleme süresi içerisinde. Atlanıyor.",
                    group.Key);
                skippedDuringCooldown++;
                continue;
            }

            var mailModel = new TenantDebtReminderEmailViewModel
            {
                FirstName = tenant.Name,
                LastName = "",
                Email = tenant.Email,
                PaymentLinkValidityText = FormatValidity(paymentSettings.TokenTtlHours),
                PaymentLink = await paymentLinkService.CreateAsync(
                    new CreatePaymentLinkInput(tenant.Id),
                    cancellationToken),
                Debts = group.OrderBy(charge => charge.DueDate).Select(charge => new DebtReminderLineViewModel
                {
                    PropertyName = charge.Unit.Property.Name,
                    UnitName = charge.Unit.Name,
                    PeriodStart = charge.PeriodStart,
                    DueDate = charge.DueDate,
                    TotalAmount = charge.TotalAmount,
                    PaidAmount = charge.Allocations
                        .Where(allocation => allocation.Status == PaymentStatus.Approved)
                        .Sum(allocation => allocation.Amount)
                }).ToList()
            };

            string htmlBody;
            try
            {
                htmlBody = await renderer.RenderAsync("DebtReminder", mailModel);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Kiracı {KiraciId} için e-posta şablonu oluşturulamadı.",
                    tenant.Id);
                failedSends++;
                continue;
            }

            try
            {
                await mailService.SendAsync(
                    tenant.Email,
                    mailModel.DisplayName,
                    "KiraTakip - Ödeme Hatırlatması",
                    htmlBody,
                    cancellationToken);

                foreach (var debt in debtsOutsideCooldown)
                    debt.LastReminderDate = DateTime.Today;

                successfulSends++;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Kiracı {KiraciId} için e-posta gönderimi başarısız.",
                    tenant.Id);
                failedSends++;
            }
        }

        if (successfulSends > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        if (failedSends > 0)
        {
            var messageParts = new List<string>();
            if (successfulSends > 0)
                messageParts.Add($"{successfulSends} kiracıya e-posta gönderildi");
            if (skippedDuringCooldown > 0)
                messageParts.Add(
                    $"{skippedDuringCooldown} kiracı (bekleme süresinde olduğu için) atlandı");
            messageParts.Add($"{failedSends} gönderimde hata oluştu");

            Guard.Against(
                true,
                string.Join(", ", messageParts) + ". Detaylar için logları inceleyin.");
        }
    }

    private static string FormatValidity(int validityHours)
        => validityHours % 24 == 0
            ? $"{validityHours / 24} gün"
            : $"{validityHours} saat";
}
