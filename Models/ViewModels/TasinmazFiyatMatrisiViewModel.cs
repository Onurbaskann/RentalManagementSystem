namespace KiraTakip.Models.ViewModels
{
    using System.Collections.Generic;
    using KiraTakip.Models;

    public class TasinmazFiyatMatrisiViewModel
    {
        public int TasinmazId { get; set; }
        public string TasinmazAd { get; set; } = string.Empty;
        public List<KiraciKategoriFiyatSatiriViewModel> Satirlar { get; set; } = [];
        public List<BorcTipiFiyatKolonuViewModel> Kolonlar { get; set; } = [];
        // Toplam satır sayısı (tüm kiracı kategorileri) – sayfalama için kullanılır
        public int TotalRows { get; set; }
    }

    public class KiraciKategoriFiyatSatiriViewModel
    {
        public int KiraciKategoriId { get; set; }
        public string KiraciKategoriAd { get; set; } = string.Empty;
        public List<TasinmazFiyatHucreViewModel> Hucreler { get; set; } = [];
    }

    public class BorcTipiFiyatKolonuViewModel
    {
        public int BorcTipiId { get; set; }
        public string BorcTipiAd { get; set; } = string.Empty;
        public string BorcTipiKod { get; set; } = string.Empty;
        public BorcTipiDavranisi BorcTipiDavranisi { get; set; }
    }

    public class TasinmazFiyatHucreViewModel
    {
        public int? TasinmazTarifeId { get; set; }
        public int TasinmazId { get; set; }
        public int KiraciKategoriId { get; set; }
        public int BorcTipiId { get; set; }
        public decimal? BirimDeger { get; set; }
        public HesaplamaYontemi HesaplamaYontemi { get; set; } = HesaplamaYontemi.Sabit;
        public decimal? KdvOrani { get; set; }
        public bool Aktif { get; set; } = true;
        public string? Aciklama { get; set; }
        public bool RateVarMi { get; set; }
    }
}
