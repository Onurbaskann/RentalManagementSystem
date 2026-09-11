using KiraTakip.Data;
using KiraTakip.Domain.Charges;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces.Charges;
using KiraTakip.Models.Dtos.ManualCharge;

namespace KiraTakip.Services.Charges;

public class ManualChargeService(
    IChargeRepository chargeRepository,
    ILeaseRepository leaseRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork) : IManualChargeService, ITransactionalService
{
    public Task<List<ManualChargeListItemDto>> GetAllAsync(GetManualChargesInput input)
        => chargeRepository.GetManualChargeListAsync(
            input.PropertyIds?.ToList(),
            input.Status,
            input.Relation,
            input.LeaseId,
            input.UnitIds?.ToList());

    public Task<PagedResult<ManualChargeListItemDto>> GetPageAsync(GetManualChargesPageInput input)
        => chargeRepository.GetManualChargePagedListAsync(
            input.Query,
            input.PropertyIds?.ToList(),
            input.Relation,
            input.LeaseId,
            input.UnitIds?.ToList());

    public Task<int> GetCancelledCountAsync(GetCancelledManualChargeCountInput input)
        => chargeRepository.GetCancelledManualChargeCountAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public Task<List<LeaseDropdownDto>> GetActiveLeasesAsync(GetActiveManualChargeLeasesInput input)
        => leaseRepository.GetActiveDropdownAsync(
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());

    public Task<List<ChargeTypeLookupDto>> GetManualChargeTypesAsync()
        => chargeTypeRepository.GetManualChargeTypesAsync();

    public Task<List<UnitLookupDto>> GetAllUnitsAsync(GetManualChargeUnitsInput input)
        => unitRepository.GetAllOptionsAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public async Task CreateAsync(CreateManualChargeInput input)
    {
        var propertyIds = input.AccessScope.PropertyIds?.ToList();
        var unitIds = input.AccessScope.UnitIds?.ToList();

        var unitContext = await unitRepository.GetLeaseContextAsync(input.UnitId);
        Guard.InvalidField(
            unitContext == null,
            nameof(input.UnitId),
            "Birim bulunamadı.",
            "MANUAL_CHARGE_UNIT_NOT_FOUND");
        var selectedUnit = unitContext!;

        Guard.Forbidden(
            IsOutsideScope(selectedUnit.PropertyId, selectedUnit.UnitId, propertyIds, unitIds),
            "Bu birim için işlem yetkiniz bulunmuyor.",
            "MANUAL_CHARGE_UNIT_FORBIDDEN");

        var tenant = await tenantRepository.GetByIdAsync(input.TenantId);
        Guard.InvalidField(
            tenant == null,
            nameof(input.TenantId),
            "Kiracı bulunamadı.",
            "MANUAL_CHARGE_TENANT_NOT_FOUND");
        var selectedTenant = tenant!;

        Guard.Forbidden(
            !await tenantRepository.IsInScopeAsync(selectedTenant.Id, propertyIds, unitIds),
            "Bu kiracı için işlem yetkiniz bulunmuyor.",
            "MANUAL_CHARGE_TENANT_FORBIDDEN");

        int? leaseId = null;
        if (input.LeaseId.HasValue && input.LeaseId.Value > 0)
        {
            var lease = await leaseRepository.GetByIdAsync(input.LeaseId.Value);
            Guard.InvalidField(
                lease == null,
                nameof(input.LeaseId),
                "Sözleşme bulunamadı.",
                "MANUAL_CHARGE_LEASE_NOT_FOUND");
            var selectedLease = lease!;
            var leaseValidation = ManualChargePolicy.ValidateLease(
                selectedLease.Status,
                selectedLease.TenantId,
                input.TenantId,
                selectedLease.UnitId,
                input.UnitId);

            Guard.InvalidField(
                leaseValidation == ManualChargeLeaseValidationResult.Terminated,
                nameof(input.LeaseId),
                "Feshedilmiş sözleşme için manuel borç oluşturulamaz.",
                "MANUAL_CHARGE_LEASE_TERMINATED");
            Guard.InvalidField(
                leaseValidation == ManualChargeLeaseValidationResult.NotActive,
                nameof(input.LeaseId),
                "Sona ermiş sözleşme için manuel borç oluşturulamaz.",
                "MANUAL_CHARGE_LEASE_ENDED");
            Guard.InvalidField(
                leaseValidation == ManualChargeLeaseValidationResult.TenantMismatch,
                nameof(input.LeaseId),
                "Seçilen kiracı, sözleşmenin kiracısıyla eşleşmiyor.",
                "MANUAL_CHARGE_LEASE_TENANT_MISMATCH");
            Guard.InvalidField(
                leaseValidation == ManualChargeLeaseValidationResult.UnitMismatch,
                nameof(input.LeaseId),
                "Seçilen birim, sözleşmenin birimiyle eşleşmiyor.",
                "MANUAL_CHARGE_LEASE_UNIT_MISMATCH");
            leaseId = selectedLease.Id;
        }

        var chargeType = await chargeTypeRepository.GetActiveManualByIdAsync(input.ChargeTypeId);
        Guard.InvalidField(
            chargeType == null,
            nameof(input.ChargeTypeId),
            "Geçersiz borç tipi.",
            "MANUAL_CHARGE_TYPE_INVALID");

        var amounts = ManualChargePolicy.CalculateAmounts(
            input.Amount,
            input.IsVatApplied,
            input.VatRate);
        var vatAmount = amounts.VatAmount;
        var totalAmount = amounts.TotalAmount;
        var vatRate = amounts.VatRate;

        var lineItem = new ChargeLineItem
        {
            ChargeTypeId = chargeType!.Id,
            Description = input.Description,
            CalculationMethod = CalculationMethod.Fixed,
            UnitValue = input.Amount,
            Multiplier = 1m,
            Amount = input.Amount,
            KdvRate = vatRate,
            KdvAmount = vatAmount,
            TotalAmount = totalAmount,
            SourceType = LineItemSourceType.ManualInput
        };

        var charge = new Charge
        {
            TenantId = input.TenantId,
            UnitId = input.UnitId,
            LeaseId = leaseId,
            PeriodStart = input.DueDate,
            PeriodEnd = input.DueDate.AddDays(1),
            DueDate = input.DueDate,
            ExpectedAmount = input.Amount,
            KdvAmount = vatAmount,
            TotalAmount = totalAmount,
            PaidAmount = 0,
            Status = ChargeStatus.Pending,
            SourceType = ChargeSourceType.Manual,
            CancellationNote = input.Note,
            LineItems = new List<ChargeLineItem> { lineItem }
        };

        await chargeRepository.AddAsync(charge);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task CancelAsync(CancelManualChargeInput input)
    {
        var charge = Guard.NotFound(
            await chargeRepository.GetManualChargeByIdAsync(
                input.ChargeId,
                input.AccessScope.PropertyIds?.ToList(),
                input.AccessScope.UnitIds?.ToList()),
            "Manuel borç kaydı bulunamadı.",
            "MANUAL_CHARGE_NOT_FOUND");

        var hasPayment = ManualChargePolicy.HasApprovedPayment(
            charge.Allocations.Select(allocation => allocation.Status));
        var cancellationValidation = ManualChargePolicy.ValidateCancellation(
            charge.Status,
            hasPayment);

        Guard.Conflict(
            cancellationValidation == ManualChargeCancellationValidationResult.AlreadyCancelled,
            "Bu kayıt zaten iptal edilmiş.",
            "MANUAL_CHARGE_ALREADY_CANCELLED");

        Guard.Conflict(
            cancellationValidation == ManualChargeCancellationValidationResult.HasApprovedPayment,
            "Ödemesi alınmış manuel borç iptal edilemez.",
            "MANUAL_CHARGE_HAS_PAYMENT");

        charge.Status = ChargeStatus.Cancelled;
        charge.CancellationNote = string.IsNullOrEmpty(charge.CancellationNote)
            ? input.Reason
            : $"{charge.CancellationNote} | İptal: {input.Reason}";

        await unitOfWork.SaveChangesAsync();
    }

    private static bool IsOutsideScope(
        int propertyId,
        int unitId,
        IReadOnlyCollection<int>? propertyIds,
        IReadOnlyCollection<int>? unitIds)
    {
        if (propertyIds == null && unitIds == null)
            return false;

        return propertyIds?.Contains(propertyId) != true
            && unitIds?.Contains(unitId) != true;
    }
}
