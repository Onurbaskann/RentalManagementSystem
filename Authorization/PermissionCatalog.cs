namespace KiraTakip.Authorization;

public record PermissionModuleInfo(string Path, string DisplayName, IReadOnlyList<string> Actions);

public static class PermissionCatalog
{
    // ─── İç Ekip Operasyonel (Internal.*) ────────────────────────────────────

    public static class Tasinmaz
    {
        public const string Module = "Internal.Tasinmaz";
        public const string Create = "Internal.Tasinmaz.Create";
        public const string Edit   = "Internal.Tasinmaz.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class Birim
    {
        public const string Module      = "Internal.Birim";
        public const string Create      = "Internal.Birim.Create";
        public const string Edit        = "Internal.Birim.Edit";
        public const string OverrideRate = "Internal.Birim.OverrideRate";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit, OverrideRate];
    }

    public static class Kiraci
    {
        public const string Module = "Internal.Kiraci";
        public const string Create = "Internal.Kiraci.Create";
        public const string Edit   = "Internal.Kiraci.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class Sozlesme
    {
        public const string Module       = "Internal.Sozlesme";
        public const string Create       = "Internal.Sozlesme.Create";
        public const string Edit         = "Internal.Sozlesme.Edit";
        public const string Extend       = "Internal.Sozlesme.Extend";
        public const string Terminate    = "Internal.Sozlesme.Terminate";
        public const string OverrideRate = "Internal.Sozlesme.OverrideRate";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit, Extend, Terminate, OverrideRate];
    }

    public static class Odeme
    {
        public const string Module               = "Internal.Odeme";
        public const string Create               = "Internal.Odeme.Create";
        public const string UploadDekont         = "Internal.Odeme.UploadDekont";
        public const string Approve              = "Internal.Odeme.Approve";
        public const string Reject               = "Internal.Odeme.Reject";
        public const string ImportBankStatement  = "Internal.Odeme.ImportBankStatement";
        public const string MatchBankTransaction = "Internal.Odeme.MatchBankTransaction";
        public static readonly IReadOnlyList<string> Actions = [Create, UploadDekont, Approve, Reject, ImportBankStatement, MatchBankTransaction];
    }

    public static class BorcTipi
    {
        public const string Module = "Internal.BorcTipi";
        public const string Create = "Internal.BorcTipi.Create";
        public const string Edit   = "Internal.BorcTipi.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class Parametre
    {
        public const string Module = "Internal.Parametre";
        public const string Edit   = "Internal.Parametre.Edit";
        public static readonly IReadOnlyList<string> Actions = [Edit];
    }

    public static class TasinmazTipi
    {
        public const string Module = "Internal.TasinmazTipi";
        public const string Create = "Internal.TasinmazTipi.Create";
        public const string Edit   = "Internal.TasinmazTipi.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class BirimTuru
    {
        public const string Module = "Internal.BirimTuru";
        public const string Create = "Internal.BirimTuru.Create";
        public const string Edit   = "Internal.BirimTuru.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class KiraciKategori
    {
        public const string Module = "Internal.KiraciKategori";
        public const string Create = "Internal.KiraciKategori.Create";
        public const string Edit   = "Internal.KiraciKategori.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class Sektor
    {
        public const string Module = "Internal.Sektor";
        public const string Create = "Internal.Sektor.Create";
        public const string Edit   = "Internal.Sektor.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class BelgeTuru
    {
        public const string Module = "Internal.BelgeTuru";
        public const string Create = "Internal.BelgeTuru.Create";
        public const string Edit   = "Internal.BelgeTuru.Edit";
        public const string Delete = "Internal.BelgeTuru.Delete";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit, Delete];
    }

    public static class Tarife
    {
        public const string Module = "Internal.Tarife";
        public const string Create = "Internal.Tarife.Create";
        public const string Edit   = "Internal.Tarife.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class Tahakkuk
    {
        public const string Module     = "Internal.Tahakkuk";
        public const string Regenerate = "Internal.Tahakkuk.Regenerate";
        public static readonly IReadOnlyList<string> Actions = [Regenerate];
    }

    public static class ManuelBorc
    {
        public const string Module = "Internal.ManuelBorc";
        public const string Create = "Internal.ManuelBorc.Create";
        public const string Cancel = "Internal.ManuelBorc.Cancel";
        public static readonly IReadOnlyList<string> Actions = [Create, Cancel];
    }

    public static class Rezervasyon
    {
        public const string Module             = "Internal.Rezervasyon";
        public const string Create             = "Internal.Rezervasyon.Create";
        public const string Edit               = "Internal.Rezervasyon.Edit";
        public const string Cancel             = "Internal.Rezervasyon.Cancel";
        public const string TransferToTahakkuk = "Internal.Rezervasyon.TransferToTahakkuk";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit, Cancel, TransferToTahakkuk];
    }

    public static class TasinmazCarpan
    {
        public const string Module = "Internal.TasinmazCarpan";
        public const string Edit   = "Internal.TasinmazCarpan.Edit";
        public static readonly IReadOnlyList<string> Actions = [Edit];
    }

    public static class RezervasyonTarifeKural
    {
        public const string Module = "Internal.RezervasyonTarifeKural";
        public const string Create = "Internal.RezervasyonTarifeKural.Create";
        public const string Edit   = "Internal.RezervasyonTarifeKural.Edit";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit];
    }

    public static class Bildirim
    {
        public const string Module         = "Internal.Bildirim";
        public const string BorcHatirlatma = "Internal.Bildirim.BorcHatirlatma";
        public static readonly IReadOnlyList<string> Actions = [BorcHatirlatma];
    }

    // ─── Sistem Yönetimi (System.*) ───────────────────────────────────────────

    public static class Kullanici
    {
        public const string Module           = "System.Kullanici";
        public const string Create           = "System.Kullanici.Create";
        public const string Edit             = "System.Kullanici.Edit";
        public const string AssignPermission = "System.Kullanici.AssignPermission";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit, AssignPermission];
    }

