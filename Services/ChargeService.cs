using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ChargeService(
    IChargeRepository chargeRepository,
    IPaymentAllocationRepository paymentAllocationRepository,
    IUnitRepository unitRepository,
    IChargeLineItemRepository chargeLineItemRepository,
    IUnitOfWork unitOfWork) : IChargeService
{
    // ── Listeleme ────────────────────────────────────────────────────────
    public async Task<List<ChargeListItemDto>> GetListAsync(GetChargesInput input)
    {
        return await chargeRepository.GetListAsync(input.LeaseId, input.PropertyIds?.ToList(), input.UnitIds?.ToList());
    }

    public async Task<PagedResult<ChargeListItemDto>> GetPagedAsync(GetChargesPageInput input)
    {
        return await chargeRepository.GetPagedListAsync(
            input.Query,
            input.LeaseId,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());
    }

    public Task<ChargeDetailDto?> GetDetailsAsync(GetChargeDetailsInput input)
        => chargeRepository.GetDetailsAsync(input.Id);

    public async Task<ChargeDetailDto> GetTenantDetailsAsync(GetTenantChargeDetailsInput input)
        => Guard.NotFound(
            await chargeRepository.GetTenantDetailsAsync(
                input.ChargeId,
                input.TenantId,
                input.PropertyIds?.ToList(),
                input.UnitIds?.ToList()),
            "Tahakkuk bulunamadı.",
            "TENANT_CHARGE_NOT_FOUND");

    public Task<ChargeIndexOptionsDto> GetIndexOptionsAsync(GetChargeIndexOptionsInput input)
        => chargeRepository.GetIndexOptionsAsync(input);

    public Task<CurrentLeaseChargeDto> GetCurrentLeaseChargeAsync(GetCurrentLeaseChargeInput input)
        => chargeRepository.GetCurrentLeaseChargeAsync(input);

    public Task<TenantLeaseChargeDataDto> GetTenantLeaseDataAsync(
        GetTenantLeaseChargeDataInput input)
        => chargeRepository.GetTenantLeaseDataAsync(input);

    public Task<ManualLeaseChargeSummaryDto> GetManualLeaseChargeSummaryAsync(
        GetManualLeaseChargeSummaryInput input)
        => chargeRepository.GetManualLeaseChargeSummaryAsync(input);

    public async Task<TenantChargeIndexDataDto> GetTenantChargeIndexAsync(
        GetTenantChargeIndexInput input)
    {
        var charges = await chargeRepository.GetTenantPagedListAsync(input);
        var overview = await chargeRepository.GetTenantChargeOverviewAsync(
            input.TenantId,
            input.Today);
        var collectedAmount = await paymentAllocationRepository.GetTenantApprovedTotalAsync(
            input.TenantId,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());
        var units = await unitRepository.GetTenantLeaseOptionsAsync(
            input.TenantId,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

        return new TenantChargeIndexDataDto(
            charges,
            overview.TotalChargeAmount,
            collectedAmount,
            overview.RemainingDebtAmount,
            overview.OverdueRemainingAmount,
            units,
            overview.AvailableYears);
    }

    public async Task<MonthlyCollectionReportDto> GetMonthlyCollectionReportAsync(
        GetMonthlyCollectionReportInput input)
    {
        var report = await chargeRepository.GetMonthlyCollectionReportAsync(input);
        var rowsByMonth = report.Rows.ToDictionary(row => row.Month);

        report.Rows = Enumerable.Range(1, 12)
            .Select(month => rowsByMonth.GetValueOrDefault(month)
                ?? new MonthlyCollectionReportRowDto { Month = month })
            .ToList();

        if (!report.AvailableYears.Contains(input.Year))
        {
            report.AvailableYears.Add(input.Year);
            report.AvailableYears = report.AvailableYears
                .OrderByDescending(year => year)
                .ToList();
        }

        return report;
    }

    // ── Business: Gecikme Güncelleme ─────────────────────────────────────
    public async Task UpdateDelaysAsync()
    {
        var chargesToMarkOverdue = await chargeRepository.GetChargesToMarkOverdueAsync(DateTime.Today);
        if (chargesToMarkOverdue.Count == 0) return;

        foreach (var charge in chargesToMarkOverdue)
        {
            charge.Status = ChargeStatus.Overdue;

            await chargeRepository.UpdateAsync(charge);
        }

        await unitOfWork.SaveChangesAsync();
    }

    // ── İş Kuralı: Ödenen Tutarı Güncelleme ──────────────────────────────
    public async Task UpdatePaidAmountAsync(UpdateChargePaidAmountInput input)
    {
        var lineItem = Guard.NotFound(
            await chargeLineItemRepository.GetForPaymentUpdateAsync(input.ChargeLineItemId),
            "Tahakkuk kalemi bulunamadı.",
            "CHARGE_LINE_ITEM_NOT_FOUND");
        var balance = Guard.NotFound(
            await chargeLineItemRepository.GetPaymentBalanceAsync(input.ChargeLineItemId),
            "Tahakkuk kalemi bulunamadı.",
            "CHARGE_LINE_ITEM_NOT_FOUND");
        lineItem.PaidAmount = balance.ApprovedAmount;
        await unitOfWork.SaveChangesAsync();

        var charge = Guard.NotFound(
            await chargeRepository.GetByIdAsync(input.ChargeId),
            "Tahakkuk bulunamadı.");

        var paidAmount = await chargeLineItemRepository.GetChargePaidAmountTotalAsync(input.ChargeId);

        charge.PaidAmount = paidAmount;
        charge.Status = paidAmount >= charge.TotalAmount
                        ? ChargeStatus.Paid
                        : paidAmount > 0
                            ? ChargeStatus.PartiallyPaid
                            : DateTime.Today > charge.DueDate
                                ? ChargeStatus.Overdue
                                : ChargeStatus.Pending;

        await chargeRepository.UpdateAsync(charge);
        await unitOfWork.SaveChangesAsync();
    }
}
