using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.RateSchedule;

public record RateYearSummaryDto(int Year, bool IsActive, int ItemCount);

public record RateMatrixDto(
    int Year,
    bool IsActive,
    List<RateMatrixColumnDto> Columns,
    List<RateMatrixRowDto> Rows
);

public record RateMatrixColumnDto(int ChargeTypeId, string Name, string Code);

public record RateMatrixRowDto(int TenantCategoryId, string TenantCategoryName, List<RateMatrixCellDto> Cells);

public record RateMatrixCellDto(
    int LineItemId,
    int TenantCategoryId,
    int ChargeTypeId,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal KdvRate
);

public record SaveRateMatrixInput(
    List<SaveRateCellInput> Cells
);

public record SaveRateCellInput(
    int LineItemId,
    int TenantCategoryId,
    int ChargeTypeId,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal KdvRate
);

public record CreateRateYearInput(
    int Year,
    int? CopyFromYear
);
