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
            var sozRate = await _ctx.SozlesmeTarifeler
                .FirstOrDefaultAsync(r => r.KiraSozlesmesiId == sozlesmeId.Value && r.BorcTipiId == borcTipiId);
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
            var birimRate = await _ctx.BirimTarifeler
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
            var fiyatMatrisi = await _ctx.TasinmazTarifeler
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

        // Exact year first, then fall back to most recent active year
        var kalem = await _ctx.GenelTarifeler
            .Where(k => k.Aktif && k.KiraciKategoriId == kategoriId.Value && k.BorcTipiId == borcTipiId)
            .OrderByDescending(k => k.Yil == donem.Year ? 1 : 0)
            .ThenByDescending(k => k.Yil)
            .FirstOrDefaultAsync();
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
