namespace KiraTakip.Web.Help;

public enum HelpAudience { All, Admin, Tenant }

public record HelpSection(string Id, string Title, string Group, HelpAudience Audience, string Partial);

public static class HelpSections
{
    private const string Dir = "~/Views/Help/Sections/";

    public static IReadOnlyList<HelpSection> All { get; } =
    [
        new("renk-kodlari", "İş Akışı ve Renk Kodları", "Başlangıç", HelpAudience.All, Dir + "_RenkKodlari.cshtml"),
        new("tarife-hiyerarsisi", "Tarife Hiyerarşisi", "Başlangıç", HelpAudience.Admin, Dir + "_TarifeHiyerarsisi.cshtml"),
        new("yonetim-anasayfa", "1. Ana Sayfa", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimAnasayfa.cshtml"),
        new("yonetim-tasinmaz", "2. Taşınmazlar ve Birimler", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimTasinmaz.cshtml"),
        new("yonetim-kiraci", "3. Kiracı Yönetimi", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimKiraci.cshtml"),
        new("yonetim-sozlesme", "4. Sözleşme Yaşam Döngüsü", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimSozlesme.cshtml"),
        new("yonetim-finans", "5. Tahakkuk, Ödeme ve Banka", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimFinans.cshtml"),
        new("yonetim-rapor", "6. Tahsilat Raporu", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimRapor.cshtml"),
        new("yonetim-rezervasyon", "7. Rezervasyon", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimRezervasyon.cshtml"),
        new("yonetim-yetki", "8. Kullanıcı, Rol ve Denetim", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimYetki.cshtml"),
        new("yonetim-parametre", "9. Parametreler ve Tarifeler", "Yönetim Paneli", HelpAudience.Admin, Dir + "_YonetimParametre.cshtml"),
        new("kiraci-panel", "10. Kiracı Ana Sayfası", "Kiracı Portalı", HelpAudience.Tenant, Dir + "_KiraciPanel.cshtml"),
        new("kiraci-sozlesme-borc", "11. Sözleşmelerim ve Ödeme", "Kiracı Portalı", HelpAudience.Tenant, Dir + "_KiraciSozlesmeBorc.cshtml"),
        new("kiraci-rezervasyon-profil", "12. Rezervasyon, Profil ve Ekip", "Kiracı Portalı", HelpAudience.Tenant, Dir + "_KiraciRezervasyonProfil.cshtml"),
        new("hesap-islemleri", "13. Giriş, Şifre ve Davet", "Ortak İşlemler", HelpAudience.All, Dir + "_HesapIslemleri.cshtml"),
    ];

    public static IReadOnlyList<HelpSection> For(bool isTenant) =>
        isTenant ? All.Where(s => s.Audience != HelpAudience.Admin).ToList() : All;

    private static readonly Dictionary<string, string> ControllerMap = BuildMap();

    public static string? ForController(string? controllerName) =>
        controllerName is not null && ControllerMap.TryGetValue(controllerName, out var id) ? id : null;

    private static Dictionary<string, string> BuildMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        void Add(string id, params string[] controllers)
        {
            foreach (var c in controllers) map[c] = id;
        }
        Add("yonetim-anasayfa", "Home");
        Add("yonetim-tasinmaz", "Space");
        Add("yonetim-kiraci", "Tenant");
        Add("yonetim-sozlesme", "Lease", "ChargeReminder");
        Add("yonetim-finans", "Charge", "ManualCharge", "Payment", "BankTransaction");
        Add("yonetim-rapor", "Report");
        Add("yonetim-rezervasyon", "Reservation");
        Add("yonetim-yetki", "AdminUser", "AdminTenantUser", "AdminRole", "AdminAuditLog");
        Add("yonetim-parametre", "AdminSpaceType", "AdminSpaceAttribute", "AdminTenantCategory", "AdminSector", "AdminChargeType", "AdminStore",
            "AdminPaymentStoreRouting", "AdminDocumentType", "AdminSystemSetting", "AdminRate");
        Add("tarife-hiyerarsisi", "AdminReservationRateRule");
        Add("kiraci-panel", "TenantPanel");
        Add("kiraci-sozlesme-borc", "TenantLease", "TenantCharge");
        Add("kiraci-rezervasyon-profil", "TenantReservation", "TenantProfile", "TenantUser", "TenantRole");
        Add("hesap-islemleri", "Account");
        return map;
    }
}
