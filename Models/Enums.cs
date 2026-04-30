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
