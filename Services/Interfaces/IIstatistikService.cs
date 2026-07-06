using KiraTakip.Models;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IIstatistikService
{
    OccupancyStatus GetBirimDurumu(Unit birim);
    Lease? GetAktifSozlesme(Unit birim);
    bool Aktif(Lease s);
    Task<decimal> AylikBedelAsync(Lease s);
    Task<decimal> YillikBedelAsync(Lease s);
    int KalanGun(Lease s);
    double SureYuzdesi(Lease s);
    decimal TufeArtisliBedel(decimal mevcutBedel, decimal tufeOrani);
    decimal KdvTutari(decimal kdvHaricBedel, decimal kdvOrani);
    decimal KdvDahilTutar(decimal kdvHaricBedel, decimal kdvOrani);
    KiraHesaplamaSonucu HesaplaKiraArtisi(decimal mevcutKiraBedeli, decimal? tufeOrani, bool kdvUygulanacakMi, decimal? kdvOrani);
}
