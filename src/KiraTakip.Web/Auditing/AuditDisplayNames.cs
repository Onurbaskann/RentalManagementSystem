using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Auditing;

public static class AuditDisplayNames
{
    private static readonly Dictionary<string, string> EventTypes = new()
    {
        // Oturum
        ["User.LoginSuccess"] = "Giriş başarılı",
        ["User.LoginFailed"] = "Giriş başarısız",
        ["User.LockedOut"] = "Hesap kilitlendi",
        ["User.Logout"] = "Çıkış",

        // Hesap yönetimi
        ["User.Activated"] = "Hesap aktifleştirildi",
        ["User.Deactivated"] = "Hesap pasifleştirildi",
        ["User.PasswordReset.Requested"] = "Şifre sıfırlama talebi",
        ["User.PasswordReset.Completed"] = "Şifre sıfırlandı",
        ["User.PasswordChanged"] = "Şifre değiştirildi",
        ["User.ScopeChanged"] = "Yetki kapsamı değiştirildi",

        // Davet
        ["Invite.Sent"] = "Davet gönderildi",
        ["Invite.Accepted"] = "Davet kabul edildi",
        ["Invite.Cancelled"] = "Davet iptal edildi",
        ["Invite.Resent"] = "Davet yeniden gönderildi",

        // Rol
        ["Role.Created"] = "Rol oluşturuldu",
        ["Role.Updated"] = "Rol güncellendi",
        ["Role.Deleted"] = "Rol silindi",
        ["Role.Permission.Changed"] = "Rol izinleri değiştirildi",

        // Kiracı kullanıcı yönetimi
        ["User.RoleChanged"] = "Rol değiştirildi",
        // Entity (interceptor)
        ["Entity.Added"] = "Kayıt eklendi",
        ["Entity.Modified"] = "Kayıt güncellendi",
        ["Entity.Deleted"] = "Kayıt silindi",
    };

    private static readonly Dictionary<string, string> EntityTypes = new()
    {
        // Identity
        ["ApplicationUser"] = "Kullanıcı",
        ["Role"] = "Rol",
        ["Invitation"] = "Davet",
        ["PasswordResetRequest"] = "Şifre sıfırlama",

        // Domain
        ["Tenant"] = "Kiracı",
        ["Lease"] = "Sözleşme",
        ["Charge"] = "Tahakkuk",
        ["ChargeLineItem"] = "Tahakkuk kalemi",
        ["PaymentAllocation"] = "Ödeme dağıtımı",
        ["PaymentMatch"] = "Ödeme eşleştirmesi",
        ["Property"] = "Taşınmaz",
        ["PropertyType"] = "Taşınmaz türü",
        ["Unit"] = "Bağımsız bölüm",
        ["BankTransaction"] = "Banka hareketi",
        ["Reservation"] = "Rezervasyon",
        ["ReservationAttendee"] = "Rezervasyon katılımcısı",
        ["UserPermission"] = "Kullanıcı izni",
        ["UserPermissionScope"] = "Kullanıcı yetki kapsamı",
        ["UserRole"] = "Kullanıcı rolü",
        ["RolePermission"] = "Rol izni",
        ["PropertyRateOverride"] = "Taşınmaz tarifesi",
        ["UnitRate"] = "Bağımsız bölüm tarifesi",
        ["RateSchedule"] = "Genel tarife",
        ["LeaseRateOverride"] = "Sözleşme tarifesi",
        ["ReservationRateOverride"] = "Rezervasyon tarifesi",
        ["ChargeType"] = "Borç türü",
        ["UnitType"] = "Bağımsız bölüm türü",
        ["Category"] = "Kategori",
        ["Document"] = "Belge",
        ["DocumentType"] = "Belge türü",
        ["PaymentStoreRouting"] = "Ödeme mağaza yönlendirmesi",
        ["Store"] = "Mağaza",
        ["StoreAccount"] = "Mağaza hesabı",
        ["OnlinePaymentTransaction"] = "Online ödeme işlemi",
        ["SystemSetting"] = "Sistem ayarı",

        // Önceki sürümlerde yazılmış kayıt adları geriye dönük görüntülenmeye devam eder.
        ["Davetiye"] = "Davet",
        ["Tarife"] = "Tarife",
        ["GenelTarife"] = "Genel tarife",
        ["Payment"] = "Ödeme",
        ["PropertyPricing"] = "Taşınmaz fiyatı",
        ["Sector"] = "Sektör",
        ["UserTasinmazYetki"] = "Taşınmaz yetkisi",
    };

    public static string EventDisplay(string key)
        => EventTypes.TryGetValue(key, out var v) ? v : key;

    public static string EntityDisplay(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "—";
        return EntityTypes.TryGetValue(key, out var v) ? v : key;
    }

    public static string UserTypeDisplay(UserType? userType)
        => userType switch
        {
            UserType.Internal => "İç kullanıcı",
            UserType.Tenant => "Kiracı kullanıcısı",
            _ => "—"
        };
}
