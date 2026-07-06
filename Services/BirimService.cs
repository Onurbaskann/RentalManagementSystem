using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class BirimService : IBirimService
{
    private readonly IBirimRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IIstatistikService _istatistikService;

    public BirimService(IBirimRepository repo, IUnitOfWork uow, IIstatistikService istatistikService)
    {
        _repo = repo;
        _uow = uow;
        _istatistikService = istatistikService;
    }

    public async Task<List<BirimListItemDto>> GetByTasinmazIdAsync(int tasinmazId)
    {
        return await _repo.GetByTasinmazIdAsync(tasinmazId);
    }

    public async Task<BirimDetayDto?> GetByIdAsync(int id)
    {
        var dto = await _repo.GetDetayAsync(id);
        if (dto == null) return null;

        if (dto.AktifSozlesmeId.HasValue)
        {
            var dummySozlesme = new Lease
            {
                Id = dto.AktifSozlesmeId.Value,
                TenantId = dto.AktifSozlesmeKiraciId ?? 0,
                UnitId = dto.Id,
                Unit = new Unit { Id = dto.Id, Area = dto.Yuzolcumu }
            };
            dto.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }

        return dto;
    }

    public async Task CreateAsync(Unit b)
    {
        await _repo.AddAsync(b);
        await _uow.SaveChangesAsync();
    }

    public async Task UpdateAsync(Unit b)
    {
        await _repo.UpdateAsync(b);
        await _uow.SaveChangesAsync();
    }
}
