using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class SozlesmeDetayViewModel
{
    public LeaseDetailDto Lease { get; set; } = null!;
    public int KalanGun { get; set; }
    public decimal AylikBedel { get; set; }
    public decimal YillikBedel { get; set; }
    public bool Aktif { get; set; }
    public double SureYuzdesi { get; set; }
    public OccupancyStatus Durum { get; set; }
    public List<LeaseListItemDto> GecmisSozlesmeler { get; set; } = [];
    public List<LeaseListItemDto> KiraciSozlesmeleri { get; set; } = [];
    public List<ChargeListItemDto> Charges { get; set; } = [];
    public bool HasOdemeAccess { get; set; }
    public ParentTarifeKartViewModel? ParentTarife { get; set; }
    public List<ChargeLineItem> GuncelKalemler { get; set; } = [];
    public DateTime? GuncelKalemDonemi { get; set; }
    public decimal? DepozitoTutari { get; set; }
    public decimal KdvOraniEtkin { get; set; }
    public DateTime DefaultYenidenUretBaslangicTarihi { get; set; } = DateTime.Today;
    public DateTime? SonOdenenDonem { get; set; }
    public int OdenmemisTahakkukSayisi { get; set; }
    public List<Belge> Belgeler { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}
