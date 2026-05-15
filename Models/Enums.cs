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
    KdvGuncelleme = 5,
    TahakkukYenidenUretim = 6
}


public enum KiralamaSekli
{
    TekParca = 1,
    BirimBazli = 2
}

public enum KiraDurumu
{
    Bos = 1,
    Kirali = 2,
    SuresiDolmakUzere = 3
}


public enum BirimTipi
{
    Komple = 1,
    Birim = 2
}

public enum TahakkukDurumu
{
    Bekleniyor = 1,
    KismenOdendi = 2,
    TamOdendi = 3,
    Gecikti = 4,
    IptalEdildi = 5
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

public enum HesaplamaYontemi
{
    Sabit = 1,
    M2    = 2
}

public enum KalemKaynakTipi
{
    TanimsizTarife              = 0,
    SozlesmeTarifesi            = 1,
    BirimTarifesi               = 2,
    GenelTarife                 = 3,
    TasinmazTarifesi            = 4,
    ManuelGiris                 = 5,
    RezervasyonKurali           = 6
}

public enum TahakkukKaynakTipi
{
    Sozlesme    = 1,
    Manuel      = 2,
    Rezervasyon = 3
}

public enum RezervasyonDurumu
{
    Planlandi          = 1,
    Tamamlandi         = 2,
    IptalEdildi        = 3,
    TahakkukaAktarildi = 4
}

public enum BorcTipiDavranisi
{
    AylikSabit = 1,
    IlkAyTekSeferlik = 2,
    KullaniciManuel = 3,
    RezervasyonOzel = 4
}
