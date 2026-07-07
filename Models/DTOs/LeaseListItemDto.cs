namespace KiraTakip.Models.Dtos;

public class LeaseListItemDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public string TenantCategoryName { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyAmount { get; set; }
    public LeaseStatus Status { get; set; }
    public decimal UnitArea { get; set; }

    public int KalanGun => (int)(EndDate - DateTime.Now).TotalDays;
    public bool Aktif => Status == LeaseStatus.Active && StartDate <= DateTime.Now && EndDate >= DateTime.Now;
}
