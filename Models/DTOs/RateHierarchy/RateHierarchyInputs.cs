using KiraTakip.Models.ViewModels;

namespace KiraTakip.Models.Dtos;

public record GetParentRateInput(
    RateHierarchyLayer Layer,
    int? PropertyId = null,
    int? UnitId = null,
    int? TenantCategoryId = null,
    int? Year = null);

public record GetParentReservationRateInput(int? Year = null);
