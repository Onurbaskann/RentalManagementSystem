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
        ["Tenant.Invited"]               = "Kiracı daveti gönderildi",
        ["Tenant.Activated"]             = "Kiracı aktifleştirildi",
        ["Tenant.Deactivated"]           = "Kiracı pasifleştirildi",

        // Entity (interceptor)
        ["Entity.Added"]                 = "Kayıt eklendi",
        ["Entity.Modified"]              = "Kayıt güncellendi",
        ["Entity.Deleted"]               = "Kayıt silindi",
    };

    private static readonly Dictionary<string, string> EntityTypes = new()
    {
        // Identity
        ["ApplicationUser"]         = "Kullanıcı",
        ["Role"]                    = "Rol",
        ["Davetiye"]                = "Davet",
        ["PasswordResetRequest"]    = "Şifre sıfırlama",

        // Domain
        ["Tenant"]                  = "Kiracı",
        ["Lease"]                = "Sözleşme",
        ["Charge"]                = "Charge",
        ["Payment"]                   = "Ödeme",
        ["Property"]                = "Taşınmaz",
        ["Unit"]                   = "Unit",
        ["BankTransaction"]           = "Banka hareketi",
        ["Reservation"]             = "Reservation",
        ["UserPermission"]          = "Kullanıcı izni",
        ["UserTasinmazYetki"]       = "Taşınmaz yetkisi",
        ["UserRole"]                = "Kullanıcı rolü",
        ["RolePermission"]          = "Rol izni",
        ["Tarife"]                  = "Tarife",
        ["PropertyRateOverride"]    = "Taşınmaz tarifesi",
        ["UnitRate"]                = "Unit tarifesi",
        ["GenelTarife"]             = "Genel tarife",
        ["LeaseRateOverride"]       = "Sözleşme tarifesi",
        ["ReservationRateOverride"]       = "Reservation tarifesi",
        ["PropertyPricing"]           = "Taşınmaz fiyatı",
        ["ChargeType"]                = "Borç tipi",
        ["UnitType"]               = "Unit türü",
        ["Category"]                = "Category",
        ["Sector"]                  = "Sektör",
    };

    public static string EventDisplay(string key)
        => EventTypes.TryGetValue(key, out var v) ? v : key;

    public static string EntityDisplay(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "—";
        return EntityTypes.TryGetValue(key, out var v) ? v : key;
    }
}
