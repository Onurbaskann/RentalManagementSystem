namespace KiraTakip.Models.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public UserType? UserType { get; set; }
    public int? KiraciId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
