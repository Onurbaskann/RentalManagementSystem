namespace KiraTakip.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(string eventType, string? entityType = null, string? entityId = null, string? details = null);
}
