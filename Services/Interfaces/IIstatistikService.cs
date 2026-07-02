using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IIstatistikService
{
    OccupancyStatus GetBirimDurumu(Birim birim);
    Sozlesme? GetAktifSozlesme(Birim birim);
    bool Aktif(Sozlesme s);
    Task<decimal> AylikBedelAsync(Sozlesme s);
    Task<decimal> YillikBedelAsync(Sozlesme s);
    int KalanGun(Sozlesme s);
    double SureYuzdesi(Sozlesme s);
    decimal TufeArtisliBedel(decimal mevcutBedel, decimal tufeOrani);
    decimal KdvTutari(decimal kdvHaricBedel, decimal kdvOrani);
    decimal KdvDahilTutar(decimal kdvHaricBedel, decimal kdvOrani);
    KiraHesaplamaSonucu HesaplaKiraArtisi(decimal mevcutKiraBedeli, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani);
}
