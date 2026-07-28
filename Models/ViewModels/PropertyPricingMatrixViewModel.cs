namespace KiraTakip.Models.ViewModels
{
    using System.Collections.Generic;
    using KiraTakip.Models;

    public class PropertyPricingMatrixViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public List<TenantCategoryPricingRowViewModel> Rows { get; set; } = [];
        public List<ChargeTypePricingColumnViewModel> Columns { get; set; } = [];
        // Toplam satır sayısı (tüm kiracı kategorileri) – sayfalama için kullanılır
        public int TotalRows { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

    public class TenantCategoryPricingRowViewModel
    {
        public int TenantCategoryId { get; set; }
        public string TenantCategoryName { get; set; } = string.Empty;
        public List<PropertyPricingCellViewModel> Cells { get; set; } = [];
    }

    public class ChargeTypePricingColumnViewModel
    {
        public int ChargeTypeId { get; set; }
        public string ChargeTypeName { get; set; } = string.Empty;
        public string ChargeTypeCode { get; set; } = string.Empty;
        public ChargeTypeBehavior ChargeTypeBehavior { get; set; }
    }

    public class PropertyPricingCellViewModel
    {
        public int? PropertyRateOverrideId { get; set; }
        public int PropertyId { get; set; }
        public int TenantCategoryId { get; set; }
        public int ChargeTypeId { get; set; }
        public decimal? UnitValue { get; set; }
        public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
        public decimal? VatRate { get; set; }
        public bool HasRate { get; set; }
    }
}
