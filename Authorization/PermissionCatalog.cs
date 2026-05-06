namespace KiraTakip.Authorization;

public static class PermissionCatalog
{
    public static class Tasinmaz
    {
        public const string View   = "Tasinmaz.View";
        public const string Create = "Tasinmaz.Create";
        public const string Edit   = "Tasinmaz.Edit";
    }

    public static class Birim
    {
        public const string View       = "Birim.View";
        public const string Create     = "Birim.Create";
        public const string Edit       = "Birim.Edit";
        public const string ManageRate = "Birim.ManageRate";
    }

    public static class Kiraci
    {
        public const string View   = "Kiraci.View";
        public const string Create = "Kiraci.Create";
        public const string Edit   = "Kiraci.Edit";
    }

    public static class Sozlesme
    {
        public const string View         = "Sozlesme.View";
        public const string Create       = "Sozlesme.Create";
        public const string Edit         = "Sozlesme.Edit";
        public const string Extend       = "Sozlesme.Extend";
        public const string Terminate    = "Sozlesme.Terminate";
        public const string OverrideRate = "Sozlesme.OverrideRate";
    }

    public static class Odeme
    {
        public const string View                = "Odeme.View";
        public const string Create              = "Odeme.Create";
        public const string UploadDekont        = "Odeme.UploadDekont";
        public const string Approve             = "Odeme.Approve";
        public const string Reject              = "Odeme.Reject";
        public const string ImportBankStatement = "Odeme.ImportBankStatement";
        public const string MatchBankTransaction = "Odeme.MatchBankTransaction";
    }

    public static class BorcTipi
    {
        public const string Manage = "BorcTipi.Manage";
    }

    public static class Tarife
    {
        public const string View   = "Tarife.View";
        public const string Manage = "Tarife.Manage";
    }

    public static class Tahakkuk
    {
        public const string Regenerate = "Tahakkuk.Regenerate";
    }

    public static class Kullanici
    {
        public const string View             = "Kullanici.View";
        public const string Create           = "Kullanici.Create";
        public const string Edit             = "Kullanici.Edit";
        public const string AssignPermission = "Kullanici.AssignPermission";
    }

    // Yonetici rolüne Admin tarafından atanabilecek tüm permission'lar
    public static readonly IReadOnlyList<string> AssignableToYonetici =
    [
        Tasinmaz.View, Tasinmaz.Create, Tasinmaz.Edit,
        Birim.View, Birim.Create, Birim.Edit,
        Kiraci.View, Kiraci.Create, Kiraci.Edit,
        Sozlesme.View, Sozlesme.Create, Sozlesme.Edit, Sozlesme.Extend, Sozlesme.Terminate,
        Odeme.View, Odeme.Create, Odeme.UploadDekont, Odeme.Approve, Odeme.Reject,
        Odeme.ImportBankStatement, Odeme.MatchBankTransaction,
    ];

    // Goruntuleyici rolüne atanabilecek permission'lar (sadece View)
    public static readonly IReadOnlyList<string> AssignableToGoruntuleyici =
    [
        Tasinmaz.View,
        Birim.View,
        Kiraci.View,
        Sozlesme.View,
        Odeme.View,
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
    ];
}
