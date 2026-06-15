namespace KiraTakip.Authorization;

public static class PermissionCatalog
{
    public static class Tasinmaz
    {
        public const string View = "Tasinmaz.View";
        public const string Create = "Tasinmaz.Create";
        public const string Edit = "Tasinmaz.Edit";
    }

    public static class Birim
    {
        public const string View = "Birim.View";
        public const string Create = "Birim.Create";
        public const string Edit = "Birim.Edit";
        public const string ManageRate = "Birim.ManageRate";
    }

    public static class Kiraci
    {
        public const string View = "Kiraci.View";
        public const string Create = "Kiraci.Create";
        public const string Edit = "Kiraci.Edit";
    }

    public static class Sozlesme
    {
        public const string View = "Sozlesme.View";
        public const string Create = "Sozlesme.Create";
        public const string Edit = "Sozlesme.Edit";
        public const string Extend = "Sozlesme.Extend";
        public const string Terminate = "Sozlesme.Terminate";
        public const string OverrideRate = "Sozlesme.OverrideRate";
    }

    public static class Odeme
    {
        public const string View = "Odeme.View";
        public const string Create = "Odeme.Create";
        public const string UploadDekont = "Odeme.UploadDekont";
        public const string Approve = "Odeme.Approve";
        public const string Reject = "Odeme.Reject";
        public const string ImportBankStatement = "Odeme.ImportBankStatement";
        public const string MatchBankTransaction = "Odeme.MatchBankTransaction";
    }

    public static class BorcTipi
    {
        public const string Manage = "BorcTipi.Manage";
    }

    public static class Parametre
    {
        public const string View = "Parametre.View";
        public const string Manage = "Parametre.Manage";
    }

    public static class TasinmazTipiPerm
    {
        public const string View = "TasinmazTipi.View";
        public const string Manage = "TasinmazTipi.Manage";
    }

    public static class BirimTuruPerm
    {
        public const string View = "BirimTuru.View";
        public const string Manage = "BirimTuru.Manage";
    }

    public static class KiraciKategoriPerm
    {
        public const string View = "KiraciKategori.View";
        public const string Manage = "KiraciKategori.Manage";
    }

    public static class SektorPerm
    {
        public const string View = "Sektor.View";
        public const string Manage = "Sektor.Manage";
    }

    public static class Tarife
    {
        public const string View = "Tarife.View";
        public const string Manage = "Tarife.Manage";
    }

    public static class Tahakkuk
    {
        public const string Regenerate = "Tahakkuk.Regenerate";
    }

    public static class ManuelBorc
    {
        public const string View = "ManuelBorc.View";
        public const string Create = "ManuelBorc.Create";
        public const string Cancel = "ManuelBorc.Cancel";
    }

    public static class Rezervasyon
    {
        public const string View = "Rezervasyon.View";
        public const string Create = "Rezervasyon.Create";
        public const string Edit = "Rezervasyon.Edit";
        public const string Cancel = "Rezervasyon.Cancel";
        public const string TransferToTahakkuk = "Rezervasyon.TransferToTahakkuk";
    }

    public static class TasinmazCarpanPerm
    {
        public const string View = "TasinmazCarpan.View";
        public const string Manage = "TasinmazCarpan.Manage";
    }

    public static class RezervasyonTarifeKuralPerm
    {
        public const string Manage = "RezervasyonTarifeKural.Manage";
    }

    public static class Kullanici
    {
        public const string View = "Kullanici.View";
        public const string Create = "Kullanici.Create";
        public const string Edit = "Kullanici.Edit";
        public const string AssignPermission = "Kullanici.AssignPermission";
    }

    public static class Bildirim
    {
        public const string BorcHatirlatma = "Bildirim.BorcHatirlatma";
        public static IEnumerable<string> All() => new[] { BorcHatirlatma };
    }

    // Yonetici rolüne Admin tarafından atanabilecek tüm permission'lar
    public static readonly IReadOnlyList<string> AssignableToYonetici =
    [
        Tasinmaz.View, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.View, Birim.Create, Birim.Edit,
        Kiraci.View, Kiraci.Create, Kiraci.Edit,
        Sozlesme.View, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate, Sozlesme.OverrideRate,
        Odeme.View, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject,
        Odeme.ImportBankStatement, Odeme.MatchBankTransaction,
        ManuelBorc.View, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.View, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        TasinmazCarpanPerm.View, TasinmazCarpanPerm.Manage,
        Bildirim.BorcHatirlatma,
    ];

    // Goruntuleyici rolüne atanabilecek permission'lar (sadece View)
    public static readonly IReadOnlyList<string> AssignableToGoruntuleyici =
    [
        Tasinmaz.View,
        Birim.View,
        Kiraci.View,
        Sozlesme.View,
        Odeme.View,
        ManuelBorc.View,
        Rezervasyon.View,
    ];

    // Sistemdeki tüm permission'lar (policy kayıtları için)
    public static readonly IReadOnlyList<string> All =
    [
        Tasinmaz.View, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.View, Birim.Create, Birim.Edit,
        Kiraci.View, Kiraci.Create, Kiraci.Edit,
        Sozlesme.View, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate,
        Odeme.View, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject,
        Odeme.ImportBankStatement, Odeme.MatchBankTransaction,
        Kullanici.View, Kullanici.Create, Kullanici.Edit, Kullanici.AssignPermission,
        BorcTipi.Manage,
        Tarife.View, Tarife.Manage,
        Birim.ManageRate,
        Sozlesme.OverrideRate,
        Tahakkuk.Regenerate,
        Parametre.View, Parametre.Manage,
        TasinmazTipiPerm.View, TasinmazTipiPerm.Manage,
        BirimTuruPerm.View, BirimTuruPerm.Manage,
        KiraciKategoriPerm.View, KiraciKategoriPerm.Manage,
        SektorPerm.View, SektorPerm.Manage,
        ManuelBorc.View, ManuelBorc.Create, ManuelBorc.Cancel,
        Rezervasyon.View, Rezervasyon.Create, Rezervasyon.Edit, Rezervasyon.Cancel, Rezervasyon.TransferToTahakkuk,
        TasinmazCarpanPerm.View, TasinmazCarpanPerm.Manage,
        RezervasyonTarifeKuralPerm.Manage,
        Bildirim.BorcHatirlatma,
    ];
}
