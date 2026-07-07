namespace KiraTakip.Models.Dtos;

public class LeaseActivityLogDto
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public LeaseActivityType ActivityType { get; set; }
    public string? Description { get; set; }
    public decimal? OldRentAmount { get; set; }
    public decimal? NewRentAmount { get; set; }
    public DateTime? OldEndDate { get; set; }
    public DateTime? NewEndDate { get; set; }
    public decimal? InflationRate { get; set; }
    public bool IsKdvApplied { get; set; }
    public decimal? KdvRate { get; set; }
    public decimal? KdvAmount { get; set; }
    public decimal? KdvIncludedAmount { get; set; }
}
