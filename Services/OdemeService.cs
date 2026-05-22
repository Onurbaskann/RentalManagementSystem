using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class OdemeService : IOdemeService
{
    private readonly IOdemeRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ITahakkukService _tahakkukService;
    private readonly IUserTasinmazYetkiService _yetkiService;

    public OdemeService(
        IOdemeRepository repo,
        IUnitOfWork uow,
        ITahakkukService tahakkukService,
        IUserTasinmazYetkiService yetkiService)
    {
        _repo = repo;
        _uow = uow;
        _tahakkukService = tahakkukService;
        _yetkiService = yetkiService;
    }

    public async Task<List<OdemeListItemDto>> GetAllAsync(int? tahakkukId = null, string? userId = null)
    {
        List<int>? yetkiliIds = null;
        if (userId != null)
        {
            yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
        }

        return await _repo.GetListAsync(tahakkukId, yetkiliIds);
    }

    public async Task<PagedResult<OdemeListItemDto>> GetPagedAsync(TableQuery q, int? tahakkukId = null, string? userId = null)
    {
        List<int>? yetkiliIds = null;
        if (userId != null)
        {
            yetkiliIds = await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
        }

        return await _repo.GetPagedListAsync(q, tahakkukId, yetkiliIds);
    }

    public async Task<OdemeDetayDto?> GetByIdAsync(int id)
    {
        return await _repo.GetDetayAsync(id);
    }

    public async Task<KiraOdeme> EkleAsync(KiraOdeme odeme)
    {
        odeme.GirisTarihi = DateTime.Now;
        odeme.Durum = OdemeDurumu.OnayBekliyor;
        await _repo.AddAsync(odeme);
        await _uow.SaveChangesAsync();
        return odeme;
    }

    public async Task<bool> OnaylaAsync(int id, string onaylayanUserId)
    {
        var odeme = await _repo.GetByIdAsync(id);
        if (odeme == null || odeme.Durum != OdemeDurumu.OnayBekliyor) return false;

        odeme.Durum = OdemeDurumu.Onaylandi;
        odeme.OnaylayanUserId = onaylayanUserId;
        odeme.OnayTarihi = DateTime.Now;
        await _repo.UpdateAsync(odeme);
        await _uow.SaveChangesAsync();

        await _tahakkukService.OdenenTutarGuncelleAsync(odeme.KiraTahakkukId);
        return true;
    }

    public async Task<bool> ReddetAsync(int id, string neden)
    {
        var odeme = await _repo.GetByIdAsync(id);
        if (odeme == null || odeme.Durum != OdemeDurumu.OnayBekliyor) return false;

        odeme.Durum = OdemeDurumu.Reddedildi;
        odeme.RedNedeni = neden;
        await _repo.UpdateAsync(odeme);
        await _uow.SaveChangesAsync();

        await _tahakkukService.OdenenTutarGuncelleAsync(odeme.KiraTahakkukId);
        return true;
    }
}
