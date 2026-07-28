using System.Globalization;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TenantPanelService(
    ITenantRepository tenantRepository,
    IApplicationUserRepository applicationUserRepository,
    ILeaseRepository leaseRepository,
    IChargeRepository chargeRepository,
    IChargeLineItemRepository chargeLineItemRepository,
    IPaymentAllocationRepository paymentAllocationRepository) : ITenantPanelService
{
    public async Task<TenantPanelDashboardDto> GetDashboardAsync(GetTenantPanelDashboardInput input)
    {
        var tenant = Guard.NotFound(
            await tenantRepository.GetDetailsAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_PANEL_TENANT_NOT_FOUND");

        var user = Guard.NotFound(
            await applicationUserRepository.GetUserByIdAndTenantIdAsync(
                input.UserId,
                input.TenantId),
            "Kullanıcı bulunamadı.",
            "TENANT_PANEL_USER_NOT_FOUND");

        var activeLeaseCount = input.CanViewLeases
            ? await leaseRepository.CountActiveByTenantAsync(
                input.TenantId,
                input.Today,
                input.PropertyIds?.ToList(),
                input.UnitIds?.ToList())
            : 0;

        var chargeData = input.CanViewCharges || input.CanViewPayments
            ? await chargeRepository.GetTenantPanelDataAsync(
                new GetTenantPanelChargeDataInput(
                    input.TenantId,
                    input.Today,
                    IncludeDebtData: input.CanViewCharges,
                    IncludeMonthlyExpected: input.CanViewPayments,
                    PropertyIds: input.PropertyIds,
                    UnitIds: input.UnitIds))
            : new TenantPanelChargeDataDto(0m, 0, 0m, 0, 0m, [], [], []);

        var debtTypeDistribution = input.CanViewCharges
            ? await chargeLineItemRepository.GetTenantDebtDistributionAsync(
                input.TenantId,
                input.PropertyIds?.ToList(),
                input.UnitIds?.ToList())
            : [];

        var paymentData = input.CanViewPayments
            ? await paymentAllocationRepository.GetTenantPanelDataAsync(
                new GetTenantPanelPaymentDataInput(
                    input.TenantId,
                    input.Today,
                    PropertyIds: input.PropertyIds,
                    UnitIds: input.UnitIds))
            : new TenantPanelPaymentDataDto([], []);

        var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
        var monthlyCashFlow = new List<TenantPanelMonthlyCashFlowDto>();
        if (input.CanViewPayments)
        {
            for (var monthOffset = 5; monthOffset >= 0; monthOffset--)
            {
                var month = new DateTime(input.Today.Year, input.Today.Month, 1)
                    .AddMonths(-monthOffset);
                var expected = chargeData.MonthlyExpected
                    .FirstOrDefault(item =>
                        item.Year == month.Year && item.Month == month.Month)?.Total ?? 0m;
                var paid = paymentData.MonthlyPaid
                    .FirstOrDefault(item =>
                        item.Year == month.Year && item.Month == month.Month)?.Total ?? 0m;

                monthlyCashFlow.Add(new TenantPanelMonthlyCashFlowDto(
                    turkishCulture.DateTimeFormat.GetAbbreviatedMonthName(month.Month),
                    expected,
                    paid));
            }
        }

        var upcomingCharges = chargeData.UpcomingCharges.Select(charge =>
        {
            var dayDifference = (charge.DueDate.Date - input.Today).Days;
            var borderColor = dayDifference < 0 ? "red" : dayDifference <= 7 ? "amber" : "emerald";

            return new TenantPanelUpcomingChargeDto(
                charge.ChargeId,
                charge.PeriodStart.ToString("MMMM yyyy", turkishCulture),
                string.IsNullOrEmpty(charge.UnitName)
                    ? charge.PropertyName ?? "—"
                    : $"{charge.UnitName} · {charge.PropertyName}",
                charge.DueDate,
                dayDifference,
                charge.RemainingAmount,
                borderColor);
        }).ToList();

        var recentPayments = paymentData.RecentPayments.Select(payment =>
            new TenantPanelRecentPaymentDto(
                payment.PaymentId,
                payment.PaymentDate,
                payment.Amount,
                payment.PaymentChannel switch
                {
                    PaymentChannel.BankTransfer => "Havale",
                    PaymentChannel.Eft => "EFT",
                    PaymentChannel.Cash => "Nakit",
                    _ => "Diğer"
                },
                payment.Status switch
                {
                    PaymentStatus.Approved => "Onaylandı",
                    PaymentStatus.Rejected => "Reddedildi",
                    _ => "Onay Bekliyor"
                },
                payment.Status switch
                {
                    PaymentStatus.Approved => "emerald",
                    PaymentStatus.Rejected => "red",
                    _ => "amber"
                }))
            .ToList();

        return new TenantPanelDashboardDto(
            tenant.DisplayName,
            user.AdSoyad ?? user.Email ?? "Kullanıcı",
            activeLeaseCount,
            chargeData.TotalOutstandingDebt,
            chargeData.UpcomingPaymentCount,
            chargeData.UpcomingPaymentAmount,
            chargeData.OverdueCount,
            chargeData.OverdueAmount,
            monthlyCashFlow,
            debtTypeDistribution,
            chargeData.DebtBalanceSparkline,
            upcomingCharges,
            recentPayments);
    }
}
