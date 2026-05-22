using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class KiraciRepository : BaseRepository<Kiraci>, IKiraciRepository
{
    public KiraciRepository(ApplicationDbContext ctx) : base(ctx)
    {
    }

    public async Task<List<KiraciListItemDto>> GetListAsync(List<int>? yetkiliTasinmazIds)
    {
        IQueryable<Kiraci> q = _dbSet.AsNoTracking();

        if (yetkiliTasinmazIds != null)
        {
            var yetkiliKiraciIds = _ctx.Sozlesmeler
                .Where(s => yetkiliTasinmazIds.Contains(s.Birim.TasinmazId))
                .Select(s => s.KiraciId)
                .Distinct();

            q = q.Where(k => yetkiliKiraciIds.Contains(k.Id));
        }

        return await q
            .OrderBy(k => k.Ad)
            .Select(k => new KiraciListItemDto
            {
                Id = k.Id,
                KiraciNo = k.KiraciNo,
                GosterimAdi = k.KiraciTuru == KiraciTuru.Gercek ? (k.Ad + " " + k.Soyad).Trim() : k.Ad,
                KiraciTuru = k.KiraciTuru,
                VergiNo = k.VergiNo,
                TcKimlikNo = k.TcKimlikNo,
                KiraciKategoriAd = k.KiraciKategori != null ? k.KiraciKategori.Ad : null,
                Telefon = k.Telefon,
                Email = k.Email,
                KayitTarihi = k.KayitTarihi
            })
            .ToListAsync();
    }

    public async Task<KiraciDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(k => k.Id == id)
            .Select(k => new KiraciDetayDto
            {
                Id = k.Id,
                KiraciKategoriId = k.KiraciKategoriId,
                KiraciKategoriAd = k.KiraciKategori != null ? k.KiraciKategori.Ad : null,
                SektorId = k.SektorId,
                SektorAd = k.Sektor != null ? k.Sektor.Ad : null,
                KiraciNo = k.KiraciNo,
                KiraciTuru = k.KiraciTuru,
                Ad = k.Ad,
                Soyad = k.Soyad,
                TcKimlikNo = k.TcKimlikNo,
                PasaportNo = k.PasaportNo,
                Unvan = k.Unvan,
                AnneAdi = k.AnneAdi,
                BabaAdi = k.BabaAdi,
                DogumTarihi = k.DogumTarihi,
                DogumYeri = k.DogumYeri,
                TicaretSicilNo = k.TicaretSicilNo,
                VergiNo = k.VergiNo,
                VergiDairesi = k.VergiDairesi,
                MersisNo = k.MersisNo,
                Telefon = k.Telefon,
                Email = k.Email,
                Adres = k.Adres,
                KvkkOnayi = k.KvkkOnayi,
                KayitTarihi = k.KayitTarihi
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<string>> GetExistingKiraciNosAsync()
    {
        return await _dbSet.AsNoTracking()
            .Select(k => k.KiraciNo)
            .ToListAsync();
    }

    public async Task<int?> GetKategoriIdAsync(int kiraciId)
        => await _dbSet.AsNoTracking()
            .Where(k => k.Id == kiraciId)
            .Select(k => k.KiraciKategoriId)
            .FirstOrDefaultAsync();
}
