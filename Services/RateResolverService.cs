using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateResolverService : IRateResolverService
{
    private readonly ApplicationDbContext _ctx;

    public RateResolverService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<RateSnapshot?> ResolveAsync(int sozlesmeId, int birimId, int borcTipiId, DateTime donem)
    {
        var sozRate = await _ctx.SozlesmeRateler
            .FirstOrDefaultAsync(r => r.SozlesmeId == sozlesmeId && r.BorcTipiId == borcTipiId);
        if (sozRate != null)
            return new RateSnapshot
            {
                HesaplamaYontemi = sozRate.HesaplamaYontemi,
                BirimDeger = sozRate.BirimDeger,
                KdvOrani = sozRate.KdvOrani,
                KaynakTipi = KaynakTipi.Sozlesme
            };

        var birimRate = await _ctx.BirimRateler
            .FirstOrDefaultAsync(r => r.BirimId == birimId && r.BorcTipiId == borcTipiId);
        if (birimRate != null)
            return new RateSnapshot
            {
                HesaplamaYontemi = birimRate.HesaplamaYontemi,
                BirimDeger = birimRate.BirimDeger,
                KdvOrani = birimRate.KdvOrani,
                KaynakTipi = KaynakTipi.Birim
            };

        var tarife = await _ctx.Tarifeler
                         .Where(t => t.Aktif && t.Yil == donem.Year)
                         .FirstOrDefaultAsync()
                     ?? await _ctx.Tarifeler
                         .Where(t => t.Aktif)
                         .OrderByDescending(t => t.Yil)
                         .FirstOrDefaultAsync();
        if (tarife == null) return null;

        var kalem = await _ctx.TarifeKalemleri
            .FirstOrDefaultAsync(k => k.TarifeId == tarife.Id && k.BorcTipiId == borcTipiId);
        if (kalem == null) return null;

        return new RateSnapshot
        {
            HesaplamaYontemi = kalem.HesaplamaYontemi,
            BirimDeger = kalem.BirimDeger,
            KdvOrani = kalem.KdvOrani,
            KaynakTipi = KaynakTipi.Tarife
        };
    }
}
