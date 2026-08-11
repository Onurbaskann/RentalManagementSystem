using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos.AuditLog;

public record QueryInput(
    string? EventType,
    string? EntityType,
    string? UserEmail,
    TableQuery Query);
