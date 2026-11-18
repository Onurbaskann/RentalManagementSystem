namespace KiraTakip.Models.Dtos.Lease;

public sealed record LeaseCapacityWarningDto(
    int FreeSlots,
    decimal RemainingArea,
    decimal NormTarget,
    decimal MinimumTarget,
    bool NormTargetShort,
    bool MinimumTargetShort);
