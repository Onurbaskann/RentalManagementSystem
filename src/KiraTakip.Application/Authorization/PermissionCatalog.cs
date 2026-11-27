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

    public static class Space
    {
        public const string Module         = "Internal.Space";
        public const string Create         = "Internal.Space.Create";
        public const string Edit           = "Internal.Space.Edit";
        public const string Deactivate     = "Internal.Space.Deactivate";
        public const string Activate       = "Internal.Space.Activate";
        public const string Delete         = "Internal.Space.Delete";
        public const string EditRate       = "Internal.Space.EditRate";
        public const string EditAddress    = "Internal.Space.EditAddress";
        public const string EditAttributes = "Internal.Space.EditAttributes";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Deactivate, "Pasifleştir"), new(Activate, "Etkinleştir"), new(Delete, "Sil"), new(EditRate, "Tarife Düzenle"), new(EditAddress, "Adres Düzenle"), new(EditAttributes, "Özellik Düzenle")];
    }

    public static class SpaceType
    {
        public const string Module       = "Internal.SpaceType";
        public const string Create       = "Internal.SpaceType.Create";
        public const string Edit         = "Internal.SpaceType.Edit";
        public const string ToggleStatus = "Internal.SpaceType.ToggleStatus";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(ToggleStatus, "Aktif/Pasif")];
    }

    public static class SpaceAttribute
    {
        public const string Module       = "Internal.SpaceAttribute";
        public const string Create       = "Internal.SpaceAttribute.Create";
        public const string Edit         = "Internal.SpaceAttribute.Edit";
        public const string ToggleStatus = "Internal.SpaceAttribute.ToggleStatus";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(ToggleStatus, "Aktif/Pasif")];
    }

    public static class Tenant
    {
        public const string Module = "Internal.Tenant";
        public const string Create = "Internal.Tenant.Create";
        public const string Edit   = "Internal.Tenant.Edit";
        public const string Delete = "Internal.Tenant.Delete";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Delete, "Sil")];
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
        public const string CreateGroup = "Internal.Lease.CreateGroup";
        public const string UpdateGroup = "Internal.Lease.UpdateGroup";
        public const string ResubmitGroup = "Internal.Lease.ResubmitGroup";
        public const string DeleteGroupDraft = "Internal.Lease.DeleteGroupDraft";
        public const string ApproveGroup = "Internal.Lease.ApproveGroup";
        public const string ExtendGroup = "Internal.Lease.ExtendGroup";
        public const string TerminateGroup = "Internal.Lease.TerminateGroup";
        public const string RestructureGroup = "Internal.Lease.RestructureGroup";
        public const string UpdateDraft = "Internal.Lease.UpdateDraft";
        public const string ResubmitRevision = "Internal.Lease.ResubmitRevision";
        public const string UploadDraftDocument = "Internal.Lease.UploadDraftDocument";
        public const string UpdateDueDate = "Internal.Lease.UpdateDueDate";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Extend, "Süre Uzat"), new(Terminate, "Feshet"), new(OverrideRate, "Elle Müdahale"), new(Approve, "Onayla"), new(RequestRevision, "Revizyon İste"), new(DeleteDraft, "Başvuru Sil"), new(CreateGroup, "Paylaşımlı Grup Oluştur"), new(UpdateGroup, "Grup Taslağı Düzenle"), new(ResubmitGroup, "Grubu Yeniden Gönder"), new(DeleteGroupDraft, "Grup Taslağı Sil"), new(ApproveGroup, "Grup Onayla"), new(ExtendGroup, "Grubu Uzat"), new(TerminateGroup, "Grubu Feshet"), new(RestructureGroup, "Grubu Yeniden Yapılandır"), new(UpdateDraft, "Başvuru Taslağı Düzenle"), new(ResubmitRevision, "Revizyonu Yeniden Gönder"), new(UploadDraftDocument, "Başvuru Belgesi Yükle"), new(UpdateDueDate, "Vade Günü Düzenle")];
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
        public const string UnmatchBankTransaction = "Internal.Payment.UnmatchBankTransaction";
        public const string RemovePaymentMatch = "Internal.Payment.RemovePaymentMatch";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(UploadReceipt, "Dekont Yükle"), new(Approve, "Onayla"), new(Reject, "Reddet"), new(ImportBankStatement, "Banka Hareketleri İçe Aktar"), new(MatchBankTransaction, "Banka Hareketi Eşleştir"), new(UnmatchBankTransaction, "Banka Eşleşmesini Kaldır"), new(RemovePaymentMatch, "Ödeme Eşleşmesini Kaldır")];
    }

    public static class ChargeType
    {
        public const string Module = "Internal.ChargeType";
        public const string Create = "Internal.ChargeType.Create";
        public const string Edit   = "Internal.ChargeType.Edit";
        public const string ToggleStatus = "Internal.ChargeType.ToggleStatus";
        public const string ChangeSortOrder = "Internal.ChargeType.ChangeSortOrder";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(ToggleStatus, "Aktif/Pasif"), new(ChangeSortOrder, "Sıra Değiştir")];
    }

    public static class Store
    {
        public const string Module  = "Internal.Store";
        public const string Create  = "Internal.Store.Create";
        public const string Edit    = "Internal.Store.Edit";
        public const string ToggleStatus = "Internal.Store.ToggleStatus";
        public const string DeactivateAccount = "Internal.Store.DeactivateAccount";
        public const string ReplaceAccount = "Internal.Store.ReplaceAccount";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions =
            [new(Create, "Ekle"), new(Edit, "Düzenle"), new(ToggleStatus, "Aktif/Pasif"), new(DeactivateAccount, "Hesap Pasifleştir"), new(ReplaceAccount, "Hesap Değiştir")];
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

    public static class TenantCategory
    {
        public const string Module = "Internal.TenantCategory";
        public const string Create = "Internal.TenantCategory.Create";
        public const string Edit   = "Internal.TenantCategory.Edit";
        public const string ToggleStatus = "Internal.TenantCategory.ToggleStatus";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(ToggleStatus, "Aktif/Pasif")];
    }

    public static class Sector
    {
        public const string Module = "Internal.Sector";
        public const string Create = "Internal.Sector.Create";
        public const string Edit   = "Internal.Sector.Edit";
        public const string ToggleStatus = "Internal.Sector.ToggleStatus";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(ToggleStatus, "Aktif/Pasif")];
    }

    public static class DocumentType
    {
        public const string Module = "Internal.DocumentType";
        public const string Create = "Internal.DocumentType.Create";
        public const string Edit   = "Internal.DocumentType.Edit";
        public const string Delete = "Internal.DocumentType.Delete";
        public const string ToggleStatus = "Internal.DocumentType.ToggleStatus";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(Delete, "Sil"), new(ToggleStatus, "Aktif/Pasif")];
    }

    public static class RateSchedule
    {
        public const string Module = "Internal.RateSchedule";
        public const string Create = "Internal.RateSchedule.Create";
        public const string ToggleStatus = "Internal.RateSchedule.ToggleStatus";
        public const string UpdateLine = "Internal.RateSchedule.UpdateLine";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(ToggleStatus, "Aktif/Pasif"), new(UpdateLine, "Satır Güncelle")];
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

    public static class ReservationRateRule
    {
        public const string Module = "Internal.ReservationRateRule";
        public const string Create = "Internal.ReservationRateRule.Create";
        public const string Edit   = "Internal.ReservationRateRule.Edit";
        public const string ToggleStatus = "Internal.ReservationRateRule.ToggleStatus";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Create, "Ekle"), new(Edit, "Düzenle"), new(ToggleStatus, "Aktif/Pasif")];
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
        public const string Edit             = "System.User.Edit";
        public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Edit, "Düzenle")];
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
            public const string Report = "Tenant.Payment.Report";
            public const string StartOnline = "Tenant.Payment.StartOnline";
            public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Report, "Ödeme Bildir"), new(StartOnline, "Online Ödeme Başlat")];
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
                public const string CancelInvite = "Tenant.System.User.CancelInvite";
                public const string ResendInvite = "Tenant.System.User.ResendInvite";
                public static readonly IReadOnlyList<PermissionActionInfo> ActionDefinitions = [new(Invite, "Davet Et"), new(Edit, "Düzenle"), new(Deactivate, "Pasifleştir"), new(CancelInvite, "Daveti İptal Et"), new(ResendInvite, "Daveti Yeniden Gönder")];
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
        new(Space.Module,                  "Alan",                       Space.ActionDefinitions),
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
        new(SpaceType.Module,              "Alan Türü",                  SpaceType.ActionDefinitions, "Parametreler"),
        new(SpaceAttribute.Module,         "Alan Özelliği",              SpaceAttribute.ActionDefinitions, "Parametreler"),
        new(TenantCategory.Module,         "Kiracı Kategorisi",          TenantCategory.ActionDefinitions, "Parametreler"),
        new(Sector.Module,                 "Sektör",                     Sector.ActionDefinitions, "Parametreler"),
        new(DocumentType.Module,              "Belge Türü",              DocumentType.ActionDefinitions),
        new(RateSchedule.Module,                 "Yıllık Tarife",                     RateSchedule.ActionDefinitions),
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
        Tenant.Module, Tenant.Create, Tenant.Edit, Tenant.Delete,
        Lease.Module, Lease.Create, Lease.Edit, Lease.Extend, Lease.Terminate, Lease.OverrideRate,
        Lease.Approve, Lease.RequestRevision, Lease.DeleteDraft,
        Lease.UpdateDraft, Lease.ResubmitRevision, Lease.UploadDraftDocument, Lease.UpdateDueDate,
        Lease.CreateGroup, Lease.UpdateGroup, Lease.ResubmitGroup, Lease.DeleteGroupDraft, Lease.ApproveGroup,
        Lease.ExtendGroup, Lease.TerminateGroup, Lease.RestructureGroup,
        Payment.Module, Payment.Create, Payment.UploadReceipt, Payment.Approve, Payment.Reject, Payment.MatchBankTransaction,
        Payment.UnmatchBankTransaction, Payment.RemovePaymentMatch,
        ManualCharge.Module, ManualCharge.Create, ManualCharge.Cancel,
        Reservation.Module, Reservation.Create, Reservation.Edit, Reservation.Cancel,
        Reservation.Approve, Reservation.Reject, Reservation.OverrideTimeRestriction,
        Reservation.TransferToCharge,
        Charge.Module, Charge.Regenerate,
        ReservationRateRule.Module, ReservationRateRule.Create, ReservationRateRule.Edit, ReservationRateRule.ToggleStatus,
        Space.Module, Space.Create, Space.Edit, Space.Deactivate, Space.Activate, Space.Delete,
        Space.EditRate, Space.EditAddress, Space.EditAttributes,
        Notification.Module, Notification.BorcHatirlatma,
    ];

    public static bool IsScopeAware(string permission) => ScopeAware.Contains(permission);

    public static readonly IReadOnlyList<string> TenantAll =
    [
        TenantPortal.Lease.Module,
        TenantPortal.Charge.Module,
        TenantPortal.Payment.Module, TenantPortal.Payment.Report, TenantPortal.Payment.StartOnline,
        TenantPortal.Reservation.Module, TenantPortal.Reservation.Create, TenantPortal.Reservation.Cancel,
        TenantPortal.System.User.Module, TenantPortal.System.User.Invite,
        TenantPortal.System.User.Edit, TenantPortal.System.User.Deactivate,
        TenantPortal.System.User.CancelInvite, TenantPortal.System.User.ResendInvite,
        TenantPortal.System.Role.Module, TenantPortal.System.Role.Create,
        TenantPortal.System.Role.Edit, TenantPortal.System.Role.Delete,
    ];

    public static readonly IReadOnlyList<string> InternalAll =
    [
        // Internal.*
        Space.Module, Space.Create, Space.Edit, Space.Deactivate, Space.Activate, Space.Delete,
        Space.EditRate, Space.EditAddress, Space.EditAttributes,
        Tenant.Module, Tenant.Create, Tenant.Edit, Tenant.Delete,
        Lease.Module, Lease.Create, Lease.Edit, Lease.Extend, Lease.Terminate, Lease.OverrideRate,
        Lease.Approve, Lease.RequestRevision, Lease.DeleteDraft,
        Lease.UpdateDraft, Lease.ResubmitRevision, Lease.UploadDraftDocument, Lease.UpdateDueDate,
        Lease.CreateGroup, Lease.UpdateGroup, Lease.ResubmitGroup, Lease.DeleteGroupDraft, Lease.ApproveGroup,
        Lease.ExtendGroup, Lease.TerminateGroup, Lease.RestructureGroup,
        Payment.Module, Payment.Create, Payment.UploadReceipt, Payment.Approve, Payment.Reject,
        Payment.ImportBankStatement, Payment.MatchBankTransaction,
        Payment.UnmatchBankTransaction, Payment.RemovePaymentMatch,
        ChargeType.Module, ChargeType.Create, ChargeType.Edit,
        ChargeType.ToggleStatus, ChargeType.ChangeSortOrder,
        Store.Module, Store.Create, Store.Edit,
        Store.ToggleStatus, Store.DeactivateAccount, Store.ReplaceAccount,
        PaymentRouting.Module, PaymentRouting.Create, PaymentRouting.Edit,
        Parameter.Module, Parameter.Edit,
        SpaceType.Module, SpaceType.Create, SpaceType.Edit, SpaceType.ToggleStatus,
        SpaceAttribute.Module, SpaceAttribute.Create, SpaceAttribute.Edit, SpaceAttribute.ToggleStatus,
        TenantCategory.Module, TenantCategory.Create, TenantCategory.Edit, TenantCategory.ToggleStatus,
        Sector.Module, Sector.Create, Sector.Edit, Sector.ToggleStatus,
        DocumentType.Module, DocumentType.Create, DocumentType.Edit, DocumentType.Delete, DocumentType.ToggleStatus,
        RateSchedule.Module, RateSchedule.Create, RateSchedule.ToggleStatus, RateSchedule.UpdateLine,
        Charge.Module, Charge.Regenerate,
        ManualCharge.Module, ManualCharge.Create, ManualCharge.Cancel,
        Reservation.Module, Reservation.Create, Reservation.Edit, Reservation.Cancel,
        Reservation.Approve, Reservation.Reject, Reservation.OverrideTimeRestriction,
        Reservation.TransferToCharge,
        ReservationRateRule.Module, ReservationRateRule.Create, ReservationRateRule.Edit, ReservationRateRule.ToggleStatus,
        Notification.Module, Notification.BorcHatirlatma,
        // System.*
        User.Module, User.Edit,
        Role.Module, Role.Create, Role.Edit, Role.Delete,
        Invitation.Module, Invitation.Create, Invitation.Cancel, Invitation.Resend,
        Audit.Module,
    ];
}
