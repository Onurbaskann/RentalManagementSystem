using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class UnitService(
    IUnitRepository unitRepository,
    IUnitOfWork uow,
    IStatisticsService statisticsService) : IUnitService
{
    public async Task<List<UnitListItemDto>> GetByPropertyIdAsync(int propertyId)
    {
        return await unitRepository.GetByPropertyIdAsync(propertyId);
    }

    public async Task<UnitDetailDto?> GetByIdAsync(int id)
    {
        var dto = await unitRepository.GetDetayAsync(id);
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
            dto.MonthlyRent = await statisticsService.GetMonthlyAmountAsync(lease);
        }

        return dto;
    }

    public async Task CreateAsync(Unit b)
    {
        await unitRepository.AddAsync(b);
        await uow.SaveChangesAsync();
    }

    public async Task UpdateAsync(Unit b)
    {
        await unitRepository.UpdateAsync(b);
        await uow.SaveChangesAsync();
    }

    public async Task<List<UnitListItemDto>> GetReservableUnitsAsync()
    {
        return await unitRepository.GetReservableUnitsAsync();
    }
}
