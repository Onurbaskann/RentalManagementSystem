namespace KiraTakip.Models.Dtos;

public class OdemeBankaEslesmeDto
{
    public int Id { get; set; }
    public EslesmeTipi EslesmeTipi { get; set; }
    public decimal BankaHareketiTutar { get; set; }
    public DateTime BankaHareketiTarih { get; set; }
    public string BankaHareketiAciklama { get; set; } = string.Empty;
}
