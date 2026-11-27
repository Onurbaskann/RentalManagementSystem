using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.RateHierarchy;

public record GetParentRateInput(
    RateHierarchyLayer Layer,
    int? SpaceId = null,
    int? TenantCategoryId = null,
    int? Year = null);
