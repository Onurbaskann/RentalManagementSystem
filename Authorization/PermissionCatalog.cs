namespace KiraTakip.Authorization;

public static class PermissionCatalog
{
    // ─── İç Ekip Operasyonel (Internal.*) ────────────────────────────────────

    public static class Tasinmaz
    {
        public const string View   = "Internal.Tasinmaz.View";
        public const string Create = "Internal.Tasinmaz.Create";
        public const string Edit   = "Internal.Tasinmaz.Edit";
    }

    public static class Birim
    {
        public const string View       = "Internal.Birim.View";
        public const string Create     = "Internal.Birim.Create";
        public const string Edit       = "Internal.Birim.Edit";
        public const string ManageRate = "Internal.Birim.ManageRate";
    }

    public static class Kiraci
    {
        public const string View   = "Internal.Kiraci.View";
        public const string Create = "Internal.Kiraci.Create";
        public const string Edit   = "Internal.Kiraci.Edit";
    }

    public static class Sozlesme
    {
        public const string View         = "Internal.Sozlesme.View";
        public const string Create       = "Internal.Sozlesme.Create";
        public const string Edit         = "Internal.Sozlesme.Edit";
        public const string Extend       = "Internal.Sozlesme.Extend";
        public const string Terminate    = "Internal.Sozlesme.Terminate";
        public const string OverrideRate = "Internal.Sozlesme.OverrideRate";
    }

    public static class Odeme
    {
        public const string View                 = "Internal.Odeme.View";
        public const string Create               = "Internal.Odeme.Create";
        public const string UploadDekont         = "Internal.Odeme.UploadDekont";
        public const string Approve              = "Internal.Odeme.Approve";
        public const string Reject               = "Internal.Odeme.Reject";
        public const string ImportBankStatement  = "Internal.Odeme.ImportBankStatement";
        public const string MatchBankTransaction = "Internal.Odeme.MatchBankTransaction";
    }

    public static class BorcTipi
    {
        public const string Manage = "Internal.BorcTipi.Manage";
    }

    public static class Parametre
    {
        public const string View   = "Internal.Parametre.View";
        public const string Manage = "Internal.Parametre.Manage";
    }

    public static class TasinmazTipi
    {
        public const string View   = "Internal.TasinmazTipi.View";
        public const string Manage = "Internal.TasinmazTipi.Manage";
    }

    public static class BirimTuru
    {
        public const string View   = "Internal.BirimTuru.View";
        public const string Manage = "Internal.BirimTuru.Manage";
    }

    public static class KiraciKategori
    {
        public const string View   = "Internal.KiraciKategori.View";
        public const string Manage = "Internal.KiraciKategori.Manage";
    }

    public static class Sektor
    {
        public const string View   = "Internal.Sektor.View";
        public const string Manage = "Internal.Sektor.Manage";
    }

    public static class BelgeTuru
    {
        public const string View   = "Internal.BelgeTuru.View";
        public const string Manage = "Internal.BelgeTuru.Manage";
    }

    public static class Tarife
    {
        public const string View   = "Internal.Tarife.View";
        public const string Manage = "Internal.Tarife.Manage";
    }

    public static class Tahakkuk
    {
        public const string Regenerate = "Internal.Tahakkuk.Regenerate";
    }

    public static class ManuelBorc
    {
        public const string View   = "Internal.ManuelBorc.View";
        public const string Create = "Internal.ManuelBorc.Create";
        public const string Cancel = "Internal.ManuelBorc.Cancel";
    }

    public static class Rezervasyon
    {
        public const string View               = "Internal.Rezervasyon.View";
        public const string Create             = "Internal.Rezervasyon.Create";
        public const string Edit               = "Internal.Rezervasyon.Edit";
        public const string Cancel             = "Internal.Rezervasyon.Cancel";
        public const string TransferToTahakkuk = "Internal.Rezervasyon.TransferToTahakkuk";
    }

    public static class TasinmazCarpan
    {
        public const string View   = "Internal.TasinmazCarpan.View";
        public const string Manage = "Internal.TasinmazCarpan.Manage";
    }

