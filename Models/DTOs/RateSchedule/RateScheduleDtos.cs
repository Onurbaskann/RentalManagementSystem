using KiraTakip.Models;

namespace KiraTakip.Models.Dtos.RateSchedule;

public record RateYearSummaryDto(int Year, bool IsActive, int ItemCount);

public record RateMatrixDto(
    int Year,
    bool IsActive,
    List<RateMatrixColumnDto> Columns,
    List<RateMatrixRowDto> Rows,
    List<RateMatrixReservationRowDto> ReservationRows
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

public record RateMatrixReservationRowDto(
    int ReservationRateId,
    int UnitTypeId,
    string UnitTypeName,
    int FreeDurationMinutes,
    int BillingPeriodMinutes,
    decimal PeriodRate,
    decimal KdvRate
);

public record SaveRateMatrixInput(
    List<SaveRateCellInput> Cells,
    List<SaveReservationCellInput> ReservationCells
);

public record SaveRateCellInput(
    int LineItemId,
    int TenantCategoryId,
    int ChargeTypeId,
    CalculationMethod CalculationMethod,
    decimal UnitValue,
    decimal KdvRate
);

public record SaveReservationCellInput(
    int ReservationRateId,
    int UnitTypeId,
    int FreeDurationMinutes,
    int BillingPeriodMinutes,
    decimal PeriodRate,
    decimal KdvRate
);

public record CreateRateYearInput(
    int Year,
    int? CopyFromYear
);
