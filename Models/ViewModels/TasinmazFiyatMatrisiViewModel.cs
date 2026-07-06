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
        public int ChargeTypeId { get; set; }
        public string ChargeTypeName { get; set; } = string.Empty;
        public string ChargeTypeCode { get; set; } = string.Empty;
        public ChargeTypeBehavior ChargeTypeBehavior { get; set; }
    }

    public class TasinmazFiyatHucreViewModel
    {
        public int? TasinmazTarifeId { get; set; }
        public int TasinmazId { get; set; }
        public int KiraciKategoriId { get; set; }
        public int ChargeTypeId { get; set; }
        public decimal? UnitValue { get; set; }
        public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
        public decimal? KdvRate { get; set; }
        public bool RateVarMi { get; set; }
    }
}
