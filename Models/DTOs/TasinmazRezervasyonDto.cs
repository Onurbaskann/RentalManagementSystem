namespace KiraTakip.Models.Dtos;

public class TasinmazRezervasyonDto
{
    public int Id { get; set; }
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public int ToplamSureDakika { get; set; }
    public int UcretsizSureDakika { get; set; }
    public decimal ToplamTutar { get; set; }
    public ReservationStatus Durum { get; set; }
}