    public static class Rol
    {
        public const string Module = "System.Rol";
        public const string Create = "System.Rol.Create";
        public const string Edit   = "System.Rol.Edit";
        public const string Delete = "System.Rol.Delete";
        public static readonly IReadOnlyList<string> Actions = [Create, Edit, Delete];
    }

    public static class Davetiye
    {
        public const string Module = "System.Davetiye";
        public const string Create = "System.Davetiye.Create";
        public const string Cancel = "System.Davetiye.Cancel";
        public const string Resend = "System.Davetiye.Resend";
        public static readonly IReadOnlyList<string> Actions = [Create, Cancel, Resend];
    }

    public static class Audit
    {
        public const string Module = "System.Audit";
        public static readonly IReadOnlyList<string> Actions = [];
    }

    // ─── Kiracı Portal (Kiraci.*) ─────────────────────────────────────────────

    public static class KiraciPortal
    {
        public static class Sozlesme
        {
            public const string Module = "Kiraci.Sozlesme";
            public static readonly IReadOnlyList<string> Actions = [];
        }

        public static class Borc
        {
            public const string Module = "Kiraci.Borc";
            public static readonly IReadOnlyList<string> Actions = [];
        }

        public static class Odeme
        {
            public const string Module = "Kiraci.Odeme";
            public static readonly IReadOnlyList<string> Actions = [];
        }

        public static class Cari
        {
            public const string Module = "Kiraci.Cari";
            public static readonly IReadOnlyList<string> Actions = [];
        }

        public static class Mutabakat
        {
            public const string Module = "Kiraci.Mutabakat";
            public static readonly IReadOnlyList<string> Actions = [];
        }

        public static class Rezervasyon
        {
            public const string Module = "Kiraci.Rezervasyon";
            public const string Create = "Kiraci.Rezervasyon.Create";
            public const string Cancel = "Kiraci.Rezervasyon.Cancel";
            public static readonly IReadOnlyList<string> Actions = [Create, Cancel];
        }

        public static class System
        {
            public static class Kullanici
            {
                public const string Module     = "Kiraci.System.Kullanici";
                public const string Invite     = "Kiraci.System.Kullanici.Invite";
                public const string Edit       = "Kiraci.System.Kullanici.Edit";
                public const string Deactivate = "Kiraci.System.Kullanici.Deactivate";
                public static readonly IReadOnlyList<string> Actions = [Invite, Edit, Deactivate];
            }

            public static class Rol
            {
                public const string Module = "Kiraci.System.Rol";
                public const string Create = "Kiraci.System.Rol.Create";
                public const string Edit   = "Kiraci.System.Rol.Edit";
                public const string Delete = "Kiraci.System.Rol.Delete";
                public static readonly IReadOnlyList<string> Actions = [Create, Edit, Delete];
            }
        }
    }

