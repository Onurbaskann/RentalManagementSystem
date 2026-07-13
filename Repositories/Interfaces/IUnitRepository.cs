using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IUnitRepository : IBaseRepository<Unit>
{
    Task<List<UnitListItemDto>> GetByPropertyIdAsync(int propertyId);
    Task<UnitDetailDto?> GetDetayAsync(int id);
    Task<List<UnitListItemDto>> GetRezervasyonBirimleriAsync();
    Task<int?> GetPropertyIdAsync(int unitId);
}
