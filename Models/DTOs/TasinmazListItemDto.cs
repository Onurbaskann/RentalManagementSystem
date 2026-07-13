namespace KiraTakip.Models.Dtos;

public class TasinmazListItemDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Il { get; set; } = string.Empty;
    public string Ilce { get; set; } = string.Empty;
    public string TasinmazTipiAd { get; set; } = string.Empty;
    public decimal KapaliYuzolcumu { get; set; }
    public decimal AcikYuzolcumu { get; set; }
    public UnitStructure UnitStructure { get; set; }
    public int BirimSayisi { get; set; }
    public int KiraliBirimSayisi { get; set; }
    public int SuresiDolmakUzereBirimSayisi { get; set; }
    public int BosBirimSayisi { get; set; }
}
