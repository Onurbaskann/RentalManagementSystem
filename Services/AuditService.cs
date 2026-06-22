using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using System.Security.Claims;

namespace KiraTakip.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string eventType, string? entityType = null, string? entityId = null, string? details = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var log = new AuditLog
        {
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            UserId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier),
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request?.Headers.UserAgent.ToString() is { Length: > 0 } ua
                ? ua[..Math.Min(ua.Length, 500)]
                : null,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
