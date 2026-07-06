using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ILeaseService
{
    Task<List<SozlesmeListItemDto>> GetAllAsync(string? filtre = null, IReadOnlyList<int>? propertyIds = null);
    Task<SozlesmeDetayDto?> GetByIdAsync(int id);
    Task<Lease> CreateAsync(Lease s, decimal? aylikBedel = null);
    Task UzatAsync(int id, DateTime yeniBitis, decimal eskiBedel, decimal yeniBedel, bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama);
    Task FeshetAsync(int id, DateTime fesihTarihi, string fesihNedeni, string? aciklama);
    Task VadeGuncelleAsync(int id, DueDateRuleType tip, int gun, string? aciklama);
    Task<List<SozlesmeListItemDto>> GetByTenantIdAsync(int tenantId);
    Task<List<SozlesmeListItemDto>> GetByUnitIdAsync(int unitId);
    Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> leaseIds);
}
