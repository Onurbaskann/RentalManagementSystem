using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class SozlesmeDetayViewModel
{
    public SozlesmeDetayDto Sozlesme { get; set; } = null!;
    public int KalanGun { get; set; }
    public decimal AylikBedel { get; set; }
    public decimal YillikBedel { get; set; }
    public bool Aktif { get; set; }
    public double SureYuzdesi { get; set; }
    public KiraDurumu Durum { get; set; }
    public List<SozlesmeListItemDto> GecmisSozlesmeler { get; set; } = [];
    public List<TahakkukListItemDto> Tahakkuklar { get; set; } = [];
    public bool HasOdemeAccess { get; set; }
    public ParentTarifeKartViewModel? ParentTarife { get; set; }
    public List<TahakkukKalemi> GuncelKalemler { get; set; } = [];
    public DateTime? GuncelKalemDonemi { get; set; }
    public decimal? DepozitoTutari { get; set; }
    public decimal KdvOraniEtkin { get; set; }
}
