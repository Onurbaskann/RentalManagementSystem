namespace KiraTakip.Models.Dtos;

public class PaymentAllocationDto
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentChannel PaymentChannel { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime EntryDate { get; set; }
    public string? Description { get; set; }
    public string? RejectionReason { get; set; }
}
