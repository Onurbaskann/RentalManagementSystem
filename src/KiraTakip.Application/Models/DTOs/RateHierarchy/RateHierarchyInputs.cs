using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.RateHierarchy;

public record GetParentRateInput(
    RateHierarchyLayer Layer,
    int? PropertyId = null,
    int? UnitId = null,
    int? TenantCategoryId = null,
    int? Year = null);

public record GetParentReservationRateInput(int? Year = null);
