using KiraTakip.Models.Enums;

namespace KiraTakip.Models.Dtos.ChargeType;

public record EditInput(string Name, ChargeTypeBehavior Behavior, int SortOrder, bool IsActive);