    public static class RezervasyonTarifeKural
    {
        public const string Manage = "Internal.RezervasyonTarifeKural.Manage";
    }

    public static class Bildirim
    {
        public const string BorcHatirlatma = "Internal.Bildirim.BorcHatirlatma";
    }

    // ─── Sistem Yönetimi (System.*) ───────────────────────────────────────────

    public static class Kullanici
    {
        public const string View             = "System.Kullanici.View";
        public const string Create           = "System.Kullanici.Create";
        public const string Edit             = "System.Kullanici.Edit";
        public const string AssignPermission = "System.Kullanici.AssignPermission";
    }

    public static class Rol
    {
        public const string View   = "System.Rol.View";
        public const string Create = "System.Rol.Create";
        public const string Edit   = "System.Rol.Edit";
        public const string Delete = "System.Rol.Delete";
    }

    public static class Davetiye
    {
        public const string View   = "System.Davetiye.View";
        public const string Create = "System.Davetiye.Create";
        public const string Cancel = "System.Davetiye.Cancel";
        public const string Resend = "System.Davetiye.Resend";
    }

    public static class Audit
    {
        public const string View = "System.Audit.View";
    }

    // ─── Kiracı Portal (Kiraci.*) ─────────────────────────────────────────────

    public static class KiraciPortal
    {
        public static class Sozlesme
        {
            public const string View = "Kiraci.Sozlesme.View";
        }

        public static class Borc
        {
            public const string View = "Kiraci.Borc.View";
        }

        public static class Odeme
        {
            public const string View = "Kiraci.Odeme.View";
        }

        public static class Cari
        {
            public const string View = "Kiraci.Cari.View";
        }

        public static class Mutabakat
        {
            public const string Manage = "Kiraci.Mutabakat.Manage";
        }

        public static class Rezervasyon
        {
            public const string View   = "Kiraci.Rezervasyon.View";
            public const string Create = "Kiraci.Rezervasyon.Create";
            public const string Cancel = "Kiraci.Rezervasyon.Cancel";
        }

        // Kiracı firma içi yönetim (Kiraci.System.*)
        public static class Kullanici
        {
            public const string View       = "Kiraci.System.Kullanici.View";
            public const string Invite     = "Kiraci.System.Kullanici.Invite";
            public const string Edit       = "Kiraci.System.Kullanici.Edit";
            public const string Deactivate = "Kiraci.System.Kullanici.Deactivate";
            public const string Manage     = "Kiraci.System.Kullanici.Manage";
        }

        public static class Rol
        {
            public const string View   = "Kiraci.System.Rol.View";
            public const string Create = "Kiraci.System.Rol.Create";
            public const string Edit   = "Kiraci.System.Rol.Edit";
            public const string Delete = "Kiraci.System.Rol.Delete";
        }
    }

    // ─── Sabit Listeler ───────────────────────────────────────────────────────

    public static readonly IReadOnlyList<string> ScopeAware =
    [
        Tasinmaz.View, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.View, Birim.Create, Birim.Edit, Birim.ManageRate,
        Kiraci.View, Kiraci.Create, Kiraci.Edit,
        Sozlesme.View, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate, Sozlesme.OverrideRate,
        Odeme.View, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject, Odeme.MatchBankTransaction,
        ManuelBorc.View, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.View, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        Tahakkuk.Regenerate,
        TasinmazCarpan.View, TasinmazCarpan.Manage,
        RezervasyonTarifeKural.Manage,
    ];

    public static bool IsScopeAware(string permission) => ScopeAware.Contains(permission);

