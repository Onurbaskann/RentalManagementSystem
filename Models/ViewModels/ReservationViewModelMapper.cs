using KiraTakip.Models.Dtos;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Models.ViewModels;

public static class ReservationViewModelMapper
{
    public static CalculateReservationInput ToInput(
        this ReservationCalculationQueryViewModel viewModel,
        ReservationAccessScopeInput accessScope)
        => new(
            viewModel.UnitId,
            DateTime.Parse(viewModel.Start!),
            DateTime.Parse(viewModel.End!),
            accessScope);

    public static CreateReservationRequestInput ToInput(
        this ReservationCreateViewModel viewModel,
        ICurrentUserContext currentUser,
        ReservationAccessScopeInput accessScope,
        bool createAndApprove)
        => MapCreate(
            viewModel.UnitId!.Value,
            viewModel.TenantId!.Value,
            viewModel.StartDate,
            viewModel.EndDate,
            viewModel.Title!,
            viewModel.Description,
            viewModel.Notes,
            viewModel.InternalNotes,
            viewModel.Attendees,
            createAndApprove,
            currentUser,
            accessScope);

    public static CreateReservationRequestInput ToInput(
        this TenantReservationCreateViewModel viewModel,
        ICurrentUserContext currentUser,
        ReservationAccessScopeInput accessScope)
        => MapCreate(
            viewModel.UnitId!.Value,
            currentUser.TenantId!.Value,
            viewModel.StartDate,
            viewModel.EndDate,
            viewModel.Title!,
            viewModel.Description,
            viewModel.Notes,
            null,
            viewModel.Attendees,
            false,
            currentUser,
            accessScope);

    private static CreateReservationRequestInput MapCreate(
        int unitId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        string title,
        string? description,
        string? notes,
        string? internalNotes,
        IReadOnlyList<ReservationAttendeeInputViewModel> attendees,
        bool createAndApprove,
        ICurrentUserContext currentUser,
        ReservationAccessScopeInput accessScope)
        => new(
            unitId,
            tenantId,
            startDate,
            endDate,
            title,
            description,
            notes,
            internalNotes,
            attendees
                .Where(attendee => !string.IsNullOrWhiteSpace(attendee.DisplayName)
                    || !string.IsNullOrWhiteSpace(attendee.EmailAddress))
                .Select(attendee => new ReservationAttendeePolicyInput(
                    attendee.DisplayName,
                    attendee.EmailAddress,
                    false))
                .ToList(),
            createAndApprove,
            currentUser.UserId!,
            currentUser.DisplayName ?? currentUser.EmailAddress ?? "Kullanıcı",
            currentUser.EmailAddress!,
            accessScope);

    public static UpdateReservationInput ToInput(
        this ReservationEditViewModel viewModel,
        ICurrentUserContext currentUser,
        ReservationAccessScopeInput accessScope,
        bool canOverrideTimeRestriction)
        => new(
            viewModel.Id,
            viewModel.UnitId!.Value,
            viewModel.TenantId!.Value,
            viewModel.StartDate,
            viewModel.EndDate,
            viewModel.Title!,
            viewModel.Description,
            viewModel.Notes,
            viewModel.InternalNotes,
            viewModel.Attendees
                .Where(attendee => !string.IsNullOrWhiteSpace(attendee.DisplayName)
                    || !string.IsNullOrWhiteSpace(attendee.EmailAddress))
                .Select(attendee => new ReservationAttendeePolicyInput(
                    attendee.DisplayName,
                    attendee.EmailAddress,
                    false))
                .ToList(),
            viewModel.RowVersion,
            currentUser.UserId!,
            currentUser.DisplayName ?? currentUser.EmailAddress ?? "Kullanıcı",
            currentUser.EmailAddress!,
            canOverrideTimeRestriction,
            viewModel.OverrideReason,
            accessScope);
}
