namespace KiraTakip.Models.Dtos;

public class PaymentCandidateDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentStatus Status { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
}
