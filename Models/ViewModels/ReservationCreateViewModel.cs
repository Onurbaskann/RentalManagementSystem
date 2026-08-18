using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class ReservationCreateViewModel
{
    public int? UnitId { get; set; }
    public int? TenantId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddHours(2);
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public bool CreateAndApprove { get; set; }
    public List<ReservationAttendeeInputViewModel> Attendees { get; set; } = [];
    public List<UnitListItemDto> Units { get; set; } = [];
    public List<ReservationTenantOptionDto> Tenants { get; set; } = [];
}

public class TenantReservationCreateViewModel
{
    public int? UnitId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today.AddHours(2);
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public List<ReservationAttendeeInputViewModel> Attendees { get; set; } = [];
    public List<UnitListItemDto> Units { get; set; } = [];
}

public class ReservationAttendeeInputViewModel
{
    public string? DisplayName { get; set; }
    public string? EmailAddress { get; set; }
}

public class ReservationEditViewModel
{
    public int Id { get; set; }
    public int? UnitId { get; set; }
    public int? TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? OverrideReason { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public List<ReservationAttendeeInputViewModel> Attendees { get; set; } = [];
    public List<UnitListItemDto> Units { get; set; } = [];
    public List<ReservationTenantOptionDto> Tenants { get; set; } = [];
}
