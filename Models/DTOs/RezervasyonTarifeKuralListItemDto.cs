namespace KiraTakip.Models.Dtos;

public class RezervasyonTarifeKuralListItemDto
{
    public int Id { get; set; }
    public int? BirimId { get; set; }
    public string? BirimAd { get; set; }
    public string? TasinmazAd { get; set; }
    public int UcretsizSureDakika { get; set; }
    public int UcretlendirmePeriyoduDakika { get; set; }
    public decimal PeriyotUcreti { get; set; }
    public decimal KdvOrani { get; set; }
    public bool Aktif { get; set; }
    public string? Aciklama { get; set; }
}
