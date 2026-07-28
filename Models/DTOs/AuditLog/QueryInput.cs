namespace KiraTakip.Models.Dtos.AuditLog;

public record QueryInput(
    string? EventType,
    string? EntityType,
    DateTime? StartDate,
    DateTime? EndDate,
    string? UserEmail,
    int Page,
    int PageSize);
