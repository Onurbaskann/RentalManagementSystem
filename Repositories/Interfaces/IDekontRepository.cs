using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IDekontRepository : IBaseRepository<Dekont>
{
    Task<List<DekontListItemDto>> GetByOdemeIdAsync(int odemeId);
    Task<DekontDetayDto?> GetDetayAsync(int id);
    Task<(int? KiraSozlesmesiId, int TahakkukId)?> GetOdemeInfoAsync(int odemeId);
}