    // ─── AllModules (UI Tree + SistemYoneticisi claims) ──────────────────────

    public static readonly IReadOnlyList<PermissionModuleInfo> AllModules =
    [
        // Internal
        new(Tasinmaz.Module,               "Taşınmaz",                   Tasinmaz.Actions),
        new(Birim.Module,                  "Birim",                      Birim.Actions),
        new(Kiraci.Module,                 "Kiracı",                     Kiraci.Actions),
        new(Sozlesme.Module,               "Sözleşme",                   Sozlesme.Actions),
        new(Odeme.Module,                  "Ödeme",                      Odeme.Actions),
        new(ManuelBorc.Module,             "Manuel Borç",                ManuelBorc.Actions),
        new(Rezervasyon.Module,            "Rezervasyon",                Rezervasyon.Actions),
        new(Tahakkuk.Module,               "Tahakkuk",                   Tahakkuk.Actions),
        new(BorcTipi.Module,               "Borç Tipi",                  BorcTipi.Actions),
        new(Parametre.Module,              "Parametre",                  Parametre.Actions),
        new(TasinmazTipi.Module,           "Taşınmaz Tipi",              TasinmazTipi.Actions),
        new(BirimTuru.Module,              "Birim Türü",                 BirimTuru.Actions),
        new(KiraciKategori.Module,         "Kiracı Kategorisi",          KiraciKategori.Actions),
        new(Sektor.Module,                 "Sektör",                     Sektor.Actions),
        new(BelgeTuru.Module,              "Belge Türü",                 BelgeTuru.Actions),
        new(Tarife.Module,                 "Tarife",                     Tarife.Actions),
        new(TasinmazCarpan.Module,         "Taşınmaz Çarpanı",           TasinmazCarpan.Actions),
        new(RezervasyonTarifeKural.Module, "Rezervasyon Tarife Kuralı",  RezervasyonTarifeKural.Actions),
        new(Bildirim.Module,               "Bildirim",                   Bildirim.Actions),
        // System
        new(Kullanici.Module,              "Kullanıcı",                  Kullanici.Actions),
        new(Rol.Module,                    "Rol",                        Rol.Actions),
        new(Davetiye.Module,               "Davetiye",                   Davetiye.Actions),
        new(Audit.Module,                  "Hareket Geçmişi",            Audit.Actions),
        // Kiraci Portal
        new(KiraciPortal.Sozlesme.Module,           "Kiracı — Sözleşme",          KiraciPortal.Sozlesme.Actions),
        new(KiraciPortal.Borc.Module,               "Kiracı — Borç",              KiraciPortal.Borc.Actions),
        new(KiraciPortal.Odeme.Module,              "Kiracı — Ödeme",             KiraciPortal.Odeme.Actions),
        new(KiraciPortal.Rezervasyon.Module,        "Kiracı — Rezervasyon",       KiraciPortal.Rezervasyon.Actions),
        new(KiraciPortal.System.Kullanici.Module,   "Kiracı Yönetim — Kullanıcı", KiraciPortal.System.Kullanici.Actions),
        new(KiraciPortal.System.Rol.Module,         "Kiracı Yönetim — Rol",       KiraciPortal.System.Rol.Actions),
    ];

    // ─── Kapsam Farkındalığı (row-level scope) ───────────────────────────────

    public static readonly IReadOnlyList<string> ScopeAware =
    [
        Tasinmaz.Module, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.Module, Birim.Create, Birim.Edit, Birim.OverrideRate,
        Kiraci.Module, Kiraci.Create, Kiraci.Edit,
        Sozlesme.Module, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate, Sozlesme.OverrideRate,
        Odeme.Module, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject, Odeme.MatchBankTransaction,
        ManuelBorc.Module, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.Module, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        Tahakkuk.Module, Tahakkuk.Regenerate,
        TasinmazCarpan.Module, TasinmazCarpan.Edit,
        RezervasyonTarifeKural.Module, RezervasyonTarifeKural.Create, RezervasyonTarifeKural.Edit,
    ];

    public static bool IsScopeAware(string permission) => ScopeAware.Contains(permission);

    // ─── Preset Listeler ──────────────────────────────────────────────────────

