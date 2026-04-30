namespace KiraTakip.Models;

public class UserTasinmazYetki
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int TasinmazId { get; set; }

    public DateTime AtanmaTarihi { get; set; }

    public string? AtayanUserId { get; set; }
}
