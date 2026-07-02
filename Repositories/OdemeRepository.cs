using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class OdemeRepository : BaseRepository<TahakkukOdeme>, IOdemeRepository
{
    public OdemeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<OdemeListItemDto>> GetListAsync(int? tahakkukId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<TahakkukOdeme> query = _dbSet.AsNoTracking();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.TahakkukId == tahakkukId.Value);

        if (yetkiliTasinmazIds != null)
            query = query.Where(o => yetkiliTasinmazIds.Contains(o.Tahakkuk.Birim.TasinmazId));

        return await query
            .OrderByDescending(o => o.GirisTarihi)
            .Select(o => new OdemeListItemDto
            {
                Id = o.Id,
                TahakkukId = o.TahakkukId,
                KiraSozlesmesiId = o.KiraSozlesmesiId,
                OdemeTarihi = o.OdemeTarihi,
                Tutar = o.Tutar,
                PaymentChannel = o.PaymentChannel,
                PaymentSourceType = o.PaymentSourceType,
                Durum = o.Durum,
                GirisTarihi = o.GirisTarihi,
                Aciklama = o.Aciklama,
                KiraciGosterimAdi = o.Tahakkuk.Kiraci.Ad,
                TahakkukDonemBaslangic = o.Tahakkuk.DonemBaslangic,
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            })
            .ToListAsync();
    }

    public async Task<PagedResult<OdemeListItemDto>> GetPagedListAsync(TableQuery q, int? tahakkukId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<TahakkukOdeme> query = _dbSet.AsNoTracking();

        if (tahakkukId.HasValue)
            query = query.Where(o => o.TahakkukId == tahakkukId.Value);

        if (yetkiliTasinmazIds != null)
            query = query.Where(o => yetkiliTasinmazIds.Contains(o.Tahakkuk.Birim.TasinmazId));

        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(o =>
                EF.Functions.Like(o.Tahakkuk.Kiraci.Ad, $"%{s}%") ||
                (o.Aciklama != null && EF.Functions.Like(o.Aciklama, $"%{s}%")));
        }

        if (q.From.HasValue) query = query.Where(o => o.OdemeTarihi >= q.From.Value);
        if (q.To.HasValue) query = query.Where(o => o.OdemeTarihi <= q.To.Value);
        if (q.Min.HasValue) query = query.Where(o => o.Tutar >= q.Min.Value);
        if (q.Max.HasValue) query = query.Where(o => o.Tutar <= q.Max.Value);

        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            PaymentStatus? d = q.Durum switch
            {
                "onaybekliyor" => PaymentStatus.PendingApproval,
                "onaylandi" => PaymentStatus.Approved,
                "reddedildi" => PaymentStatus.Rejected,
                _ => null
            };
            if (d.HasValue) query = query.Where(o => o.Durum == d.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.GirisTarihi)
            .Skip(q.Skip).Take(q.Take)
            .Select(o => new OdemeListItemDto
            {
                Id = o.Id,
                TahakkukId = o.TahakkukId,
                KiraSozlesmesiId = o.KiraSozlesmesiId,
                OdemeTarihi = o.OdemeTarihi,
                Tutar = o.Tutar,
                PaymentChannel = o.PaymentChannel,
                PaymentSourceType = o.PaymentSourceType,
                Durum = o.Durum,
                GirisTarihi = o.GirisTarihi,
                Aciklama = o.Aciklama,
                KiraciGosterimAdi = o.Tahakkuk.Kiraci.Ad,
                TahakkukDonemBaslangic = o.Tahakkuk.DonemBaslangic,
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null
            })
            .ToListAsync();

        return new PagedResult<OdemeListItemDto>
        {
            Items = items,
            Total = total,
            Page = Math.Max(1, q.Page),
            Size = q.SafeSize
        };
    }

    public async Task<OdemeDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OdemeDetayDto
            {
                Id = o.Id,
                TahakkukId = o.TahakkukId,
                KiraSozlesmesiId = o.KiraSozlesmesiId,
                OdemeTarihi = o.OdemeTarihi,
                Tutar = o.Tutar,
                PaymentChannel = o.PaymentChannel,
                PaymentSourceType = o.PaymentSourceType,
                PosReferansNo = o.PosReferansNo,
                Aciklama = o.Aciklama,
                Durum = o.Durum,
                GirisTarihi = o.GirisTarihi,
                OnayTarihi = o.OnayTarihi,
                RedNedeni = o.RedNedeni,
                TasinmazId = o.Tahakkuk.KiraSozlesmesi != null && o.Tahakkuk.KiraSozlesmesi.Birim != null ? (int?)o.Tahakkuk.KiraSozlesmesi.Birim.TasinmazId : null,
                KiraciGosterimAdi = o.Tahakkuk.Kiraci.Ad,
                TahakkukDonemBaslangic = o.Tahakkuk.DonemBaslangic,
                GirenUserGosterimAdi = o.GirenUser != null ? (o.GirenUser.AdSoyad ?? o.GirenUser.Email) : null,
                OnaylayanUserGosterimAdi = o.OnaylayanUser != null ? o.OnaylayanUser.AdSoyad : null,
                BankaEslesmeleri = o.BankaEslesmeleri.Select(e => new OdemeBankaEslesmeDto
                {
                    Id = e.Id,
                    MatchType = e.MatchType,
                    BankaHareketiTutar = e.BankaHareketi.IslemTutari,
                    BankaHareketiTarih = e.BankaHareketi.IslemTarihi,
                    BankaHareketiAciklama = e.BankaHareketi.Aciklama ?? string.Empty
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }
}