    // Tüm Internal.* izinler — Operasyon Müdürü için
    public static readonly IReadOnlyList<string> OperasyonMuduruIzinleri =
    [
        Tasinmaz.View, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.View, Birim.Create, Birim.Edit, Birim.ManageRate,
        Kiraci.View, Kiraci.Create, Kiraci.Edit,
        Sozlesme.View, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate, Sozlesme.OverrideRate,
        Odeme.View, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject,
        Odeme.ImportBankStatement, Odeme.MatchBankTransaction,
        BorcTipi.Manage,
        Tarife.View, Tarife.Manage,
        Tahakkuk.Regenerate,
        Parametre.View, Parametre.Manage,
        TasinmazTipi.View, TasinmazTipi.Manage,
        BirimTuru.View, BirimTuru.Manage,
        KiraciKategori.View, KiraciKategori.Manage,
        Sektor.View, Sektor.Manage,
        BelgeTuru.View, BelgeTuru.Manage,
        ManuelBorc.View, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.View, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        TasinmazCarpan.View, TasinmazCarpan.Manage,
        RezervasyonTarifeKural.Manage,
        Bildirim.BorcHatirlatma,
    ];

    // Kiraci.* + Kiraci.System.* — Kiracı Yöneticisi için
    public static readonly IReadOnlyList<string> KiraciYoneticisiIzinleri =
    [
        KiraciPortal.Sozlesme.View,
        KiraciPortal.Borc.View,
        KiraciPortal.Odeme.View,
        KiraciPortal.Cari.View,
        KiraciPortal.Mutabakat.Manage,
        KiraciPortal.Rezervasyon.View, KiraciPortal.Rezervasyon.Create, KiraciPortal.Rezervasyon.Cancel,
        KiraciPortal.Kullanici.View, KiraciPortal.Kullanici.Invite, KiraciPortal.Kullanici.Edit,
        KiraciPortal.Kullanici.Deactivate, KiraciPortal.Kullanici.Manage,
        KiraciPortal.Rol.View, KiraciPortal.Rol.Create, KiraciPortal.Rol.Edit, KiraciPortal.Rol.Delete,
    ];

    // Kiraci.* (Kiraci.System.* hariç) — Kiracı Sorumlusu için
    public static readonly IReadOnlyList<string> KiraciSorumlusuIzinleri =
    [
        KiraciPortal.Sozlesme.View,
        KiraciPortal.Borc.View,
        KiraciPortal.Odeme.View,
        KiraciPortal.Cari.View,
        KiraciPortal.Mutabakat.Manage,
        KiraciPortal.Rezervasyon.View, KiraciPortal.Rezervasyon.Create, KiraciPortal.Rezervasyon.Cancel,
    ];

    // Tüm Kiraci.* izinler (policy kaydı + kiracı seed için)
    public static readonly IReadOnlyList<string> KiraciAll = KiraciYoneticisiIzinleri;

    // Tüm Internal.* + System.* izinler (policy kaydı + Admin bypass için)
    public static readonly IReadOnlyList<string> All =
    [
        // Internal.*
        Tasinmaz.View, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.View, Birim.Create, Birim.Edit, Birim.ManageRate,
        Kiraci.View, Kiraci.Create, Kiraci.Edit,
        Sozlesme.View, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate, Sozlesme.OverrideRate,
        Odeme.View, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject,
        Odeme.ImportBankStatement, Odeme.MatchBankTransaction,
        BorcTipi.Manage,
        Tarife.View, Tarife.Manage,
        Tahakkuk.Regenerate,
        Parametre.View, Parametre.Manage,
        TasinmazTipi.View, TasinmazTipi.Manage,
        BirimTuru.View, BirimTuru.Manage,
        KiraciKategori.View, KiraciKategori.Manage,
        Sektor.View, Sektor.Manage,
        BelgeTuru.View, BelgeTuru.Manage,
        ManuelBorc.View, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.View, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        TasinmazCarpan.View, TasinmazCarpan.Manage,
        RezervasyonTarifeKural.Manage,
        Bildirim.BorcHatirlatma,
        // System.*
        Kullanici.View, Kullanici.Create, Kullanici.Edit, Kullanici.AssignPermission,
        Rol.View, Rol.Create, Rol.Edit, Rol.Delete,
        Davetiye.View, Davetiye.Create, Davetiye.Cancel, Davetiye.Resend,
        Audit.View,
    ];
}
