using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos.AuditLog;

namespace KiraTakip.Models.ViewModels;

public class AuditLogFilterViewModel
{
    public string? EventType { get; set; }
    public string? EntityType { get; set; }
    public string? UserEmail { get; set; }
    public TableQuery Query { get; set; } = new();
    public PagedResult<RowResult> Records { get; set; } = new();
    public List<string> AvailableEventTypes { get; set; } = [];
    public List<string> AvailableEntityTypes { get; set; } = [];
    public string? UserNotFoundMessage { get; set; }
}
