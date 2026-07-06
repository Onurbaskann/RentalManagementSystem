using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class SozlesmeService : ISozlesmeService, ITransactionalService
{
    private readonly ISozlesmeRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IIstatistikService _istatistikService;

    public SozlesmeService(
        ISozlesmeRepository repo,
        IUnitOfWork uow,
        IIstatistikService istatistikService)
    {
        _repo = repo;
        _uow = uow;
        _istatistikService = istatistikService;
    }

    public async Task<List<SozlesmeListItemDto>> GetAllAsync(string? filtre = null, IReadOnlyList<int>? tasinmazIds = null)
    {
        var yetkiliIds = tasinmazIds?.ToList();
        var list = await _repo.GetListAsync(filtre, yetkiliIds);
        foreach (var s in list)
        {
            var dummySozlesme = new Lease
            {
                Id = s.Id,
                TenantId = s.KiraciId,
                UnitId = s.BirimId,
                Unit = new Unit { Id = s.BirimId, Area = s.BirimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }
        return list;
    }

    public async Task<SozlesmeDetayDto?> GetByIdAsync(int id)
    {
        return await _repo.GetDetayAsync(id);
    }

    public async Task<Lease> CreateAsync(Lease s, decimal? aylikBedel = null)
    {
        s.ActivityLog.Add(new SozlesmeIslemGecmisi
        {
            IslemTipi = LeaseActivityType.Creation,
            TransactionDate = DateTime.Now,
            Aciklama = "Sözleşme oluşturuldu.",
            YeniKiraBedeli = aylikBedel
        });

        await _repo.AddAsync(s);
        await _uow.SaveChangesAsync();
        return s;
    }

    public async Task UzatAsync(int id, DateTime yeniBitis, decimal eskiBedel, decimal yeniBedel,
        bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama)
    {
        var s = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.ActivityLog))
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        var eskiBitis = s.EndDate;

        s.EndDate = yeniBitis;
        s.IsKdvApplied = kdvUygulanacakMi;

        decimal? kdvTutari = kdvUygulanacakMi ? yeniBedel * kdvOrani / 100 : null;
        decimal? kdvDahil = kdvUygulanacakMi ? yeniBedel + kdvTutari : null;

        s.ActivityLog.Add(new SozlesmeIslemGecmisi
        {
            LeaseId = id,
            IslemTipi = LeaseActivityType.Extension,
            TransactionDate = DateTime.Now,
            Aciklama = aciklama ?? "Sözleşme süresi uzatıldı.",
            EskiBitisTarihi = eskiBitis,
            YeniBitisTarihi = yeniBitis,
            EskiKiraBedeli = eskiBedel,
            YeniKiraBedeli = yeniBedel,
            TufeOrani = tufeOrani,
            KdvUygulandiMi = kdvUygulanacakMi,
            KdvRate = kdvUygulanacakMi ? kdvOrani : null,
            KdvTutari = kdvTutari,
            KdvDahilTutar = kdvDahil
        });

        await _uow.SaveChangesAsync();
    }

    public async Task FeshetAsync(int id, DateTime fesihTarihi, string fesihNedeni, string? aciklama)
    {
        var s = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.ActivityLog))
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        s.Status = LeaseStatus.Terminated;
        s.TerminationDate = fesihTarihi;
        s.TerminationReason = fesihNedeni;

        s.ActivityLog.Add(new SozlesmeIslemGecmisi
        {
            LeaseId = id,
            IslemTipi = LeaseActivityType.Termination,
            TransactionDate = DateTime.Now,
            Aciklama = aciklama ?? fesihNedeni
        });

        await _uow.SaveChangesAsync();
    }

    public async Task VadeGuncelleAsync(int id, DueDateRuleType tip, int gun, string? aciklama)
    {
        if (gun < 1 || gun > 31)
            throw new ArgumentOutOfRangeException(nameof(gun), "Vade günü 1-31 arasında olmalıdır.");

        var s = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.ActivityLog))
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        var eskiTip = s.DueDateRuleType;
        var eskiGun = s.DueDay;

        if (eskiTip == tip && eskiGun == gun) return;

        s.DueDateRuleType = tip;
        s.DueDay = gun;

        s.ActivityLog.Add(new SozlesmeIslemGecmisi
        {
            LeaseId = id,
            IslemTipi = LeaseActivityType.ChargeRegeneration,
            TransactionDate = DateTime.Now,
            Aciklama = aciklama ?? $"Vade kuralı güncellendi: {eskiTip}({eskiGun}) → {tip}({gun})"
        });

        await _uow.SaveChangesAsync();
    }

    public async Task<List<SozlesmeListItemDto>> GetByKiraciIdAsync(int kiraciId)
    {
        var list = await _repo.GetByKiraciIdAsync(kiraciId);
        foreach (var s in list)
        {
            var dummySozlesme = new Lease
            {
                Id = s.Id,
                TenantId = s.KiraciId,
                UnitId = s.BirimId,
                Unit = new Unit { Id = s.BirimId, Area = s.BirimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }
        return list;
    }

    public async Task<List<SozlesmeListItemDto>> GetByBirimIdAsync(int birimId)
    {
        var list = await _repo.GetByBirimIdAsync(birimId);
        foreach (var s in list)
        {
            var dummySozlesme = new Lease
            {
                Id = s.Id,
                TenantId = s.KiraciId,
                UnitId = s.BirimId,
                Unit = new Unit { Id = s.BirimId, Area = s.BirimYuzolcumu }
            };
            s.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }
        return list;
    }

    public async Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> sozlesmeIds)
    {
        return await _repo.GetDepozitoTutarlariAsync(sozlesmeIds);
    }
}
