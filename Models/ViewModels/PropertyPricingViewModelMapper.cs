using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public static class PropertyPricingViewModelMapper
{
    public static PropertyPricingMatrixViewModel ToViewModel(this PropertyPricingMatrixDto dto)
        => new()
        {
            PropertyId = dto.PropertyId,
            PropertyName = dto.PropertyName,
            TotalRows = dto.TotalRows,
            CurrentPage = dto.CurrentPage,
            PageSize = dto.PageSize,
            Columns = dto.Columns.Select(column => new ChargeTypePricingColumnViewModel
            {
                ChargeTypeId = column.ChargeTypeId,
                ChargeTypeName = column.ChargeTypeName,
                ChargeTypeCode = column.ChargeTypeCode,
                ChargeTypeBehavior = column.ChargeTypeBehavior
            }).ToList(),
            Rows = dto.Rows.Select(row => new TenantCategoryPricingRowViewModel
            {
                TenantCategoryId = row.TenantCategoryId,
                TenantCategoryName = row.TenantCategoryName,
                Cells = row.Cells.Select(cell => new PropertyPricingCellViewModel
                {
                    PropertyRateOverrideId = cell.PropertyRateOverrideId,
                    PropertyId = cell.PropertyId,
                    TenantCategoryId = cell.TenantCategoryId,
                    ChargeTypeId = cell.ChargeTypeId,
                    UnitValue = cell.UnitValue,
                    CalculationMethod = cell.CalculationMethod,
                    VatRate = cell.VatRate,
                    HasRate = cell.HasRate
                }).ToList()
            }).ToList()
        };

    public static SavePropertyPricingMatrixInput ToSaveInput(
        this PropertyPricingMatrixViewModel? viewModel,
        int propertyId)
        => new()
        {
            PropertyId = propertyId,
            Rows = viewModel?.Rows.Select(row => new PropertyPricingRowDto
            {
                TenantCategoryId = row.TenantCategoryId,
                TenantCategoryName = row.TenantCategoryName,
                Cells = row.Cells.Select(cell => new PropertyPricingCellDto
                {
                    PropertyRateOverrideId = cell.PropertyRateOverrideId,
                    PropertyId = propertyId,
                    TenantCategoryId = cell.TenantCategoryId,
                    ChargeTypeId = cell.ChargeTypeId,
                    UnitValue = cell.UnitValue,
                    CalculationMethod = cell.CalculationMethod,
                    VatRate = cell.VatRate,
                    HasRate = cell.HasRate
                }).ToList()
            }).ToList() ?? []
        };
}
