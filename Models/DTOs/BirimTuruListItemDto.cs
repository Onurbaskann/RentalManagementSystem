namespace KiraTakip.Models.Dtos;

public class UnitTypeListItemDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public int Sira { get; set; }
    public bool KiralanabilirMi { get; set; }
    public bool RezervasyonYapilabilirMi { get; set; }
    public int? BorcTipiId { get; set; }
    public string? BorcTipiAd { get; set; }
    public bool Aktif { get; set; }
}
