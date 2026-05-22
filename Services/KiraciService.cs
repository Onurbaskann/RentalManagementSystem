using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class KiraciService : IKiraciService
{
    private readonly IKiraciRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IUserTasinmazYetkiService _yetkiService;

    public KiraciService(IKiraciRepository repo, IUnitOfWork uow, IUserTasinmazYetkiService yetkiService)
    {
        _repo = repo;
        _uow = uow;
        _yetkiService = yetkiService;
    }

    public async Task<List<KiraciListItemDto>> GetAllAsync(string? userId = null)
    {
        List<int>? yetkiliIds = null;
        if (userId != null)
        {
            yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
        }
        return await _repo.GetListAsync(yetkiliIds);
    }

    public async Task<KiraciDetayDto?> GetDetayAsync(int id)
    {
        return await _repo.GetDetayAsync(id);
    }

    public async Task<Kiraci> CreateAsync(Kiraci k)
    {
        if (string.IsNullOrWhiteSpace(k.KiraciNo))
            k.KiraciNo = await GenerateKiraciNoAsync();
        k.KayitTarihi = DateTime.Now;
        await _repo.AddAsync(k);
        await _uow.SaveChangesAsync();
        return k;
    }

    public async Task UpdateAsync(Kiraci k)
    {
        var dbKiraci = await _repo.GetByIdAsync(k.Id);
        if (dbKiraci == null) return;

        dbKiraci.KiraciKategoriId = k.KiraciKategoriId;
        dbKiraci.SektorId = k.SektorId;
        dbKiraci.KiraciTuru = k.KiraciTuru;
        dbKiraci.Ad = k.Ad;
        dbKiraci.Soyad = k.Soyad;
        dbKiraci.TcKimlikNo = k.TcKimlikNo;
        dbKiraci.PasaportNo = k.PasaportNo;
        dbKiraci.Unvan = k.Unvan;
        dbKiraci.AnneAdi = k.AnneAdi;
        dbKiraci.BabaAdi = k.BabaAdi;
        dbKiraci.DogumTarihi = k.DogumTarihi;
        dbKiraci.DogumYeri = k.DogumYeri;
        dbKiraci.TicaretSicilNo = k.TicaretSicilNo;
        dbKiraci.VergiNo = k.VergiNo;
        dbKiraci.VergiDairesi = k.VergiDairesi;
        dbKiraci.MersisNo = k.MersisNo;
        dbKiraci.Telefon = k.Telefon;
        dbKiraci.Email = k.Email;
        dbKiraci.Adres = k.Adres;
        dbKiraci.KvkkOnayi = k.KvkkOnayi;
        dbKiraci.IsActive = k.IsActive;

        await _repo.UpdateAsync(dbKiraci); // No-op marker
        await _uow.SaveChangesAsync();
    }

    public async Task<string> GenerateKiraciNoAsync()
    {
        var existing = await _repo.GetExistingKiraciNosAsync();
        var usedSet = existing.ToHashSet();
        for (int i = 1; i <= 999999; i++)
        {
            var no = $"KRC-{i:D6}";
            if (!usedSet.Contains(no)) return no;
        }
        throw new InvalidOperationException("KiraciNo üretilemedi.");
    }

    public async Task<bool> KiraciNoExistsAsync(string kiraciNo, int? excludeId = null)
    {
        return await _repo.AnyAsync(k =>
            k.KiraciNo == kiraciNo && (excludeId == null || k.Id != excludeId));
    }
}
