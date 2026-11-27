using KiraTakip.Data;
using KiraTakip.Domain.Charges;
using KiraTakip.Domain.Reservations;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Reservation;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Charges;
using KiraTakip.Repositories.Interfaces.Reservations;
using KiraTakip.Repositories.Interfaces.Tenants;
using KiraTakip.Services.Interfaces.Reservations;

namespace KiraTakip.Services.Reservations;

public class ReservationService(
    IReservationRepository reservationRepository,
    IReservationRateOverrideRepository reservationRateOverrideRepository,
    IChargeRepository chargeRepository,
    IChargeTypeRepository chargeTypeRepository,
    ITenantRepository tenantRepository,
    IReservationBusinessRules reservationBusinessRules,
    IUnitOfWork uow,
    IConcurrencyViolationDetector concurrencyDetector) : IReservationService, ITransactionalService
{

    // ── Listeleme ──────────────────────────────────────────────────────────────

    public async Task<List<ReservationListItemDto>> GetAllAsync(GetReservationsInput input)
    {
        return await reservationRepository.GetListAsync(
            input.Scope);
    }

    public Task<PagedResult<ReservationListItemDto>> GetPageAsync(GetReservationsPageInput input)
        => reservationRepository.GetPagedListAsync(
            input.Query,
            input.Scope);

    public Task<int> GetCancelledCountAsync(GetCancelledReservationCountInput input)
        => reservationRepository.GetCancelledCountAsync(
            input.Scope);

    public async Task<List<ReservationListItemDto>> GetTenantReservationsAsync(
        GetTenantReservationsInput input)
    {
        Guard.NotFound(
            await tenantRepository.GetDetailsAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "TENANT_RESERVATION_TENANT_NOT_FOUND");

        return await reservationRepository.GetTenantListAsync(
            input.TenantId,
            input.AccessScope);
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
            input.AccessScope);
    }

    public async Task<ReservationListItemDto> GetByIdAsync(GetReservationByIdInput input)
    {
        var reservation = Guard.NotFound(
            await reservationRepository.GetByIdAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");

        reservationBusinessRules.EnsureAccessScope(
            reservation.SpacePath,
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
            reservation.SpacePath,
            input.AccessScope);
        return reservation;
    }

    public async Task<ReservationFormOptionsDto> GetFormOptionsAsync(
        GetReservationFormOptionsInput input)
        => new(await reservationRepository.GetReservableSpacesAsync(input.AccessScope),
               await tenantRepository.GetReservationOptionsAsync());

    public async Task<ReservationCalendarResultDto> GetCalendarAsync(
        GetReservationCalendarInput input)
    {
        var (fromDate, toDate) = GetWeekRange(input.AnchorDate);
        var spaces = await GetCalendarSpacesAsync(input.SpaceId, input.AccessScope);
        var items = await reservationRepository.GetCalendarItemsAsync(
            BuildCalendarQuery(fromDate, toDate, input.SpaceId, input.AccessScope));

        return new ReservationCalendarResultDto(
            fromDate,
            toDate.AddDays(-1),
            input.SpaceId,
            spaces,
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
        var spaces = await GetCalendarSpacesAsync(input.SpaceId, input.AccessScope);
        var items = await reservationRepository.GetTenantCalendarItemsAsync(
            input.TenantId,
            BuildCalendarQuery(fromDate, toDate, input.SpaceId, input.AccessScope));

        return new TenantReservationCalendarResultDto(
            fromDate,
            toDate.AddDays(-1),
            input.SpaceId,
            spaces,
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

        var space = Guard.NotFound(
            await reservationRepository.GetSpaceContextAsync(input.SpaceId),
            "Alan bulunamadı.",
            "RESERVATION_SPACE_NOT_FOUND");
        reservationBusinessRules.EnsureAccessScope(space.Path, input.AccessScope);
        reservationBusinessRules.EnsureSpaceIsReservable(space);

        var hasConflict = await reservationRepository.IsConflictAsync(
            input.SpaceId,
            input.StartDate,
            input.EndDate,
            input.ExcludedReservationId);

        return hasConflict
            ? new ReservationAvailabilityResultDto(
                false,
                "RESERVATION_TIME_CONFLICT",
                "Seçilen zaman aralığında bu alan doludur.")
            : new ReservationAvailabilityResultDto(
                true,
                "RESERVATION_AVAILABLE",
                "Seçilen zaman aralığı uygundur.");
    }

    private async Task<List<ReservationSpaceOptionDto>> GetCalendarSpacesAsync(
        int? selectedSpaceId,
        SpaceAccessScope accessScope)
    {
        var spaces = await reservationRepository.GetReservableSpacesAsync(accessScope);

        Guard.Forbidden(
            selectedSpaceId.HasValue && spaces.All(space => space.Id != selectedSpaceId.Value),
            "Bu alanın takvimini görüntüleme yetkiniz bulunmuyor.",
            "RESERVATION_CALENDAR_SPACE_OUT_OF_SCOPE");

        return spaces;
    }

    private static ReservationCalendarRepositoryQuery BuildCalendarQuery(
        DateTime fromDate,
        DateTime toDate,
        int? spaceId,
        SpaceAccessScope accessScope)
        => new(fromDate, toDate, spaceId, accessScope);

    private (DateTime FromDate, DateTime ToDate) GetWeekRange(DateTime? anchorDate)
    {
        var date = (anchorDate ?? reservationBusinessRules.GetCurrentTime()).Date;
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-daysFromMonday);
        return (monday, monday.AddDays(7));
    }

    // ── Ücret Hesaplama (alanın kendi kuralı → en yakın atanın kuralı → hata) ─

    public async Task<ReservationCalculationResultDto> CalculateAsync(CalculateReservationInput input)
    {
        reservationBusinessRules.EnsureScheduleIsValid(input.StartDate, input.EndDate);

        var space = Guard.NotFound(
            await reservationRepository.GetSpaceContextAsync(input.SpaceId),
            "Alan bulunamadı.",
            "RESERVATION_SPACE_NOT_FOUND");

        reservationBusinessRules.EnsureAccessScope(space.Path, input.AccessScope);
        reservationBusinessRules.EnsureSpaceIsReservable(space);

        return await CalculateCoreAsync(input, space);
    }

    private async Task<ReservationCalculationResultDto> CalculateCoreAsync(
        CalculateReservationInput input,
        ReservationSpaceContextDto space)
    {
        var result = new ReservationCalculationResultDto();
        var rateRule = await reservationRateOverrideRepository.GetActiveForSpaceAsync(
            space.SpaceId,
            space.Path);

        if (rateRule == null)
        {
            result.ErrorMessage = "Bu alan için rezervasyon kuralı tanımlı değil. Alanın veya üst alanlarından birinin rezervasyon kuralı olmalıdır.";
            return result;
        }

        var freeDurationMinutes = rateRule.FreeDurationMinutes;
        var billingPeriodMinutes = rateRule.BillingPeriodMinutes;
        var periodRate = rateRule.PeriodRate;
        var vatRate = rateRule.KdvRate;
        result.ChargeTypeId = rateRule.ChargeTypeId;
        result.HasRateRule = true;

        Guard.Against(
            billingPeriodMinutes <= 0,
            "Rezervasyon tarifesinin ücretlendirme periyodu geçersizdir.",
            "RESERVATION_INVALID_BILLING_PERIOD");

        var calculation = ReservationPricingPolicy.Calculate(
            input.StartDate,
            input.EndDate,
            freeDurationMinutes,
            billingPeriodMinutes,
            periodRate,
            vatRate);

        result.TotalDurationMinutes = calculation.TotalDurationMinutes;
        result.FreeDurationMinutes = calculation.FreeDurationMinutes;
        result.PaidDurationMinutes = calculation.PaidDurationMinutes;
        result.PaidPeriodCount = calculation.PaidPeriodCount;
        result.UnitRate = calculation.UnitRate;
        result.RateAmount = calculation.RateAmount;
        result.VatRate = calculation.VatRate;
        result.VatAmount = calculation.VatAmount;
        result.TotalAmount = calculation.TotalAmount;

        return result;
    }

    // ── Rezervasyon Oluşturma ─────────────────────────────────────────────────

    public async Task<int> CreateAsync(CreateReservationInput input)
    {
        reservationBusinessRules.EnsureScheduleIsValid(input.StartDate, input.EndDate);

        var space = await reservationRepository.GetSpaceContextAsync(input.SpaceId);
        Guard.InvalidField(
            space == null,
            nameof(input.SpaceId),
            "Alan bulunamadı.",
            "RESERVATION_SPACE_NOT_FOUND");

        reservationBusinessRules.EnsureAccessScope(space!.Path, input.AccessScope);
        reservationBusinessRules.EnsureSpaceIsReservable(space, fieldValidation: true);

        var tenant = await tenantRepository.GetByIdAsync(input.TenantId);
        Guard.InvalidField(
            tenant == null || !tenant.IsActive,
            nameof(input.TenantId),
            "Kiracı bulunamadı veya aktif değildir.",
            "RESERVATION_TENANT_NOT_ACTIVE");

        Guard.InvalidField(
            await reservationRepository.IsConflictAsync(input.SpaceId, input.StartDate, input.EndDate),
            nameof(input.StartDate),
            "Seçilen zaman aralığında bu alan için başka bir rezervasyon mevcut.",
            "RESERVATION_TIME_CONFLICT");

        var calculation = await CalculateCoreAsync(
            new CalculateReservationInput(
                input.SpaceId,
                input.StartDate,
                input.EndDate,
                input.AccessScope),
            space);
        Guard.InvalidField(
            !string.IsNullOrWhiteSpace(calculation.ErrorMessage),
            nameof(input.SpaceId),
            calculation.ErrorMessage ?? "Rezervasyon ücreti hesaplanamadı.",
            "RESERVATION_RATE_NOT_FOUND");

        var reservation = new Reservation
        {
            ReservationNo = await GenerateReservationNoAsync(),
            SpaceId = input.SpaceId,
            ChargeTypeId = calculation.ChargeTypeId!.Value,
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

        var space = Guard.NotFound(
            await reservationRepository.GetSpaceContextAsync(input.SpaceId),
            "Alan bulunamadı.",
            "RESERVATION_SPACE_NOT_FOUND");
        reservationBusinessRules.EnsureAccessScope(space.Path, input.AccessScope);
        reservationBusinessRules.EnsureSpaceIsReservable(space);

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
                    input.SpaceId,
                    input.StartDate,
                    input.EndDate),
                nameof(input.StartDate),
                "Seçilen zaman aralığında bu alan için onaylanmış başka bir rezervasyon mevcut.",
                "RESERVATION_TIME_CONFLICT");
        }

        var calculation = await CalculateCoreAsync(
            new CalculateReservationInput(
                input.SpaceId,
                input.StartDate,
                input.EndDate,
                input.AccessScope),
            space);
        Guard.InvalidField(
            !string.IsNullOrWhiteSpace(calculation.ErrorMessage),
            nameof(input.SpaceId),
            calculation.ErrorMessage ?? "Rezervasyon ücreti hesaplanamadı.",
            "RESERVATION_RATE_NOT_FOUND");

        var now = reservationBusinessRules.GetCurrentTime();
        var reservation = new Reservation
        {
            ReservationNo = await GenerateReservationNoAsync(),
            SpaceId = input.SpaceId,
            ChargeTypeId = calculation.ChargeTypeId!.Value,
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
            reservation.Space.Path,
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
            reservation.Space.Path,
            input.AccessScope);
        reservationBusinessRules.EnsureCancellationAllowed(
            reservation,
            canOverrideTimeRestriction: false);

        await CancelCoreAsync(reservation, input.Reason.Trim(), input.ActorUserId);
    }

    public async Task ApproveAsync(ApproveReservationInput input)
    {
        var spaceId = await reservationRepository.GetSpaceIdAsync(input.ReservationId);
        Guard.Against(
            !spaceId.HasValue,
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        await reservationRepository.AcquireSpaceDecisionLockAsync(spaceId!.Value);

        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        EnsureExpectedRowVersion(reservation, input.ExpectedRowVersion);
        reservationBusinessRules.EnsureAccessScope(
            reservation.Space.Path,
            input.AccessScope);
        reservationBusinessRules.EnsureTransitionAllowed(
            reservation.Status,
            ReservationStatus.Confirmed);
        Guard.Conflict(
            await reservationRepository.IsConflictAsync(
                reservation.SpaceId,
                reservation.StartDate,
                reservation.EndDate,
                reservation.Id),
            "Bu zaman aralığında alan için onaylanmış başka bir rezervasyon bulunuyor.",
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
            reservation.Space.Path,
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

        var currentSpaceId = await reservationRepository.GetSpaceIdAsync(input.ReservationId);
        Guard.Against(
            !currentSpaceId.HasValue,
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        foreach (var spaceId in new[] { currentSpaceId!.Value, input.SpaceId }.Distinct().Order())
            await reservationRepository.AcquireSpaceDecisionLockAsync(spaceId);

        var reservation = Guard.NotFound(
            await reservationRepository.GetForOperationAsync(input.ReservationId),
            "Rezervasyon bulunamadı.",
            "RESERVATION_NOT_FOUND");
        EnsureExpectedRowVersion(reservation, input.ExpectedRowVersion);
        reservationBusinessRules.EnsureAccessScope(
            reservation.Space.Path,
            input.AccessScope);
        reservationBusinessRules.EnsureModificationAllowed(
            reservation,
            input.CanOverrideTimeRestriction,
            input.OverrideReason);

        var targetSpace = Guard.NotFound(
            await reservationRepository.GetSpaceContextAsync(input.SpaceId),
            "Alan bulunamadı.",
            "RESERVATION_SPACE_NOT_FOUND");
        reservationBusinessRules.EnsureAccessScope(targetSpace.Path, input.AccessScope);
        reservationBusinessRules.EnsureSpaceIsReservable(targetSpace);

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

        if (ReservationLifecycle.BlocksAvailability(reservation.Status))
        {
            Guard.InvalidField(
                await reservationRepository.IsConflictAsync(
                    input.SpaceId,
                    input.StartDate,
                    input.EndDate,
                    reservation.Id),
                nameof(input.StartDate),
                "Seçilen zaman aralığında bu alan için onaylanmış başka bir rezervasyon mevcut.",
                "RESERVATION_TIME_CONFLICT");
        }

        var calculation = await CalculateCoreAsync(
            new CalculateReservationInput(
                input.SpaceId,
                input.StartDate,
                input.EndDate,
                input.AccessScope),
            targetSpace);
        Guard.InvalidField(
            !string.IsNullOrWhiteSpace(calculation.ErrorMessage),
            nameof(input.SpaceId),
            calculation.ErrorMessage ?? "Rezervasyon ücreti hesaplanamadı.",
            "RESERVATION_RATE_NOT_FOUND");

        reservation.SpaceId = input.SpaceId;
        reservation.ChargeTypeId = calculation.ChargeTypeId!.Value;
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

    private async Task<string> GenerateChargeNoAsync()
    {
        var existingChargeNos = await chargeRepository.GetExistingChargeNosAsync();
        var usedChargeNos = existingChargeNos.ToHashSet();

        if (ChargeNumberPolicy.TryGenerateNextChargeNo(usedChargeNos, out var chargeNo))
            return chargeNo;

        throw new BusinessException(
            "Tahakkuk No üretilemedi.",
            ErrorType.Failure,
            "Charge.NumberGenerationFailed");
    }

    private async Task<string> GenerateReservationNoAsync()
    {
        var existingReservationNos = await reservationRepository.GetExistingReservationNosAsync();
        var usedReservationNos = existingReservationNos.ToHashSet();

        if (ReservationNumberPolicy.TryGenerateNextReservationNo(usedReservationNos, out var reservationNo))
            return reservationNo;

        throw new BusinessException(
            "Rezervasyon No üretilemedi.",
            ErrorType.Failure,
            "Reservation.NumberGenerationFailed");
    }

    private async Task SaveDecisionAsync()
    {
        try
        {
            await uow.SaveChangesAsync();
        }
        catch (Exception exception) when (concurrencyDetector.IsConcurrencyViolation(exception))
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
            reservation.Space.Path,
            input.AccessScope);
        Guard.Conflict(
            !ReservationLifecycle.CanTransferToCharge(reservation.Status),
            "Yalnızca onaylanmış rezervasyonlar tahakkuka aktarılabilir.",
            "RESERVATION_NOT_CONFIRMED");
        Guard.Conflict(
            await chargeRepository.ExistsForReservationAsync(reservation.Id),
            "Bu rezervasyon zaten tahakkuka aktarılmış.",
            "RESERVATION_ALREADY_TRANSFERRED");
        Guard.Against(
            !ReservationPricingPolicy.HasChargeableAmount(reservation.TotalAmount),
            "Ücretsiz rezervasyonlar için tahakkuk oluşturulamaz.",
            "RESERVATION_FREE_TRANSFER");

        var description = $"Toplantı salonu: {reservation.Space.Name} " +
                          $"({reservation.StartDate:dd.MM.yyyy HH:mm} – {reservation.EndDate:HH:mm})";

        var lineItem = new ChargeLineItem
        {
            ChargeTypeId = reservation.ChargeTypeId,
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
            ChargeNo = await GenerateChargeNoAsync(),
            TenantId = reservation.TenantId,
            SpaceId = reservation.SpaceId,
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

    public async Task<List<ReservationSpaceOptionDto>> GetRateRuleSpaceOptionsAsync(SpaceAccessScope scope)
    {
        var options = await reservationRepository.GetActiveSpaceOptionsAsync();
        return options.Where(option => scope.Includes(option.Path)).ToList();
    }

    private static void EnsureRateRuleSpaceInScope(string spacePath, SpaceAccessScope scope)
        => Guard.Forbidden(
            !scope.Includes(spacePath),
            "Bu rezervasyon kuralının alanı yetki kapsamınızın dışındadır.",
            "RESERVATION_RATE_SPACE_FORBIDDEN");

    public Task<List<ChargeTypeLookupDto>> GetRateRuleChargeTypeOptionsAsync()
        => chargeTypeRepository.GetRezervasyonAdaylariAsync();

    public Task<PagedResult<ReservationRateOverrideListItemDto>> GetRateRulesPagedAsync(TableQuery query, SpaceAccessScope scope)
        => reservationRateOverrideRepository.GetRateRulesPagedAsync(query, scope);

    public async Task<ReservationRateOverride?> GetRateRuleByIdAsync(GetRateRuleByIdInput input)
    {
        var rateRule = await reservationRateOverrideRepository.GetWithSpaceAsync(input.Id);
        if (rateRule != null)
            EnsureRateRuleSpaceInScope(rateRule.Space.Path, input.AccessScope);
        return rateRule;
    }

    public async Task SaveRateRuleAsync(SaveReservationRateRuleInput input)
    {
        ReservationRateOverride? rateRule = null;
        if (input.Id != 0)
            rateRule = Guard.NotFound(
                await reservationRateOverrideRepository.GetWithSpaceAsync(input.Id),
                "Kural bulunamadı.");
        if (rateRule != null)
            EnsureRateRuleSpaceInScope(rateRule.Space.Path, input.AccessScope);

        var space = await reservationRepository.GetSpaceContextAsync(input.SpaceId);
        Guard.InvalidField(
            space == null || !space.IsSpaceActive,
            nameof(input.SpaceId),
            "Seçilen alan aktif değil veya bulunamadı.");
        EnsureRateRuleSpaceInScope(space!.Path, input.AccessScope);

        var existingSpaceRule = await reservationRateOverrideRepository.GetForSpaceAsync(input.SpaceId);
        Guard.InvalidField(
            existingSpaceRule != null && existingSpaceRule.Id != input.Id,
            nameof(input.SpaceId),
            "Bu alan için zaten rezervasyon kuralı mevcut.");

        Guard.InvalidField(
            !await chargeTypeRepository.IsActiveReservationSpecificAsync(input.ChargeTypeId),
            nameof(input.ChargeTypeId),
            "Borç tipi aktif ve rezervasyona özel olmalıdır.",
            "RESERVATION_RATE_CHARGE_TYPE_INVALID");

        Guard.InvalidField(
            !ReservationRatePolicy.IsValid(
                input.FreeDurationMinutes,
                input.BillingPeriodMinutes,
                input.PeriodRate,
                input.KdvRate),
            nameof(input.BillingPeriodMinutes),
            "Rezervasyon ücret kuralı değerleri geçersiz.");

        if (rateRule == null)
        {
            rateRule = new ReservationRateOverride();
            await reservationRateOverrideRepository.AddAsync(rateRule);
        }

        rateRule.SpaceId = input.SpaceId;
        rateRule.ChargeTypeId = input.ChargeTypeId;
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
            await reservationRateOverrideRepository.GetWithSpaceAsync(input.Id),
            "Kural bulunamadı.");
        EnsureRateRuleSpaceInScope(rateRule.Space.Path, input.AccessScope);

        if (!rateRule.IsActive)
        {
            var space = await reservationRepository.GetSpaceContextAsync(rateRule.SpaceId);
            Guard.Conflict(
                space == null || !space.IsSpaceActive,
                "Seçilen alan aktif değil veya bulunamadı.",
                "RESERVATION_RATE_SPACE_NOT_ACTIVE");
            Guard.Conflict(
                !await chargeTypeRepository.IsActiveReservationSpecificAsync(rateRule.ChargeTypeId),
                "Kuralın borç tipi aktif ve rezervasyona özel olmadığı için kural aktifleştirilemez.",
                "RESERVATION_RATE_CHARGE_TYPE_INVALID");
        }

        rateRule.IsActive = !rateRule.IsActive;

        await uow.SaveChangesAsync();
    }
}