    public static readonly IReadOnlyList<string> OperasyonMuduruIzinleri =
    [
        Tasinmaz.Module, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.Module, Birim.Create, Birim.Edit, Birim.OverrideRate,
        Kiraci.Module, Kiraci.Create, Kiraci.Edit,
        Sozlesme.Module, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate, Sozlesme.OverrideRate,
        Odeme.Module, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject,
        Odeme.ImportBankStatement, Odeme.MatchBankTransaction,
        BorcTipi.Module, BorcTipi.Create, BorcTipi.Edit,
        Tarife.Module, Tarife.Create, Tarife.Edit,
        Tahakkuk.Module, Tahakkuk.Regenerate,
        Parametre.Module, Parametre.Edit,
        TasinmazTipi.Module, TasinmazTipi.Create, TasinmazTipi.Edit,
        BirimTuru.Module, BirimTuru.Create, BirimTuru.Edit,
        KiraciKategori.Module, KiraciKategori.Create, KiraciKategori.Edit,
        Sektor.Module, Sektor.Create, Sektor.Edit,
        BelgeTuru.Module, BelgeTuru.Create, BelgeTuru.Edit, BelgeTuru.Delete,
        ManuelBorc.Module, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.Module, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        TasinmazCarpan.Module, TasinmazCarpan.Edit,
        RezervasyonTarifeKural.Module, RezervasyonTarifeKural.Create, RezervasyonTarifeKural.Edit,
        Bildirim.Module, Bildirim.BorcHatirlatma,
    ];

    public static readonly IReadOnlyList<string> KiraciYoneticisiIzinleri =
    [
        KiraciPortal.Sozlesme.Module,
        KiraciPortal.Borc.Module,
        KiraciPortal.Odeme.Module,
        KiraciPortal.Rezervasyon.Module, KiraciPortal.Rezervasyon.Create, KiraciPortal.Rezervasyon.Cancel,
        KiraciPortal.System.Kullanici.Module, KiraciPortal.System.Kullanici.Invite,
        KiraciPortal.System.Kullanici.Edit, KiraciPortal.System.Kullanici.Deactivate,
        KiraciPortal.System.Rol.Module, KiraciPortal.System.Rol.Create,
        KiraciPortal.System.Rol.Edit, KiraciPortal.System.Rol.Delete,
    ];

    public static readonly IReadOnlyList<string> KiraciSorumlusuIzinleri =
    [
        KiraciPortal.Sozlesme.Module,
        KiraciPortal.Borc.Module,
        KiraciPortal.Odeme.Module,
        KiraciPortal.Rezervasyon.Module, KiraciPortal.Rezervasyon.Create, KiraciPortal.Rezervasyon.Cancel,
    ];

    public static readonly IReadOnlyList<string> KiraciAll = KiraciYoneticisiIzinleri;

    public static readonly IReadOnlyList<string> All =
    [
        // Internal.*
        Tasinmaz.Module, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.Module, Birim.Create, Birim.Edit, Birim.OverrideRate,
        Kiraci.Module, Kiraci.Create, Kiraci.Edit,
        Sozlesme.Module, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate, Sozlesme.OverrideRate,
        Odeme.Module, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject,
        Odeme.ImportBankStatement, Odeme.MatchBankTransaction,
        BorcTipi.Module, BorcTipi.Create, BorcTipi.Edit,
        Parametre.Module, Parametre.Edit,
        TasinmazTipi.Module, TasinmazTipi.Create, TasinmazTipi.Edit,
        BirimTuru.Module, BirimTuru.Create, BirimTuru.Edit,
        KiraciKategori.Module, KiraciKategori.Create, KiraciKategori.Edit,
        Sektor.Module, Sektor.Create, Sektor.Edit,
        BelgeTuru.Module, BelgeTuru.Create, BelgeTuru.Edit, BelgeTuru.Delete,
        Tarife.Module, Tarife.Create, Tarife.Edit,
        Tahakkuk.Module, Tahakkuk.Regenerate,
        ManuelBorc.Module, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.Module, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        TasinmazCarpan.Module, TasinmazCarpan.Edit,
        RezervasyonTarifeKural.Module, RezervasyonTarifeKural.Create, RezervasyonTarifeKural.Edit,
        Bildirim.Module, Bildirim.BorcHatirlatma,
        // System.*
        Kullanici.Module, Kullanici.Create, Kullanici.Edit, Kullanici.AssignPermission,
        Rol.Module, Rol.Create, Rol.Edit, Rol.Delete,
        Davetiye.Module, Davetiye.Create, Davetiye.Cancel, Davetiye.Resend,
        Audit.Module,
    ];
}
