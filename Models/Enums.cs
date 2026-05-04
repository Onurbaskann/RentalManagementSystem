namespace KiraTakip.Models;

public enum KiraciTuru
{
    Gercek = 1,
    Tuzel = 2
}

public enum SozlesmeDurumu
{
    Aktif = 1,
    SonaErdi = 2,
    Feshedildi = 3
}

public enum SozlesmeIslemTipi
{
    Olusturma = 1,
    SureUzatma = 2,
    Fesih = 3,
    TufeArtis = 4,
    KdvGuncelleme = 5
}

public enum TasinmazTipi
{
    Bina = 1,
    Arazi = 2,
    Tarla = 3,
    Depo = 4,
    Diger = 5
}

public enum KiralamaSekli
{
    TekParca = 1,
    OfisBazli = 2
}

public enum KiraDurumu
{
    Bos = 1,
    Kirali = 2,
    SuresiDolmakUzere = 3
}

public enum KiraPeriyodu
{
    Aylik = 1,
    Yillik = 2
}

public enum BirimTipi
{
    Komple = 1,
    Ofis = 2
}

public enum TahakkukDurumu
{
    Bekleniyor = 1,
    KismenOdendi = 2,
    TamOdendi = 3,
    Gecikti = 4
}

public enum OdemeDurumu
{
    OnayBekliyor = 1,
    Onaylandi = 2,
    Reddedildi = 3
}

public enum OdemeKanali
{
    Havale = 1,
    EFT = 2,
    Nakit = 3,
    Diger = 4
}

public enum BankaEslesmeDurumu
{
    Eslestirilmedi = 1,
    Eslesti = 2,
    ManuelEslesti = 3
}

public enum EslesmeTipi
{
    Otomatik = 1,
    Manuel = 2
}
