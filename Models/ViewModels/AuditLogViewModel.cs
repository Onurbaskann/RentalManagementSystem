namespace KiraTakip.Models.ViewModels;

public class AuditLogFilterViewModel
{
    public string? EventType { get; set; }
    public string? EntityType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? UserEmail { get; set; }
    public int Page { get; set; } = 1;
    public const int PageSize = 10;

    public List<AuditLogRowViewModel> Records { get; set; } = [];
    public int TotalCount { get; set; }
    public List<string> AvailableEventTypes { get; set; } = [];
    public List<string> AvailableEntityTypes { get; set; } = [];
    public string? UserNotFoundMessage { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class AuditLogRowViewModel
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
