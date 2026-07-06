using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class SozlesmeUzatViewModel
{
    public int SozlesmeId { get; set; }
    public DateTime YeniBitisTarihi { get; set; }
    public bool TufeUygulanacakMi { get; set; }
    public decimal? TufeOrani { get; set; }
    public bool KdvUygulanacakMi { get; set; }
    public decimal? KdvRate { get; set; }
    public string? Aciklama { get; set; }
    public bool TarifeyiGuncelle { get; set; }
    public List<SozlesmeKalemInputDto> SozlesmeKalemleri { get; set; } = [];
}
