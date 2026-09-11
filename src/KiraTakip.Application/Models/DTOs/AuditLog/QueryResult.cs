using KiraTakip.Models.Common;

namespace KiraTakip.Models.Dtos.AuditLog;

public class QueryResult
{
    public PagedResult<RowResult> Records { get; set; } = new();
    public List<string> AvailableEventTypes { get; set; } = [];
    public List<string> AvailableEntityTypes { get; set; } = [];
    public string? UserNotFoundMessage { get; set; }
}

public class RowResult
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? UserFullName { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
