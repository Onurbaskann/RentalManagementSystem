using KiraTakip.Models;

namespace KiraTakip.Models.Dtos.ChargeType;

public record CreateInput(string Name, ChargeTypeBehavior Behavior, int SortOrder, bool IsActive);
