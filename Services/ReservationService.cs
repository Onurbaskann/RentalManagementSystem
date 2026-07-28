using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class ReservationService(
    IReservationRepository reservationRepository,
    IReservationRateOverrideRepository reservationRateOverrideRepository,
    IChargeRepository chargeRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository,
    IUnitOfWork uow) : IReservationService, ITransactionalService
{

    // ── Listeleme ──────────────────────────────────────────────────────────────

    public async Task<List<ReservationListItemDto>> GetAllAsync(GetReservationsInput input)
    {
        return await reservationRepository.GetListAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());
    }

    public async Task<List<ReservationListItemDto>> GetTenantReservationsAsync(
        GetTenantReservationsInput input)
    {
        Guard.NotFound(
            await tenantRepository.GetDetailsAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_RESERVATION_TENANT_NOT_FOUND");

        return await reservationRepository.GetTenantListAsync(
            input.TenantId,
            input.CurrentTime,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());
    }

    public async Task<ReservationListItemDto> GetByIdAsync(GetReservationByIdInput input)
    {
        var reservation = Guard.NotFound(
            await reservationRepository.GetByIdAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");

        EnsureScope(reservation.PropertyId, reservation.UnitId, input.AccessScope);
        return reservation;
    }

    public async Task<ReservationFormOptionsDto> GetFormOptionsAsync(
        GetReservationFormOptionsInput input)
        => new(await unitRepository.GetReservableUnitsAsync(
                   input.AccessScope.PropertyIds?.ToList(),
                   input.AccessScope.UnitIds?.ToList()),
               await tenantRepository.GetReservationOptionsAsync());

    // ── Ücret Hesaplama (öncelik: birime özel → birim türü genel tarife → hata) ─

    public async Task<ReservationCalculationResultDto> CalculateAsync(CalculateReservationInput input)
    {
        Guard.Against(
            input.EndDate <= input.StartDate,
            "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.",
            "RESERVATION_INVALID_DATE_RANGE");

        var unit = Guard.NotFound(
            await unitRepository.GetReservationContextAsync(input.UnitId),
            "Birim bulunamadı.",
            "RESERVATION_UNIT_NOT_FOUND");

        EnsureScope(unit.PropertyId, unit.UnitId, input.AccessScope);
        EnsureReservableUnit(unit);

        return await CalculateCoreAsync(input, unit);
    }

    private async Task<ReservationCalculationResultDto> CalculateCoreAsync(
        CalculateReservationInput input,
        ReservationUnitContextDto unit)
    {
        var result = new ReservationCalculationResultDto();
        var rateRule = await reservationRateOverrideRepository.GetActiveForUnitAsync(input.UnitId);

        int freeDurationMinutes;
        int billingPeriodMinutes;
        decimal periodRate;
        decimal vatRate;

        if (rateRule != null)
        {
            freeDurationMinutes = rateRule.FreeDurationMinutes;
            billingPeriodMinutes = rateRule.BillingPeriodMinutes;
            periodRate = rateRule.PeriodRate;
            vatRate = rateRule.KdvRate;
            result.HasRateRule = true;
        }
        else
        {
            var currentYear = input.StartDate.Year;
            var generalRate = await reservationRateOverrideRepository.GetGeneralAsync(
                unit.UnitTypeId,
                currentYear);

            if (generalRate == null)
            {
                result.ErrorMessage = $"{currentYear} yılı için '{unit.UnitTypeName}' türünde genel rezervasyon tarifesi tanımlı değil.";
                return result;
            }

            freeDurationMinutes = generalRate.FreeDurationMinutes;
            billingPeriodMinutes = generalRate.BillingPeriodMinutes;
            periodRate = generalRate.PeriodRate;
            vatRate = generalRate.KdvRate;
            result.HasRateRule = true;
        }

        Guard.Against(
            billingPeriodMinutes <= 0,
            "Rezervasyon tarifesinin ücretlendirme periyodu geçersizdir.",
            "RESERVATION_INVALID_BILLING_PERIOD");

        var totalDurationMinutes = (int)Math.Ceiling((input.EndDate - input.StartDate).TotalMinutes);
        var paidDurationMinutes = Math.Max(0, totalDurationMinutes - freeDurationMinutes);
        var paidPeriodCount = paidDurationMinutes == 0
            ? 0
            : (int)Math.Ceiling((double)paidDurationMinutes / billingPeriodMinutes);

        result.TotalDurationMinutes = totalDurationMinutes;
        result.FreeDurationMinutes = Math.Min(freeDurationMinutes, totalDurationMinutes);
        result.PaidDurationMinutes = paidDurationMinutes;
        result.PaidPeriodCount = paidPeriodCount;
        result.UnitRate = periodRate;
        result.RateAmount = paidPeriodCount * periodRate;
        result.VatRate = vatRate;
        result.VatAmount = Math.Round(result.RateAmount * vatRate / 100, 2);
        result.TotalAmount = result.RateAmount + result.VatAmount;

        return result;
    }

    // ── Rezervasyon Oluşturma ─────────────────────────────────────────────────

    public async Task<int> CreateAsync(CreateReservationInput input)
    {
        Guard.InvalidField(
            input.EndDate <= input.StartDate,
            nameof(input.EndDate),
            "Bitiş tarihi başlangıç tarihinden sonra olmalıdır.",
            "RESERVATION_INVALID_DATE_RANGE");

        var unit = await unitRepository.GetReservationContextAsync(input.UnitId);
        Guard.InvalidField(
            unit == null,
            nameof(input.UnitId),
            "Birim bulunamadı.",
            "RESERVATION_UNIT_NOT_FOUND");

        EnsureScope(unit!.PropertyId, unit.UnitId, input.AccessScope);
        Guard.InvalidField(
            !unit.IsUnitActive || !unit.IsUnitTypeActive || unit.Usage != UnitTypeUsage.Reservable,
            nameof(input.UnitId),
            "Seçilen birim aktif veya rezervasyona uygun değildir.",
            "RESERVATION_UNIT_NOT_RESERVABLE");

        var tenant = await tenantRepository.GetByIdAsync(input.TenantId);
        Guard.InvalidField(
            tenant == null || !tenant.IsActive,
            nameof(input.TenantId),
            "Kiracı bulunamadı veya aktif değildir.",
            "RESERVATION_TENANT_NOT_ACTIVE");

        Guard.InvalidField(
            await reservationRepository.IsConflictAsync(input.UnitId, input.StartDate, input.EndDate),
            nameof(input.StartDate),
            "Seçilen zaman aralığında bu birim için başka bir rezervasyon mevcut.",
            "RESERVATION_TIME_CONFLICT");

        var calculation = await CalculateCoreAsync(
            new CalculateReservationInput(
                input.UnitId,
                input.StartDate,
                input.EndDate,
                input.AccessScope),
            unit);
        Guard.InvalidField(
            !string.IsNullOrWhiteSpace(calculation.ErrorMessage),
            nameof(input.UnitId),
            calculation.ErrorMessage ?? "Rezervasyon ücreti hesaplanamadı.",
            "RESERVATION_RATE_NOT_FOUND");

        var reservation = new Reservation
        {
            UnitId = input.UnitId,
            TenantId = input.TenantId,
            StartDate = input.StartDate,
            EndDate = input.EndDate,
            TotalDurationMinutes = calculation.TotalDurationMinutes,
            FreeDurationMinutes = calculation.FreeDurationMinutes,
            PaidDurationMinutes = calculation.PaidDurationMinutes,
            UnitRate = calculation.UnitRate,
            RateAmount = calculation.RateAmount,
            KdvRate = calculation.VatRate > 0 ? calculation.VatRate : null,
            KdvAmount = calculation.VatAmount > 0 ? calculation.VatAmount : null,
            TotalAmount = calculation.TotalAmount,
            Status = ReservationStatus.Planned,
            Description = input.Description,
        };

        await reservationRepository.AddAsync(reservation);
        await uow.SaveChangesAsync();

        return reservation.Id;
    }

    // ── İptal ────────────────────────────────────────────────────────────────

    public async Task CancelAsync(CancelReservationInput input)
    {
        Guard.Against(
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 450,
            "İptal nedeni zorunlu ve en fazla 450 karakter olmalıdır.",
            "RESERVATION_INVALID_CANCELLATION_REASON");

        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");

        EnsureScope(reservation.Unit.PropertyId, reservation.UnitId, input.AccessScope);
        Guard.Conflict(
            reservation.Status == ReservationStatus.Cancelled,
            "Bu rezervasyon zaten iptal edilmiş.",
            "RESERVATION_ALREADY_CANCELLED");
        Guard.Conflict(
            reservation.Status is not ReservationStatus.Planned
                and not ReservationStatus.TransferredToCharge,
            "Yalnızca planlanmış veya tahakkuka aktarılmış rezervasyonlar iptal edilebilir.",
            "RESERVATION_CANNOT_BE_CANCELLED");

        var reason = input.Reason.Trim();

        if (reservation.Status == ReservationStatus.TransferredToCharge)
        {
            var charge = await chargeRepository.GetByReservationWithAllocationsAsync(reservation.Id);

            var hasApprovedPayment = charge?.Allocations.Any(allocation => allocation.Status == PaymentStatus.Approved) ?? false;
            Guard.Conflict(
                hasApprovedPayment,
                "Ödemesi alınmış tahakkuka bağlı rezervasyon iptal edilemez.",
                "RESERVATION_HAS_APPROVED_PAYMENT");

            if (charge != null)
            {
                charge.Status = ChargeStatus.Cancelled;
                charge.CancellationNote = $"Rezervasyon iptal edildi: {reason}";
            }
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.Description = $"İptal: {reason}";

        await uow.SaveChangesAsync();
    }

    // ── Tahakkuka Aktar (8.6.2) ──────────────────────────────────────────────

    public async Task<int> TransferToChargeAsync(
        TransferReservationToChargeInput input)
    {
        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");

        EnsureScope(reservation.Unit.PropertyId, reservation.UnitId, input.AccessScope);
        Guard.Conflict(
            reservation.Status != ReservationStatus.Planned,
            "Yalnızca planlanmış rezervasyonlar tahakkuka aktarılabilir.",
            "RESERVATION_NOT_PLANNED");
        Guard.Conflict(
            await chargeRepository.ExistsForReservationAsync(reservation.Id),
            "Bu rezervasyon zaten tahakkuka aktarılmış.",
            "RESERVATION_ALREADY_TRANSFERRED");
        Guard.Against(
            reservation.TotalAmount <= 0,
            "Ücretsiz rezervasyonlar için tahakkuk oluşturulamaz.",
            "RESERVATION_FREE_TRANSFER");

        var unitType = reservation.Unit.UnitType;
        var chargeType = await chargeTypeRepository.ResolveReservationTypeAsync(unitType?.ChargeTypeId);

        Guard.Against(
            chargeType == null,
            "Rezervasyon borç tipi bulunamadı. Lütfen yöneticinize başvurun.",
            "RESERVATION_CHARGE_TYPE_NOT_FOUND");

        var description = $"Toplantı salonu: {reservation.Unit.Name} " +
                          $"({reservation.StartDate:dd.MM.yyyy HH:mm} – {reservation.EndDate:HH:mm})";

        var lineItem = new ChargeLineItem
        {
            ChargeTypeId = chargeType!.Id,
            Description = description,
            CalculationMethod = CalculationMethod.Fixed,
            UnitValue = reservation.RateAmount,
            Multiplier = 1m,
            Amount = reservation.RateAmount,
            KdvRate = reservation.KdvRate ?? 0m,
            KdvAmount = reservation.KdvAmount ?? 0m,
            TotalAmount = reservation.TotalAmount,
            SourceType = LineItemSourceType.ReservationRule
        };

        var charge = new Charge
        {
            TenantId = reservation.TenantId,
            UnitId = reservation.UnitId,
            ReservationId = reservation.Id,
            PeriodStart = reservation.StartDate,
            PeriodEnd = reservation.EndDate,
            DueDate = reservation.EndDate.Date,
            ExpectedAmount = reservation.RateAmount,
            KdvAmount = reservation.KdvAmount ?? 0m,
            TotalAmount = reservation.TotalAmount,
            PaidAmount = 0,
            Status = ChargeStatus.Pending,
            SourceType = ChargeSourceType.Reservation,
            LineItems = new List<ChargeLineItem> { lineItem }
        };

        await chargeRepository.AddAsync(charge);
        reservation.Status = ReservationStatus.TransferredToCharge;
        await uow.SaveChangesAsync();

        return charge.Id;
    }

    // ── Ücret Kuralı CRUD ─────────────────────────────────────────────────────

    public async Task<List<ReservationRateOverrideListItemDto>> GetRateRulesAsync()
        => await reservationRateOverrideRepository.GetUcretKurallariListAsync();

    public async Task<ReservationRateOverride?> GetRateRuleByIdAsync(GetRateRuleByIdInput input)
        => await reservationRateOverrideRepository.GetWithUnitAsync(input.Id);

    public async Task SaveRateRuleAsync(SaveReservationRateRuleInput input)
    {
        ReservationRateOverride? rateRule = null;
        if (input.Id != 0)
            rateRule = Guard.NotFound(
                await reservationRateOverrideRepository.GetWithUnitAsync(input.Id),
                "Kural bulunamadı.");

        var unit = (await unitRepository.GetReservableUnitsAsync())
            .FirstOrDefault(unit => unit.Id == input.UnitId);
        Guard.InvalidField(
            unit == null,
            nameof(input.UnitId),
            "Seçilen birim rezervasyona uygun değil veya bulunamadı.");
        var validUnit = unit!;

        var existingUnitRule = await reservationRateOverrideRepository.GetForUnitAsync(validUnit.Id);
        Guard.InvalidField(
            existingUnitRule != null && existingUnitRule.Id != input.Id,
            nameof(input.UnitId),
            "Bu birim için zaten ücret kuralı mevcut.");

        if (rateRule == null)
        {
            rateRule = new ReservationRateOverride();
            await reservationRateOverrideRepository.AddAsync(rateRule);
        }

        rateRule.UnitId = validUnit.Id;
        rateRule.FreeDurationMinutes = input.FreeDurationMinutes;
        rateRule.BillingPeriodMinutes = input.BillingPeriodMinutes;
        rateRule.PeriodRate = input.PeriodRate;
        rateRule.KdvRate = input.KdvRate;
        rateRule.IsActive = input.IsActive;
        rateRule.Description = input.Description;

        await uow.SaveChangesAsync();
    }

    public async Task SaveUnitReservationRateRuleAsync(
        SaveUnitReservationRateRuleInput input)
    {
        var unit = Guard.NotFound(
            await unitRepository.GetReservationContextAsync(input.UnitId),
            "Birim bulunamadı.",
            "UNIT_RESERVATION_RATE_UNIT_NOT_FOUND");
        EnsureUnitRateScope(unit.PropertyId, unit.UnitId, input.AccessScope);
        EnsureReservableUnit(unit);

        ReservationRateOverride? rateRule = null;
        if (input.Id != 0)
        {
            rateRule = Guard.NotFound(
                await reservationRateOverrideRepository.GetWithUnitAsync(input.Id),
                "Kural bulunamadı.",
                "UNIT_RESERVATION_RATE_RULE_NOT_FOUND");
            Guard.Forbidden(
                rateRule.UnitId != input.UnitId,
                "Bu ücret kuralı seçilen birime ait değil.",
                "UNIT_RESERVATION_RATE_FOREIGN_RULE");
        }

        var existingUnitRule = await reservationRateOverrideRepository.GetForUnitAsync(input.UnitId);
        Guard.Conflict(
            existingUnitRule != null && existingUnitRule.Id != input.Id,
            "Bu birim için zaten ücret kuralı mevcut.",
            "UNIT_RESERVATION_RATE_RULE_EXISTS");

        if (rateRule == null)
        {
            rateRule = new ReservationRateOverride { UnitId = input.UnitId };
            await reservationRateOverrideRepository.AddAsync(rateRule);
        }

        rateRule.FreeDurationMinutes = input.FreeDurationMinutes;
        rateRule.BillingPeriodMinutes = input.BillingPeriodMinutes;
        rateRule.PeriodRate = input.PeriodRate;
        rateRule.KdvRate = input.KdvRate;
        rateRule.IsActive = input.IsActive;
        rateRule.Description = input.Description;

        await uow.SaveChangesAsync();
    }

    public async Task ToggleRateRuleStatusAsync(ToggleRateRuleStatusInput input)
    {
        var rateRule = Guard.NotFound(
            await reservationRateOverrideRepository.GetWithUnitAsync(input.Id),
            "Kural bulunamadı.");

        rateRule.IsActive = !rateRule.IsActive;

        await uow.SaveChangesAsync();
    }

    public async Task ClearUnitReservationRateRuleAsync(
        ClearUnitReservationRateRuleInput input)
    {
        var unit = Guard.NotFound(
            await unitRepository.GetReservationContextAsync(input.UnitId),
            "Birim bulunamadı.",
            "UNIT_RESERVATION_RATE_UNIT_NOT_FOUND");
        EnsureUnitRateScope(unit.PropertyId, unit.UnitId, input.AccessScope);

        var rateRule = Guard.NotFound(
            await reservationRateOverrideRepository.GetForUnitAsync(input.UnitId),
            "Kural bulunamadı.",
            "UNIT_RESERVATION_RATE_RULE_NOT_FOUND");
        reservationRateOverrideRepository.Remove(rateRule);
        await uow.SaveChangesAsync();
    }

    private static void EnsureScope(
        int propertyId,
        int unitId,
        ReservationAccessScopeInput accessScope)
    {
        if (accessScope.PropertyIds == null && accessScope.UnitIds == null)
            return;

        var hasPropertyAccess = accessScope.PropertyIds?.Contains(propertyId) == true;
        var hasUnitAccess = accessScope.UnitIds?.Contains(unitId) == true;
        Guard.Forbidden(
            !hasPropertyAccess && !hasUnitAccess,
            "Bu rezervasyon yetki kapsamınızın dışındadır.",
            "RESERVATION_OUT_OF_SCOPE");
    }

    private static void EnsureUnitRateScope(
        int propertyId,
        int unitId,
        ReservationAccessScopeInput accessScope)
    {
        if (accessScope.PropertyIds == null && accessScope.UnitIds == null)
            return;

        var hasPropertyAccess = accessScope.PropertyIds?.Contains(propertyId) == true;
        var hasUnitAccess = accessScope.UnitIds?.Contains(unitId) == true;
        Guard.Forbidden(
            !hasPropertyAccess && !hasUnitAccess,
            "Bu birimin rezervasyon ücret kuralını değiştirme yetkiniz bulunmuyor.",
            "UNIT_RESERVATION_RATE_OUT_OF_SCOPE");
    }

    private static void EnsureReservableUnit(ReservationUnitContextDto unit)
        => Guard.Against(
            !unit.IsUnitActive
            || !unit.IsUnitTypeActive
            || unit.Usage != UnitTypeUsage.Reservable,
            "Seçilen birim aktif veya rezervasyona uygun değildir.",
            "RESERVATION_UNIT_NOT_RESERVABLE");
}
