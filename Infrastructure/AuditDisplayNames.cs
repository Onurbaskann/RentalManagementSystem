namespace KiraTakip.Infrastructure;

public static class AuditDisplayNames
{
    private static readonly Dictionary<string, string> EventTypes = new()
    {
        // Oturum
        ["User.LoginSuccess"]            = "Giriş başarılı",
        ["User.LoginFailed"]             = "Giriş başarısız",
        ["User.LockedOut"]               = "Hesap kilitlendi",
        ["User.Logout"]                  = "Çıkış",

        // Hesap yönetimi
        ["User.Activated"]               = "Hesap aktifleştirildi",
        ["User.Deactivated"]             = "Hesap pasifleştirildi",
        ["User.PasswordReset.Requested"] = "Şifre sıfırlama talebi",
        ["User.PasswordReset.Completed"] = "Şifre sıfırlandı",

        // Davet
        ["Invite.Sent"]                  = "Davet gönderildi",
        ["Invite.Accepted"]              = "Davet kabul edildi",
        ["Invite.Cancelled"]             = "Davet iptal edildi",
        ["Invite.Resent"]                = "Davet yeniden gönderildi",

        // Rol
        ["Role.Created"]                 = "Rol oluşturuldu",
        ["Role.Updated"]                 = "Rol güncellendi",
        ["Role.Deleted"]                 = "Rol silindi",
        ["Role.Permission.Changed"]      = "Rol izinleri değiştirildi",

        // Kiracı kullanıcı yönetimi
        ["User.RoleChanged"]             = "Rol değiştirildi",
        ["Kiraci.Invited"]               = "Kiracı daveti gönderildi",
        ["Kiraci.Activated"]             = "Kiracı aktifleştirildi",
        ["Kiraci.Deactivated"]           = "Kiracı pasifleştirildi",

        // Entity (interceptor)
        ["Entity.Added"]                 = "Kayıt eklendi",
        ["Entity.Modified"]              = "Kayıt güncellendi",
        ["Entity.Deleted"]               = "Kayıt silindi",
    };

    private static readonly Dictionary<string, string> EntityTypes = new()
    {
        // Identity
        ["ApplicationUser"]         = "Kullanıcı",
        ["Rol"]                     = "Rol",
        ["Davetiye"]                = "Davet",
        ["SifreSifirlamaTalebi"]    = "Şifre sıfırlama",

        // Domain
        ["Kiraci"]                  = "Kiracı",
        ["Sozlesme"]                = "Sözleşme",
        ["Tahakkuk"]                = "Tahakkuk",
        ["Odeme"]                   = "Ödeme",
        ["Tasinmaz"]                = "Taşınmaz",
        ["Birim"]                   = "Birim",
        ["BankaHareketi"]           = "Banka hareketi",
        ["Rezervasyon"]             = "Rezervasyon",
        ["UserPermission"]          = "Kullanıcı izni",
        ["UserTasinmazYetki"]       = "Taşınmaz yetkisi",
        ["UserRol"]                 = "Kullanıcı rolü",
        ["RolPermission"]           = "Rol izni",
        ["Tarife"]                  = "Tarife",
        ["TasinmazTarife"]          = "Taşınmaz tarifesi",
        ["BirimTarife"]             = "Birim tarifesi",
        ["GenelTarife"]             = "Genel tarife",
        ["SozlesmeTarife"]          = "Sözleşme tarifesi",
        ["RezervasyonTarife"]       = "Rezervasyon tarifesi",
        ["TasinmazFiyat"]           = "Taşınmaz fiyatı",
        ["BorcTipi"]                = "Borç tipi",
        ["UnitType"]               = "Birim türü",
        ["Kategori"]                = "Kategori",
        ["Sektor"]                  = "Sektör",
    };

    public static string EventDisplay(string key)
        => EventTypes.TryGetValue(key, out var v) ? v : key;

    public static string EntityDisplay(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "—";
        return EntityTypes.TryGetValue(key, out var v) ? v : key;
    }
}
