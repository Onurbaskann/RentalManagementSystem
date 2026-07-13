using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class UnitService : IUnitService
{
    private readonly IUnitRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStatisticsService _istatistikService;

    public UnitService(IUnitRepository repo, IUnitOfWork uow, IStatisticsService statisticsService)
    {
        _repo = repo;
        _uow = uow;
        _istatistikService = statisticsService;
    }

    public async Task<List<UnitListItemDto>> GetByPropertyIdAsync(int propertyId)
    {
        return await _repo.GetByPropertyIdAsync(propertyId);
    }

    public async Task<UnitDetailDto?> GetByIdAsync(int id)
    {
        var dto = await _repo.GetDetayAsync(id);
        if (dto == null) return null;

        if (dto.ActiveLeaseId.HasValue)
        {
            var lease = new Lease
            {
                Id = dto.ActiveLeaseId.Value,
                TenantId = dto.ActiveLeaseTenantId ?? 0,
                UnitId = dto.Id,
                Unit = new Unit { Id = dto.Id, Area = dto.Area }
            };
            dto.MonthlyRent = await _istatistikService.AylikBedelAsync(lease);
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
