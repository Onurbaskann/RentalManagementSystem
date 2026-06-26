namespace KiraTakip.Models.Dtos;

public class OdemeDetayDto
{
    public int Id { get; set; }
    public int TahakkukId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public OdemeKanali OdemeKanali { get; set; }
    public OdemeKaynakTipi OdemeKaynakTipi { get; set; }
    public string? PosReferansNo { get; set; }
    public string? Aciklama { get; set; }
    public OdemeDurumu Durum { get; set; }
    public DateTime GirisTarihi { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? RedNedeni { get; set; }
    public int? TasinmazId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public DateTime TahakkukDonemBaslangic { get; set; }
    public string? GirenUserGosterimAdi { get; set; }
    public string? OnaylayanUserGosterimAdi { get; set; }
    public List<OdemeBankaEslesmeDto> BankaEslesmeleri { get; set; } = [];
}