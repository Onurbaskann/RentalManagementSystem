namespace KiraTakip.Models.Entities;

public class UserTasinmazYetki : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int TasinmazId { get; set; }
    public string? AtayanUserId { get; set; }
    public DateTime AtanmaTarihi { get; set; }
}
