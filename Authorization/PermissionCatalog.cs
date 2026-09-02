namespace KiraTakip.Authorization;

public record PermissionActionInfo(string Path, string DisplayName);

public record PermissionModuleInfo(
    string Path,
    string DisplayName,
    IReadOnlyList<PermissionActionInfo> ActionDefinitions,
    string? ParentGroupDisplayName = null)
{
    public string AccessDisplayName => "Görüntüle";
    public IReadOnlyList<string> Actions { get; } =
        ActionDefinitions.Select(action => action.Path).ToArray();
}

public static class PermissionCatalog
{
    // ─── Internal Operations (Internal.*) ────────────────────────────────────

    public static class Property
    {
        public const string Module = "Internal.Property";
        public const string Create = "Internal.Property.Create";
        public const string Edit   = "Internal.Property.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class Unit
    {
        public const string Module      = "Internal.Unit";
        public const string Create      = "Internal.Unit.Create";
        public const string Edit        = "Internal.Unit.Edit";
        public const string OverrideRate = "Internal.Unit.OverrideRate";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(OverrideRate, "Elle Müdahale")];
    }

    public static class Tenant
    {
        public const string Module = "Internal.Tenant";
        public const string Create = "Internal.Tenant.Create";
        public const string Edit   = "Internal.Tenant.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class Lease
    {
        public const string Module       = "Internal.Lease";
        public const string Create       = "Internal.Lease.Create";
        public const string Edit         = "Internal.Lease.Edit";
        public const string Extend       = "Internal.Lease.Extend";
        public const string Terminate    = "Internal.Lease.Terminate";
        public const string OverrideRate = "Internal.Lease.OverrideRate";
        public const string Approve = "Internal.Lease.Approve";
        public const string RequestRevision = "Internal.Lease.RequestRevision";
        public const string DeleteDraft = "Internal.Lease.DeleteDraft";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Extend, "Süre Uzat"), new(Terminate, "Feshet"), new(OverrideRate, "Elle Müdahale"), new(Approve, "Onayla"), new(RequestRevision, "Revizyon İste"), new(DeleteDraft, "Başvuru Sil")];
    }

    public static class Payment
    {
        public const string Module               = "Internal.Payment";
        public const string Create               = "Internal.Payment.Create";
        public const string UploadReceipt        = "Internal.Payment.UploadReceipt";
        public const string Approve              = "Internal.Payment.Approve";
        public const string Reject               = "Internal.Payment.Reject";
        public const string ImportBankStatement  = "Internal.Payment.ImportBankStatement";
        public const string MatchBankTransaction = "Internal.Payment.MatchBankTransaction";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(UploadReceipt, "Dekont Yükle"), new(Approve, "Onayla"), new(Reject, "Reddet"), new(ImportBankStatement, "Banka Hareketleri İçe Aktar"), new(MatchBankTransaction, "Banka Hareketi Eşleştir")];
    }

    public static class ChargeType
    {
        public const string Module = "Internal.ChargeType";
        public const string Create = "Internal.ChargeType.Create";
        public const string Edit   = "Internal.ChargeType.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class Store
    {
        public const string Module  = "Internal.Store";
        public const string Create  = "Internal.Store.Create";
        public const string Edit    = "Internal.Store.Edit";
        public const string Account = "Internal.Store.Account";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions =
            [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Account, "Hesap Yönet")];
    }

    public static class PaymentRouting
    {
        public const string Module = "Internal.PaymentRouting";
        public const string Create = "Internal.PaymentRouting.Create";
        public const string Edit   = "Internal.PaymentRouting.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions =
            [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class Parameter
    {
        public const string Module = "Internal.Parameter";
        public const string Edit   = "Internal.Parameter.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Edit, "Düzenle")];
    }

    public static class PropertyType
    {
        public const string Module = "Internal.PropertyType";
        public const string Create = "Internal.PropertyType.Create";
        public const string Edit   = "Internal.PropertyType.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class UnitType
    {
        public const string Module = "Internal.UnitType";
        public const string Create = "Internal.UnitType.Create";
        public const string Edit   = "Internal.UnitType.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class TenantCategory
    {
        public const string Module = "Internal.TenantCategory";
        public const string Create = "Internal.TenantCategory.Create";
        public const string Edit   = "Internal.TenantCategory.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class Sector
    {
        public const string Module = "Internal.Sector";
        public const string Create = "Internal.Sector.Create";
        public const string Edit   = "Internal.Sector.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class DocumentType
    {
        public const string Module = "Internal.DocumentType";
        public const string Create = "Internal.DocumentType.Create";
        public const string Edit   = "Internal.DocumentType.Edit";
        public const string Delete = "Internal.DocumentType.Delete";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Delete, "Sil")];
    }

    public static class RateSchedule
    {
        public const string Module = "Internal.RateSchedule";
        public const string Create = "Internal.RateSchedule.Create";
        public const string Edit   = "Internal.RateSchedule.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class Charge
    {
        public const string Module     = "Internal.Charge";
        public const string Regenerate = "Internal.Charge.Regenerate";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Regenerate, "Yeniden Üret")];
    }

    public static class ManualCharge
    {
        public const string Module = "Internal.ManualCharge";
        public const string Create = "Internal.ManualCharge.Create";
        public const string Cancel = "Internal.ManualCharge.Cancel";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Cancel, "İptal Et")];
    }

    public static class Reservation
    {
        public const string Module             = "Internal.Reservation";
        public const string Create             = "Internal.Reservation.Create";
        public const string Edit               = "Internal.Reservation.Edit";
        public const string Cancel             = "Internal.Reservation.Cancel";
        public const string Approve             = "Internal.Reservation.Approve";
        public const string Reject              = "Internal.Reservation.Reject";
        public const string OverrideTimeRestriction = "Internal.Reservation.OverrideTimeRestriction";
        public const string TransferToCharge    = "Internal.Reservation.TransferToCharge";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions =
        [
            new(Create, "Ekle"),
            new(Edit, "Düzenle"),
            new(Cancel, "İptal Et"),
            new(Approve, "Onayla"),
            new(Reject, "Reddet"),
            new(OverrideTimeRestriction, "Zaman Sınırı İstisnası"),
            new(TransferToCharge, "Tahakkuka Aktar")
        ];
    }

    public static class PropertyMultiplier
    {
        public const string Module = "Internal.PropertyMultiplier";
        public const string Edit   = "Internal.PropertyMultiplier.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Edit, "Düzenle")];
    }

    public static class ReservationRateRule
    {
        public const string Module = "Internal.ReservationRateRule";
        public const string Create = "Internal.ReservationRateRule.Create";
        public const string Edit   = "Internal.ReservationRateRule.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle")];
    }

    public static class Notification
    {
        public const string Module         = "Internal.Notification";
        public const string BorcHatirlatma = "Internal.Notification.BorcHatirlatma";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(BorcHatirlatma, "Borç Hatırlatma")];
    }

    // ─── System Administration (System.*) ───────────────────────────────────────────

    public static class User
    {
        public const string Module           = "System.User";
        public const string Create           = "System.User.Create";
        public const string Edit             = "System.User.Edit";
        public const string AssignPermission = "System.User.AssignPermission";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(AssignPermission, "Yetki Ata")];
    }

    public static class Role
    {
        public const string Module = "System.Role";
        public const string Create = "System.Role.Create";
        public const string Edit   = "System.Role.Edit";
        public const string Delete = "System.Role.Delete";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Delete, "Sil")];
    }

    public static class Invitation
    {
        public const string Module = "System.Invitation";
        public const string Create = "System.Invitation.Create";
        public const string Cancel = "System.Invitation.Cancel";
        public const string Resend = "System.Invitation.Resend";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Cancel, "İptal Et"), new(Resend, "Yeniden Gönder")];
    }

    public static class Audit
    {
        public const string Module = "System.Audit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [];
    }

    // ─── Tenant Portal (Tenant.*) ─────────────────────────────────────────────

    public static class TenantPortal
    {
        public static class Lease
        {
            public const string Module = "Tenant.Lease";
            public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [];
        }

        public static class Charge
        {
            public const string Module = "Tenant.Charge";
            public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [];
        }

        public static class Payment
        {
            public const string Module = "Tenant.Payment";
            public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [];
        }

        public static class Statement
        {
            public const string Module = "Tenant.Statement";
            public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [];
        }

        public static class Reconciliation
        {
            public const string Module = "Tenant.Reconciliation";
            public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [];
        }

        public static class Reservation
        {
            public const string Module = "Tenant.Reservation";
            public const string Create = "Tenant.Reservation.Create";
            public const string Cancel = "Tenant.Reservation.Cancel";
            public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Cancel, "İptal Et")];
        }

        public static class System
        {
            public static class User
            {
                public const string Module     = "Tenant.System.User";
                public const string Invite     = "Tenant.System.User.Invite";
                public const string Edit       = "Tenant.System.User.Edit";
                public const string Deactivate = "Tenant.System.User.Deactivate";
                public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Invite, "Davet Et"), new(Edit, "Düzenle"), new(Deactivate, "Pasifleştir")];
            }

            public static class Role
            {
                public const string Module = "Tenant.System.Role";
                public const string Create = "Tenant.System.Role.Create";
                public const string Edit   = "Tenant.System.Role.Edit";
                public const string Delete = "Tenant.System.Role.Delete";
                public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Delete, "Sil")];
            }
        }
    }

    // ─── AllModules (UI Tree + SistemYoneticisi claims) ──────────────────────

    public static readonly IReadOnlyList<PermissionModuleInfo> AllModules =
    [
        // Internal
        new(Property.Module,               "Taşınmaz",                   Property.ActionDefinitions),
        new(Unit.Module,                  "Birim",                      Unit.ActionDefinitions),
        new(Tenant.Module,                 "Kiracı",                     Tenant.ActionDefinitions),
        new(Lease.Module,               "Sözleşme",                   Lease.ActionDefinitions),
        new(Payment.Module,                  "Ödeme",                      Payment.ActionDefinitions),
        new(ManualCharge.Module,             "Manuel Borç",                ManualCharge.ActionDefinitions),
        new(Reservation.Module,            "Rezervasyon",                Reservation.ActionDefinitions),
        new(Charge.Module,               "Tahakkuk",                   Charge.ActionDefinitions),
        new(ChargeType.Module,               "Borç Tipi",                  ChargeType.ActionDefinitions, "Parametreler"),
        new(Store.Module,                    "Mağaza",                     Store.ActionDefinitions, "Parametreler"),
        new(PaymentRouting.Module,           "Ödeme Yönlendirmeleri",      PaymentRouting.ActionDefinitions, "Parametreler"),
        new(Parameter.Module,              "Sistem Ayarları",                  Parameter.ActionDefinitions, "Parametreler"),
        new(PropertyType.Module,           "Taşınmaz Tipi",              PropertyType.ActionDefinitions, "Parametreler"),
        new(UnitType.Module,              "Birim Tipi",                 UnitType.ActionDefinitions, "Parametreler"),
        new(TenantCategory.Module,         "Kiracı Kategorisi",          TenantCategory.ActionDefinitions, "Parametreler"),
        new(Sector.Module,                 "Sektör",                     Sector.ActionDefinitions, "Parametreler"),
        new(DocumentType.Module,              "Belge Türü",              DocumentType.ActionDefinitions),
        new(RateSchedule.Module,                 "Yıllık Tarife",                     RateSchedule.ActionDefinitions),
        new(PropertyMultiplier.Module,         "Taşınmaz Katsayısı",           PropertyMultiplier.ActionDefinitions),
        new(ReservationRateRule.Module, "Rezervasyon Tarife Kuralı",  ReservationRateRule.ActionDefinitions),
        new(Notification.Module,               "Bildirim",                   Notification.ActionDefinitions),
        // System
        new(User.Module,              "Kullanıcı",                  User.ActionDefinitions),
        new(Role.Module,                    "Rol",                        Role.ActionDefinitions),
        new(Invitation.Module,               "Davet",                  Invitation.ActionDefinitions),
        new(Audit.Module,                  "Hareket Geçmişi",            Audit.ActionDefinitions),
        // Tenant Portal
        new(TenantPortal.Lease.Module,           "Kiracı — Sözleşme",          TenantPortal.Lease.ActionDefinitions),
        new(TenantPortal.Charge.Module,               "Kiracı — Tahakkuk",              TenantPortal.Charge.ActionDefinitions),
        new(TenantPortal.Payment.Module,              "Kiracı — Ödeme",             TenantPortal.Payment.ActionDefinitions),
        new(TenantPortal.Reservation.Module,        "Kiracı — Rezervasyon",       TenantPortal.Reservation.ActionDefinitions),
        new(TenantPortal.System.User.Module,   "Kiracı Yönetimi — Kullanıcı", TenantPortal.System.User.ActionDefinitions),
        new(TenantPortal.System.Role.Module,         "Kiracı Yönetimi — Rol",       TenantPortal.System.Role.ActionDefinitions),
    ];

    // ─── Scope Awareness (row-level scope) ───────────────────────────────

    public static readonly IReadOnlyList<string> ScopeAware =
    [
        Property.Module, Property.Create, Property.Edit,
        Unit.Module, Unit.Create, Unit.Edit, Unit.OverrideRate,
        Tenant.Module, Tenant.Create, Tenant.Edit,
        Lease.Module, Lease.Create, Lease.Edit, Lease.Extend, Lease.Terminate, Lease.OverrideRate,
        Lease.Approve, Lease.RequestRevision, Lease.DeleteDraft,
        Payment.Module, Payment.Create, Payment.UploadReceipt, Payment.Approve, Payment.Reject, Payment.MatchBankTransaction,
        ManualCharge.Module, ManualCharge.Create, ManualCharge.Cancel,
        Reservation.Module, Reservation.Create, Reservation.Edit, Reservation.Cancel,
        Reservation.Approve, Reservation.Reject, Reservation.OverrideTimeRestriction,
        Reservation.TransferToCharge,
        Charge.Module, Charge.Regenerate,
        PropertyMultiplier.Module, PropertyMultiplier.Edit,
        ReservationRateRule.Module, ReservationRateRule.Create, ReservationRateRule.Edit,
    ];

    public static bool IsScopeAware(string permission) => ScopeAware.Contains(permission);

    // ─── Preset Lists ──────────────────────────────────────────────────────

    public static readonly IReadOnlyList<string> OperasyonMuduruIzinleri =
    [
        Property.Module, Property.Create, Property.Edit,
        Unit.Module, Unit.Create, Unit.Edit, Unit.OverrideRate,
        Tenant.Module, Tenant.Create, Tenant.Edit,
        Lease.Module, Lease.Create, Lease.Edit, Lease.Extend, Lease.Terminate, Lease.OverrideRate,
        Lease.Approve, Lease.RequestRevision, Lease.DeleteDraft,
        Payment.Module, Payment.Create, Payment.UploadReceipt, Payment.Approve, Payment.Reject,
        Payment.ImportBankStatement, Payment.MatchBankTransaction,
        ChargeType.Module, ChargeType.Create, ChargeType.Edit,
        Store.Module, Store.Create, Store.Edit, Store.Account,
        PaymentRouting.Module, PaymentRouting.Create, PaymentRouting.Edit,
        RateSchedule.Module, RateSchedule.Create, RateSchedule.Edit,
        Charge.Module, Charge.Regenerate,
        Parameter.Module, Parameter.Edit,
        PropertyType.Module, PropertyType.Create, PropertyType.Edit,
        UnitType.Module, UnitType.Create, UnitType.Edit,
        TenantCategory.Module, TenantCategory.Create, TenantCategory.Edit,
        Sector.Module, Sector.Create, Sector.Edit,
        DocumentType.Module, DocumentType.Create, DocumentType.Edit, DocumentType.Delete,
        ManualCharge.Module, ManualCharge.Create, ManualCharge.Cancel,
        Reservation.Module, Reservation.Create, Reservation.Edit, Reservation.Cancel,
        Reservation.Approve, Reservation.Reject, Reservation.OverrideTimeRestriction,
        Reservation.TransferToCharge,
        PropertyMultiplier.Module, PropertyMultiplier.Edit,
        ReservationRateRule.Module, ReservationRateRule.Create, ReservationRateRule.Edit,
        Notification.Module, Notification.BorcHatirlatma,
    ];

    public static readonly IReadOnlyList<string> KiraciYoneticisiIzinleri =
    [
        TenantPortal.Lease.Module,
        TenantPortal.Charge.Module,
        TenantPortal.Payment.Module,
        TenantPortal.Reservation.Module, TenantPortal.Reservation.Create, TenantPortal.Reservation.Cancel,
        TenantPortal.System.User.Module, TenantPortal.System.User.Invite,
        TenantPortal.System.User.Edit, TenantPortal.System.User.Deactivate,
        TenantPortal.System.Role.Module, TenantPortal.System.Role.Create,
        TenantPortal.System.Role.Edit, TenantPortal.System.Role.Delete,
    ];

    public static readonly IReadOnlyList<string> KiraciSorumlusuIzinleri =
    [
        TenantPortal.Lease.Module,
        TenantPortal.Charge.Module,
        TenantPortal.Payment.Module,
        TenantPortal.Reservation.Module, TenantPortal.Reservation.Create, TenantPortal.Reservation.Cancel,
    ];

    public static readonly IReadOnlyList<string> TenantAll = KiraciYoneticisiIzinleri;

    public static readonly IReadOnlyList<string> All =
    [
        // Internal.*
        Property.Module, Property.Create, Property.Edit,
        Unit.Module, Unit.Create, Unit.Edit, Unit.OverrideRate,
        Tenant.Module, Tenant.Create, Tenant.Edit,
        Lease.Module, Lease.Create, Lease.Edit, Lease.Extend, Lease.Terminate, Lease.OverrideRate,
        Lease.Approve, Lease.RequestRevision, Lease.DeleteDraft,
        Payment.Module, Payment.Create, Payment.UploadReceipt, Payment.Approve, Payment.Reject,
        Payment.ImportBankStatement, Payment.MatchBankTransaction,
        ChargeType.Module, ChargeType.Create, ChargeType.Edit,
        Store.Module, Store.Create, Store.Edit, Store.Account,
        PaymentRouting.Module, PaymentRouting.Create, PaymentRouting.Edit,
        Parameter.Module, Parameter.Edit,
        PropertyType.Module, PropertyType.Create, PropertyType.Edit,
        UnitType.Module, UnitType.Create, UnitType.Edit,
        TenantCategory.Module, TenantCategory.Create, TenantCategory.Edit,
        Sector.Module, Sector.Create, Sector.Edit,
        DocumentType.Module, DocumentType.Create, DocumentType.Edit, DocumentType.Delete,
        RateSchedule.Module, RateSchedule.Create, RateSchedule.Edit,
        Charge.Module, Charge.Regenerate,
        ManualCharge.Module, ManualCharge.Create, ManualCharge.Cancel,
        Reservation.Module, Reservation.Create, Reservation.Edit, Reservation.Cancel,
        Reservation.Approve, Reservation.Reject, Reservation.OverrideTimeRestriction,
        Reservation.TransferToCharge,
        PropertyMultiplier.Module, PropertyMultiplier.Edit,
        ReservationRateRule.Module, ReservationRateRule.Create, ReservationRateRule.Edit,
        Notification.Module, Notification.BorcHatirlatma,
        // System.*
        User.Module, User.Create, User.Edit, User.AssignPermission,
        Role.Module, Role.Create, Role.Edit, Role.Delete,
        Invitation.Module, Invitation.Create, Invitation.Cancel, Invitation.Resend,
        Audit.Module,
    ];
}
