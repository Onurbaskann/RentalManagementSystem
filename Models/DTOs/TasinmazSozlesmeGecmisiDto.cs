namespace KiraTakip.Models.Dtos;

public class TasinmazSozlesmeGecmisiDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public string TenantDisplayName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AylikBedel { get; set; }
    public LeaseStatus Durum { get; set; }
}
