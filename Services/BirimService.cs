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
            var dummySozlesme = new KiraSozlesmesi
            {
                Id = dto.AktifSozlesmeId.Value,
                KiraciId = dto.AktifSozlesmeKiraciId ?? 0,
                BirimId = dto.Id,
                Birim = new Birim { Id = dto.Id, Yuzolcumu = dto.Yuzolcumu }
            };
            dto.AylikBedel = await _istatistikService.AylikBedelAsync(dummySozlesme);
        }

        return dto;
    }

    public async Task CreateAsync(Birim b)
    {
        await _repo.AddAsync(b);
        await _uow.SaveChangesAsync();
    }

    public async Task UpdateAsync(Birim b)
    {
        await _repo.UpdateAsync(b);
        await _uow.SaveChangesAsync();
    }
}
