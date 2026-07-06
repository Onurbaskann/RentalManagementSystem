namespace KiraTakip.Models.Dtos;

public class SozlesmeDetayDto
{
    public int Id { get; set; }
    public int KiraciId { get; set; }
    public string KiraciGosterimAdi { get; set; } = string.Empty;
    public string? KiraciTelefon { get; set; }
    public string? KiraciEmail { get; set; }
    public int? KiraciKategoriId { get; set; }
    public string? KiraciKategoriAd { get; set; }
    public int BirimId { get; set; }
    public string BirimAd { get; set; } = string.Empty;
    public string? BirimNo { get; set; }
    public int? BirimKatNo { get; set; }
    public decimal BirimYuzolcumu { get; set; }
    public UnitKind UnitKind { get; set; }
    public int TasinmazId { get; set; }
    public string TasinmazAd { get; set; } = string.Empty;
    public string TasinmazIl { get; set; } = string.Empty;
    public string TasinmazIlce { get; set; } = string.Empty;
    public string TasinmazMahalle { get; set; } = string.Empty;
    public string TasinmazAcikAdres { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Aciklama { get; set; }
    public LeaseStatus Durum { get; set; }
    public DateTime? FesihTarihi { get; set; }
    public string? FesihNedeni { get; set; }
    public bool KdvUygulanacakMi { get; set; }
    public DueDateRuleType DueDateRuleType { get; set; }
    public int VadeGunu { get; set; }
    public List<SozlesmeIslemGecmisiDto> IslemGecmisi { get; set; } = [];
    public List<SozlesmeTarifeDto> SozlesmeTarifeler { get; set; } = [];
}