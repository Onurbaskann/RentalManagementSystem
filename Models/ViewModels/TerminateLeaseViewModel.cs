namespace KiraTakip.Models.ViewModels;

public class TerminateLeaseViewModel
{
    public int LeaseId { get; set; }
    public DateTime TerminationDate { get; set; } = DateTime.Today;
    public string TerminationReason { get; set; } = string.Empty;
    public string? Description { get; set; }
}
