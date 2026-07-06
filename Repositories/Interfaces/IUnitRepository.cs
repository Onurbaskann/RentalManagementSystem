using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IUnitRepository : IBaseRepository<Unit>
{
    Task<List<BirimListItemDto>> GetByPropertyIdAsync(int propertyId);
    Task<BirimDetayDto?> GetDetayAsync(int id);
    Task<List<BirimListItemDto>> GetRezervasyonBirimleriAsync();
    Task<int?> GetPropertyIdAsync(int unitId);
}
