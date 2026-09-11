using KiraTakip.Data;
using KiraTakip.Domain.Charges;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Common;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Notifications;
using KiraTakip.Services.Interfaces.Payments;
using Microsoft.Extensions.Logging;
using KiraTakip.Models.Dtos.ChargeReminder;

namespace KiraTakip.Services.Charges;

public class ChargeReminderService(
    IChargeRepository chargeRepository,
    IUnitOfWork unitOfWork,
    IRequestContext requestContext,
    IMailService mailService,
    IRazorViewToStringRenderer renderer,
    ILogger<ChargeReminderService> logger,
    IOperationalPolicyProvider operationalPolicyProvider,
    ISmtpConfigurationValidator smtpValidator,
    IHashIdEncoder hashIdEncoder) : IChargeReminderService
{

    public async Task<int> GetDebtorCountAsync(
        ChargeReminderScopeInput input,
        CancellationToken cancellationToken = default)
    {
        var dueDateLimit = ChargeReminderSchedulePolicy.CalculateDueDateLimit(
            DateTime.Today,
            operationalPolicyProvider.Current.PaymentReminderDaysBefore);
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
            !smtpValidator.IsConfigured,
            "SMTP sunucu ayarları (Smtp:Host veya Smtp:From) yapılandırılmamış.");

        var today = DateTime.Today;
        var policy = operationalPolicyProvider.Current;
        var dueDateLimit = ChargeReminderSchedulePolicy.CalculateDueDateLimit(today, policy.PaymentReminderDaysBefore);
        var cooldownThreshold = ChargeReminderSchedulePolicy.CalculateCooldownThreshold(today, policy.PaymentReminderCooldownDays);
        var debts = await chargeRepository.GetPendingReminderChargesAsync(
            new GetPendingChargeRemindersInput(
                dueDateLimit,
                input.PropertyIds,
                input.UnitIds),
            cancellationToken);
        var groups = debts.GroupBy(charge => charge.TenantId).ToList();

        var baseUrl = !string.IsNullOrWhiteSpace(requestContext.BaseUrl)
            ? requestContext.BaseUrl
            : "http://localhost:5031";

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
                .Where(charge => ChargeReminderSchedulePolicy.IsEligibleForReminder(charge.LastReminderDate, cooldownThreshold))
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
                Debts = group.OrderBy(charge => charge.DueDate).Select(charge => new DebtReminderLineViewModel
                {
                    PropertyName = charge.Unit.Property.Name,
                    UnitName = charge.Unit.Name,
                    PeriodStart = charge.PeriodStart,
                    DueDate = charge.DueDate,
                    TotalAmount = charge.TotalAmount,
                    PaidAmount = charge.Allocations
                        .Where(allocation => allocation.Status == PaymentStatus.Approved)
                        .Sum(allocation => allocation.Amount),
                    ChargeDetailsUrl = $"{baseUrl}/Tenant/Charges/Details/{hashIdEncoder.Encode(charge.Id)}"
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
}
