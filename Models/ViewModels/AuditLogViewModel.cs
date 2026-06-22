namespace KiraTakip.Models.ViewModels;

public class AuditLogFilterViewModel
{
    public string? EventType { get; set; }
    public string? EntityType { get; set; }
    public DateTime? BaslangicTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }
    public string? KullaniciEmail { get; set; }
    public int Sayfa { get; set; } = 1;
    public const int SayfaBoyutu = 10;

    public List<AuditLogSatirViewModel> Kayitlar { get; set; } = [];
    public int ToplamKayit { get; set; }
    public List<string> MevcutEventTypes { get; set; } = [];
    public List<string> MevcutEntityTypes { get; set; } = [];
    public string? KullaniciBulunamadiMesaji { get; set; }

    public int ToplamSayfa => (int)Math.Ceiling((double)ToplamKayit / SayfaBoyutu);
}

public class AuditLogSatirViewModel
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? KullaniciAdSoyad { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
