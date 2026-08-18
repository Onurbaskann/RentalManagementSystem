namespace KiraTakip.Models.ViewModels;

public class ApproveReservationViewModel
{
    public byte[] RowVersion { get; set; } = [];
}

public class RejectReservationViewModel
{
    public string? Reason { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
