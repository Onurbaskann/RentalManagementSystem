using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public static class UnitPricingViewModelMapper
{
    public static UnitCustomRateViewModel ToViewModel(
        this UnitPricingDataDto data,
        IReadOnlyList<UnitRateCategoryRow>? submittedRows = null)
    {
        var viewModel = new UnitCustomRateViewModel
        {
            UnitId = data.UnitId,
            UnitName = data.UnitName,
            PropertyId = data.PropertyId,
            PropertyName = data.PropertyName,
            IsLeasable = data.IsLeasable,
            IsReservable = data.IsReservable,
            UnitTypeName = data.UnitTypeName,
            Rows = data.Rows,
            Columns = data.Columns,
            ParentRate = data.ParentRate,
            CustomReservationRule = data.CustomReservationRule,
            ParentReservationRateOverride = data.ParentReservationRateOverride
        };

        if (submittedRows == null) return viewModel;

        var submittedCells = submittedRows
            .SelectMany(row => row.Cells)
            .GroupBy(cell => (cell.TenantCategoryId, cell.ChargeTypeId))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var row in viewModel.Rows)
        {
            foreach (var cell in row.Cells)
            {
                if (!submittedCells.TryGetValue(
                        (cell.TenantCategoryId, cell.ChargeTypeId),
                        out var submittedCell))
                    continue;

                cell.IsCustomRateActive = submittedCell.IsCustomRateActive;
                cell.CalculationMethod = submittedCell.CalculationMethod;
                cell.UnitValue = submittedCell.UnitValue;
                cell.KdvRate = submittedCell.KdvRate;
            }
        }

        return viewModel;
    }

    public static SaveUnitPricingInput ToSaveInput(
        this UnitPricingFormViewModel viewModel,
        int unitId,
        UnitPricingAccessScopeInput accessScope)
        => new(
            unitId,
            viewModel.Rows.Select(row => new UnitPricingRowInput(
                row.TenantCategoryId,
                row.Cells.Select(cell => new UnitPricingCellInput(
                    cell.TenantCategoryId,
                    cell.ChargeTypeId,
                    cell.IsCustomRateActive,
                    cell.CalculationMethod,
                    cell.UnitValue,
                    cell.KdvRate)).ToList())).ToList(),
            accessScope);
}
