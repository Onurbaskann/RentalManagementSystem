using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class ReservationService(
    IReservationRepository reservationRepository,
    IReservationRateOverrideRepository reservationRateOverrideRepository,
    IChargeRepository chargeRepository,
    IChargeTypeRepository chargeTypeRepository,
    IUnitRepository unitRepository,
    ITenantRepository tenantRepository,
    IReservationBusinessRules reservationBusinessRules,
    IUnitOfWork uow) : IReservationService, ITransactionalService
{

    // ── Listeleme ──────────────────────────────────────────────────────────────

    public async Task<List<ReservationListItemDto>> GetAllAsync(GetReservationsInput input)
    {
        return await reservationRepository.GetListAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());
    }

    public Task<PagedResult<ReservationListItemDto>> GetPageAsync(GetReservationsPageInput input)
        => reservationRepository.GetPagedListAsync(
            input.Query,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public Task<int> GetCancelledCountAsync(GetCancelledReservationCountInput input)
        => reservationRepository.GetCancelledCountAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public async Task<List<ReservationListItemDto>> GetTenantReservationsAsync(
        GetTenantReservationsInput input)
    {
        Guard.NotFound(
            await tenantRepository.GetDetailsAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_RESERVATION_TENANT_NOT_FOUND");

        return await reservationRepository.GetTenantListAsync(
            input.TenantId,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());
    }

    public async Task<PagedResult<ReservationListItemDto>> GetTenantReservationsPageAsync(
        GetTenantReservationsPageInput input)
    {
        Guard.NotFound(
            await tenantRepository.GetDetailsAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_RESERVATION_TENANT_NOT_FOUND");

        return await reservationRepository.GetTenantPagedListAsync(
            input.TenantId,
            input.Query,
            input.AccessScope.PropertyIds?.ToList(),
            input.AccessScope.UnitIds?.ToList());
    }

    public async Task<ReservationListItemDto> GetByIdAsync(GetReservationByIdInput input)
    {
        var reservation = Guard.NotFound(
            await reservationRepository.GetByIdAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");

        reservationBusinessRules.EnsureAccessScope(
            reservation.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        return reservation;
    }

    public async Task<ReservationListItemDto> GetTenantByIdAsync(
        GetTenantReservationByIdInput input)
    {
        var reservation = Guard.NotFound(
            await reservationRepository.GetByIdAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        reservationBusinessRules.EnsureTenantOwnership(reservation.TenantId, input.TenantId);
        reservationBusinessRules.EnsureAccessScope(
            reservation.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        return reservation;
    }

    public async Task<ReservationFormOptionsDto> GetFormOptionsAsync(
        GetReservationFormOptionsInput input)
        => new(await unitRepository.GetReservableUnitsAsync(
                   input.AccessScope.PropertyIds?.ToList(),
                   input.AccessScope.UnitIds?.ToList()),
               await tenantRepository.GetReservationOptionsAsync());

    public async Task<ReservationCalendarResultDto> GetCalendarAsync(
        GetReservationCalendarInput input)
    {
        var (fromDate, toDate) = GetWeekRange(input.AnchorDate);
        var units = await GetCalendarUnitsAsync(input.UnitId, input.AccessScope);
        var items = await reservationRepository.GetCalendarItemsAsync(
            BuildCalendarQuery(fromDate, toDate, input.UnitId, input.AccessScope));

        return new ReservationCalendarResultDto(
            fromDate,
            toDate.AddDays(-1),
            input.UnitId,
            units,
            items);
    }

    public async Task<TenantReservationCalendarResultDto> GetTenantCalendarAsync(
        GetTenantReservationCalendarInput input)
    {
        Guard.NotFound(
            await tenantRepository.GetDetailsAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_RESERVATION_TENANT_NOT_FOUND");

        var (fromDate, toDate) = GetWeekRange(input.AnchorDate);
        var units = await GetCalendarUnitsAsync(input.UnitId, input.AccessScope);
        var items = await reservationRepository.GetTenantCalendarItemsAsync(
            input.TenantId,
            BuildCalendarQuery(fromDate, toDate, input.UnitId, input.AccessScope));

        return new TenantReservationCalendarResultDto(
            fromDate,
            toDate.AddDays(-1),
            input.UnitId,
            units,
            items);
    }

    public async Task<ReservationAvailabilityResultDto> CheckAvailabilityAsync(
        CheckReservationAvailabilityInput input)
    {
        try
        {
            reservationBusinessRules.EnsureScheduleIsValid(input.StartDate, input.EndDate);
        }
        catch (BusinessException exception) when (exception.ErrorType == ErrorType.Failure)
        {
            return new ReservationAvailabilityResultDto(
                false,
                exception.Code ?? "RESERVATION_POLICY_RESTRICTION",
                exception.Message);
        }

        var unit = Guard.NotFound(
            await unitRepository.GetReservationContextAsync(input.UnitId),
            "Birim bulunamadı.",
            "RESERVATION_UNIT_NOT_FOUND");
        reservationBusinessRules.EnsureAccessScope(unit.PropertyId, unit.UnitId, input.AccessScope);
        reservationBusinessRules.EnsureUnitIsReservable(unit);

        var hasConflict = await reservationRepository.IsConflictAsync(
            input.UnitId,
            input.StartDate,
            input.EndDate,
            input.ExcludedReservationId);

        return hasConflict
            ? new ReservationAvailabilityResultDto(
                false,
                "RESERVATION_TIME_CONFLICT",
                "Seçilen zaman aralığında bu birim doludur.")
            : new ReservationAvailabilityResultDto(
                true,
                "RESERVATION_AVAILABLE",
                "Seçilen zaman aralığı uygundur.");
    }

    private async Task<List<UnitListItemDto>> GetCalendarUnitsAsync(
        int? selectedUnitId,
        ReservationAccessScopeInput accessScope)
    {
        var units = await unitRepository.GetReservableUnitsAsync(
            accessScope.PropertyIds?.ToList(),
            accessScope.UnitIds?.ToList());

        Guard.Forbidden(
            selectedUnitId.HasValue && units.All(unit => unit.Id != selectedUnitId.Value),
            "Bu birimin takvimini görüntüleme yetkiniz bulunmuyor.",
            "RESERVATION_CALENDAR_UNIT_OUT_OF_SCOPE");

        return units;
    }

    private static ReservationCalendarRepositoryQuery BuildCalendarQuery(
        DateTime fromDate,
        DateTime toDate,
        int? unitId,
        ReservationAccessScopeInput accessScope)
        => new(
            fromDate,
            toDate,
            unitId,
            accessScope.PropertyIds,
            accessScope.UnitIds);

    private (DateTime FromDate, DateTime ToDate) GetWeekRange(DateTime? anchorDate)
    {
        var date = (anchorDate ?? reservationBusinessRules.GetCurrentTime()).Date;
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-daysFromMonday);
        return (monday, monday.AddDays(7));
    }

    // ── Ücret Hesaplama (öncelik: birime özel → birim türü genel tarife → hata) ─

    public async Task<ReservationCalculationResultDto> CalculateAsync(CalculateReservationInput input)
    {
        reservationBusinessRules.EnsureScheduleIsValid(input.StartDate, input.EndDate);

        var unit = Guard.NotFound(
            await unitRepository.GetReservationContextAsync(input.UnitId),
            "Birim bulunamadı.",
            "RESERVATION_UNIT_NOT_FOUND");

        reservationBusinessRules.EnsureAccessScope(unit.PropertyId, unit.UnitId, input.AccessScope);
        reservationBusinessRules.EnsureUnitIsReservable(unit);

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
        reservationBusinessRules.EnsureScheduleIsValid(input.StartDate, input.EndDate);

        var unit = await unitRepository.GetReservationContextAsync(input.UnitId);
        Guard.InvalidField(
            unit == null,
            nameof(input.UnitId),
            "Birim bulunamadı.",
            "RESERVATION_UNIT_NOT_FOUND");

        reservationBusinessRules.EnsureAccessScope(unit!.PropertyId, unit.UnitId, input.AccessScope);
        reservationBusinessRules.EnsureUnitIsReservable(unit, fieldValidation: true);

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
            Status = ReservationStatus.Confirmed,
            Description = input.Description,
        };

        await reservationRepository.AddAsync(reservation);
        await uow.SaveChangesAsync();

        return reservation.Id;
    }

    public async Task<int> CreateRequestAsync(CreateReservationRequestInput input)
    {
        reservationBusinessRules.EnsureScheduleIsValid(input.StartDate, input.EndDate);
        Guard.Against(
            string.IsNullOrWhiteSpace(input.RequestedByUserId)
                || string.IsNullOrWhiteSpace(input.RequestedByEmailAddress),
            "Talep sahibi kullanıcı bilgisi doğrulanamadı.",
            "RESERVATION_REQUESTER_NOT_RESOLVED");

        var attendeeInputs = new List<ReservationAttendeePolicyInput>
        {
            new(
                input.RequestedByDisplayName,
                input.RequestedByEmailAddress,
                true)
        };
        attendeeInputs.AddRange(input.Attendees);
        reservationBusinessRules.EnsureContentIsValid(new ReservationContentPolicyInput(
            input.Title,
            input.Description,
            input.Notes,
            input.InternalNotes,
            attendeeInputs));

        var unit = Guard.NotFound(
            await unitRepository.GetReservationContextAsync(input.UnitId),
            "Birim bulunamadı.",
            "RESERVATION_UNIT_NOT_FOUND");
        reservationBusinessRules.EnsureAccessScope(unit.PropertyId, unit.UnitId, input.AccessScope);
        reservationBusinessRules.EnsureUnitIsReservable(unit);

        var tenant = await tenantRepository.GetByIdAsync(input.TenantId);
        Guard.InvalidField(
            tenant == null || !tenant.IsActive,
            nameof(input.TenantId),
            "Kiracı bulunamadı veya aktif değildir.",
            "RESERVATION_TENANT_NOT_ACTIVE");

        if (input.CreateAndApprove)
        {
            Guard.InvalidField(
                await reservationRepository.IsConflictAsync(
                    input.UnitId,
                    input.StartDate,
                    input.EndDate),
                nameof(input.StartDate),
                "Seçilen zaman aralığında bu birim için onaylanmış başka bir rezervasyon mevcut.",
                "RESERVATION_TIME_CONFLICT");
        }

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

        var now = reservationBusinessRules.GetCurrentTime();
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
            KdvRate = calculation.VatRate,
            KdvAmount = calculation.VatAmount,
            TotalAmount = calculation.TotalAmount,
            Status = input.CreateAndApprove
                ? ReservationStatus.Confirmed
                : ReservationStatus.PendingApproval,
            Title = input.Title.Trim(),
            Description = input.Description?.Trim(),
            Notes = input.Notes?.Trim(),
            InternalNotes = input.InternalNotes?.Trim(),
            RequestedByUserId = input.RequestedByUserId,
            RequestedByDisplayNameSnapshot = input.RequestedByDisplayName.Trim(),
            RequestedByEmailSnapshot = input.RequestedByEmailAddress.Trim(),
            ApprovedByUserId = input.CreateAndApprove ? input.RequestedByUserId : null,
            ApprovedAt = input.CreateAndApprove ? now : null,
            Attendees = attendeeInputs.Select(attendee => new ReservationAttendee
            {
                DisplayName = attendee.DisplayName!.Trim(),
                EmailAddress = attendee.EmailAddress!.Trim(),
                NormalizedEmailAddress = attendee.EmailAddress.Trim().ToUpperInvariant(),
                IsReservationOwner = attendee.IsReservationOwner
            }).ToList()
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

        reservationBusinessRules.EnsureAccessScope(
            reservation.Unit.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        reservationBusinessRules.EnsureCancellationAllowed(
            reservation,
            input.CanOverrideTimeRestriction);

        await CancelCoreAsync(reservation, input.Reason.Trim(), input.ActorUserId);
    }

    public async Task CancelTenantAsync(CancelTenantReservationInput input)
    {
        Guard.Against(
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 450,
            "İptal nedeni zorunlu ve en fazla 450 karakter olmalıdır.",
            "RESERVATION_INVALID_CANCELLATION_REASON");

        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        reservationBusinessRules.EnsureTenantOwnership(reservation.TenantId, input.TenantId);
        reservationBusinessRules.EnsureAccessScope(
            reservation.Unit.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        reservationBusinessRules.EnsureCancellationAllowed(
            reservation,
            canOverrideTimeRestriction: false);

        await CancelCoreAsync(reservation, input.Reason.Trim(), input.ActorUserId);
    }

    public async Task ApproveAsync(ApproveReservationInput input)
    {
        var unitId = await reservationRepository.GetUnitIdAsync(input.ReservationId);
        Guard.Against(
            !unitId.HasValue,
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        await reservationRepository.AcquireUnitDecisionLockAsync(unitId!.Value);

        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        EnsureExpectedRowVersion(reservation, input.ExpectedRowVersion);
        reservationBusinessRules.EnsureAccessScope(
            reservation.Unit.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        reservationBusinessRules.EnsureTransitionAllowed(
            reservation.Status,
            ReservationStatus.Confirmed);
        Guard.Conflict(
            await reservationRepository.IsConflictAsync(
                reservation.UnitId,
                reservation.StartDate,
                reservation.EndDate,
                reservation.Id),
            "Bu zaman aralığında birim için onaylanmış başka bir rezervasyon bulunuyor.",
            "RESERVATION_APPROVAL_TIME_CONFLICT");

        reservation.Status = ReservationStatus.Confirmed;
        reservation.ApprovedByUserId = input.ActorUserId;
        reservation.ApprovedAt = reservationBusinessRules.GetCurrentTime();
        await SaveDecisionAsync();
    }

    public async Task RejectAsync(RejectReservationInput input)
    {
        Guard.Against(
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 450,
            "Ret gerekçesi zorunlu ve en fazla 450 karakter olmalıdır.",
            "RESERVATION_INVALID_REJECTION_REASON");

        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        EnsureExpectedRowVersion(reservation, input.ExpectedRowVersion);
        reservationBusinessRules.EnsureAccessScope(
            reservation.Unit.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        reservationBusinessRules.EnsureTransitionAllowed(
            reservation.Status,
            ReservationStatus.Rejected);

        reservation.Status = ReservationStatus.Rejected;
        reservation.RejectionReason = input.Reason.Trim();
        reservation.RejectedByUserId = input.ActorUserId;
        reservation.RejectedAt = reservationBusinessRules.GetCurrentTime();
        await SaveDecisionAsync();
    }

    public async Task UpdateAsync(UpdateReservationInput input)
    {
        reservationBusinessRules.EnsureScheduleIsValid(input.StartDate, input.EndDate);
        Guard.Against(
            string.IsNullOrWhiteSpace(input.ActorUserId)
                || string.IsNullOrWhiteSpace(input.ActorEmailAddress),
            "İşlem yapan kullanıcı bilgisi doğrulanamadı.",
            "RESERVATION_ACTOR_NOT_RESOLVED");

        var currentUnitId = await reservationRepository.GetUnitIdAsync(input.ReservationId);
        Guard.Against(
            !currentUnitId.HasValue,
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        foreach (var unitId in new[] { currentUnitId!.Value, input.UnitId }.Distinct().Order())
            await reservationRepository.AcquireUnitDecisionLockAsync(unitId);

        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        EnsureExpectedRowVersion(reservation, input.ExpectedRowVersion);
        reservationBusinessRules.EnsureAccessScope(
            reservation.Unit.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        reservationBusinessRules.EnsureModificationAllowed(
            reservation,
            input.CanOverrideTimeRestriction,
            input.OverrideReason);

        var targetUnit = Guard.NotFound(
            await unitRepository.GetReservationContextAsync(input.UnitId),
            "Birim bulunamadı.",
            "RESERVATION_UNIT_NOT_FOUND");
        reservationBusinessRules.EnsureAccessScope(
            targetUnit.PropertyId,
            targetUnit.UnitId,
            input.AccessScope);
        reservationBusinessRules.EnsureUnitIsReservable(targetUnit);

        var tenant = await tenantRepository.GetByIdAsync(input.TenantId);
        Guard.InvalidField(
            tenant == null || !tenant.IsActive,
            nameof(input.TenantId),
            "Kiracı bulunamadı veya aktif değildir.",
            "RESERVATION_TENANT_NOT_ACTIVE");

        var owner = reservation.Attendees.FirstOrDefault(attendee => attendee.IsReservationOwner);
        var attendeeInputs = new List<ReservationAttendeePolicyInput>
        {
            new(
                owner?.DisplayName ?? reservation.RequestedByDisplayNameSnapshot ?? input.ActorDisplayName,
                owner?.EmailAddress ?? reservation.RequestedByEmailSnapshot ?? input.ActorEmailAddress,
                true)
        };
        attendeeInputs.AddRange(input.Attendees);
        reservationBusinessRules.EnsureContentIsValid(new ReservationContentPolicyInput(
            input.Title,
            input.Description,
            input.Notes,
            input.InternalNotes,
            attendeeInputs));

        if (reservation.Status == ReservationStatus.Confirmed)
        {
            Guard.InvalidField(
                await reservationRepository.IsConflictAsync(
                    input.UnitId,
                    input.StartDate,
                    input.EndDate,
                    reservation.Id),
                nameof(input.StartDate),
                "Seçilen zaman aralığında bu birim için onaylanmış başka bir rezervasyon mevcut.",
                "RESERVATION_TIME_CONFLICT");
        }

        var calculation = await CalculateCoreAsync(
            new CalculateReservationInput(
                input.UnitId,
                input.StartDate,
                input.EndDate,
                input.AccessScope),
            targetUnit);
        Guard.InvalidField(
            !string.IsNullOrWhiteSpace(calculation.ErrorMessage),
            nameof(input.UnitId),
            calculation.ErrorMessage ?? "Rezervasyon ücreti hesaplanamadı.",
            "RESERVATION_RATE_NOT_FOUND");

        reservation.UnitId = input.UnitId;
        reservation.TenantId = input.TenantId;
        reservation.StartDate = input.StartDate;
        reservation.EndDate = input.EndDate;
        reservation.Title = input.Title.Trim();
        reservation.Description = input.Description?.Trim();
        reservation.Notes = input.Notes?.Trim();
        reservation.InternalNotes = input.InternalNotes?.Trim();
        reservation.LastModificationReason = string.IsNullOrWhiteSpace(input.OverrideReason)
            ? null
            : input.OverrideReason.Trim();
        reservation.TotalDurationMinutes = calculation.TotalDurationMinutes;
        reservation.FreeDurationMinutes = calculation.FreeDurationMinutes;
        reservation.PaidDurationMinutes = calculation.PaidDurationMinutes;
        reservation.UnitRate = calculation.UnitRate;
        reservation.RateAmount = calculation.RateAmount;
        reservation.KdvRate = calculation.VatRate;
        reservation.KdvAmount = calculation.VatAmount;
        reservation.TotalAmount = calculation.TotalAmount;
        reservation.Attendees.Clear();
        reservation.Attendees.AddRange(attendeeInputs.Select(attendee => new ReservationAttendee
        {
            DisplayName = attendee.DisplayName!.Trim(),
            EmailAddress = attendee.EmailAddress!.Trim(),
            NormalizedEmailAddress = attendee.EmailAddress.Trim().ToUpperInvariant(),
            IsReservationOwner = attendee.IsReservationOwner
        }));

        await SaveDecisionAsync();
    }

    private async Task CancelCoreAsync(
        Reservation reservation,
        string reason,
        string? actorUserId)
    {

        var charge = await chargeRepository.GetByReservationWithAllocationsAsync(reservation.Id);
        var hasApprovedPayment = charge?.Allocations.Any(
            allocation => allocation.Status == PaymentStatus.Approved) ?? false;
        Guard.Conflict(
            hasApprovedPayment,
            "Ödemesi alınmış tahakkuka bağlı rezervasyon iptal edilemez.",
            "RESERVATION_HAS_APPROVED_PAYMENT");

        if (charge != null)
        {
            charge.Status = ChargeStatus.Cancelled;
            charge.CancellationNote = $"Rezervasyon iptal edildi: {reason}";
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancellationReason = reason;
        if (!string.IsNullOrWhiteSpace(actorUserId))
            reservation.CancelledByUserId = actorUserId;
        reservation.CancelledAt = reservationBusinessRules.GetCurrentTime();

        await uow.SaveChangesAsync();
    }

    private static void EnsureExpectedRowVersion(
        Reservation reservation,
        byte[] expectedRowVersion)
        => Guard.Conflict(
            expectedRowVersion.Length == 0
                || !reservation.RowVersion.SequenceEqual(expectedRowVersion),
            "Rezervasyon başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
            "RESERVATION_STALE_VERSION");

    private async Task SaveDecisionAsync()
    {
        try
        {
            await uow.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(
                "Rezervasyon başka bir kullanıcı tarafından değiştirildi. Sayfayı yenileyip tekrar deneyin.",
                ErrorType.Conflict,
                "RESERVATION_STALE_VERSION");
        }
    }

    // ── Tahakkuka Aktar (8.6.2) ──────────────────────────────────────────────

    public async Task<int> TransferToChargeAsync(
        TransferReservationToChargeInput input)
    {
        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");

        reservationBusinessRules.EnsureAccessScope(
            reservation.Unit.PropertyId,
            reservation.UnitId,
            input.AccessScope);
        Guard.Conflict(
            reservation.Status != ReservationStatus.Confirmed,
            "Yalnızca onaylanmış rezervasyonlar tahakkuka aktarılabilir.",
            "RESERVATION_NOT_CONFIRMED");
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
        await uow.SaveChangesAsync();

        return charge.Id;
    }

    // ── Ücret Kuralı CRUD ─────────────────────────────────────────────────────

    public async Task<List<ReservationRateOverrideListItemDto>> GetRateRulesAsync()
        => await reservationRateOverrideRepository.GetUcretKurallariListAsync();

    public Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery query)
        => reservationRateOverrideRepository.GetRateRulesPagedAsync(query);

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
        reservationBusinessRules.EnsureUnitIsReservable(unit);

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

}
