using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateResolverService : IRateResolverService
{
    private readonly ApplicationDbContext _ctx;

    public RateResolverService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<RateSnapshot?> ResolveAsync(int? sozlesmeId, int? kiraciId, int birimId, int borcTipiId, DateTime donem)
    {
        if (sozlesmeId.HasValue)
        {
            var sozRate = await _ctx.SozlesmeRateler
                .FirstOrDefaultAsync(r => r.SozlesmeId == sozlesmeId.Value && r.BorcTipiId == borcTipiId);
            if (sozRate != null)
                return new RateSnapshot
                {
                    HesaplamaYontemi = sozRate.HesaplamaYontemi,
                    BirimDeger = sozRate.BirimDeger,
                    KdvOrani = sozRate.KdvOrani,
                    KaynakTipi = KalemKaynakTipi.SozlesmeTarifesi
                };
        }

        int? tasinmazId = null;
        int? kategoriId = null;

        if (sozlesmeId.HasValue)
        {
            var info = await _ctx.Sozlesmeler
                .Where(s => s.Id == sozlesmeId.Value)
                .Select(s => new { s.Birim.TasinmazId, s.Kiraci.KiraciKategoriId })
                .FirstOrDefaultAsync();
            if (info != null)
            {
                tasinmazId = info.TasinmazId;
                kategoriId = info.KiraciKategoriId;
            }
        }
        else if (kiraciId.HasValue)
        {
            var birim = await _ctx.Birimler.FindAsync(birimId);
            tasinmazId = birim?.TasinmazId;

            var kiraci = await _ctx.Kiraciler.FindAsync(kiraciId.Value);
            kategoriId = kiraci?.KiraciKategoriId;
        }

        if (kategoriId.HasValue)
        {
            var birimRate = await _ctx.BirimRateler
                .FirstOrDefaultAsync(r => r.BirimId == birimId
                    && r.KiraciKategoriId == kategoriId.Value
                    && r.BorcTipiId == borcTipiId);
            if (birimRate != null)
                return new RateSnapshot
                {
                    HesaplamaYontemi = birimRate.HesaplamaYontemi,
                    BirimDeger = birimRate.BirimDeger,
                    KdvOrani = birimRate.KdvOrani,
                    KaynakTipi = KalemKaynakTipi.BirimTarifesi
                };
        }

        if (tasinmazId.HasValue && kategoriId.HasValue)
        {
            var fiyatMatrisi = await _ctx.TasinmazKiraciKategoriFiyatlari
                .FirstOrDefaultAsync(f => f.TasinmazId == tasinmazId.Value
                    && f.KiraciKategoriId == kategoriId.Value
                    && f.BorcTipiId == borcTipiId
                    && f.Aktif);

            if (fiyatMatrisi != null)
                return new RateSnapshot
                {
                    HesaplamaYontemi = fiyatMatrisi.HesaplamaYontemi,
                    BirimDeger = fiyatMatrisi.BirimDeger,
                    KdvOrani = fiyatMatrisi.KdvOrani,
                    KaynakTipi = KalemKaynakTipi.TasinmazTarifesi
                };
        }

        if (!kategoriId.HasValue) return null;

        var tarife = await _ctx.Tarifeler
                         .Where(t => t.Aktif && t.Yil == donem.Year)
                         .FirstOrDefaultAsync()
                     ?? await _ctx.Tarifeler
                         .Where(t => t.Aktif)
                         .OrderByDescending(t => t.Yil)
                         .FirstOrDefaultAsync();
        if (tarife == null) return null;

        var kalem = await _ctx.TarifeKalemleri
            .FirstOrDefaultAsync(k => k.TarifeId == tarife.Id
                && k.KiraciKategoriId == kategoriId.Value
                && k.BorcTipiId == borcTipiId);
        if (kalem == null) return null;

        return new RateSnapshot
        {
            HesaplamaYontemi = kalem.HesaplamaYontemi,
            BirimDeger = kalem.BirimDeger,
            KdvOrani = kalem.KdvOrani,
            KaynakTipi = KalemKaynakTipi.GenelTarife
        };
    }
}
