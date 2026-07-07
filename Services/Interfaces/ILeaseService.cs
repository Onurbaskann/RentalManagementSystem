using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ILeaseService
{
    Task<List<LeaseListItemDto>> GetAllAsync(string? filter = null, IReadOnlyList<int>? propertyIds = null);
    Task<LeaseDetailDto?> GetByIdAsync(int id);
    Task<Lease> CreateAsync(Lease lease, decimal? monthlyAmount = null);
    Task UzatAsync(int id, DateTime newEndDate, decimal oldAmount, decimal newAmount, bool isKdvApplied, decimal kdvRate, decimal? inflationRate, string? description);
    Task FeshetAsync(int id, DateTime terminationDate, string terminationReason, string? description);
    Task VadeGuncelleAsync(int id, DueDateRuleType ruleType, int dueDay, string? description);
    Task<List<LeaseListItemDto>> GetByTenantIdAsync(int tenantId);
    Task<List<LeaseListItemDto>> GetByUnitIdAsync(int unitId);
    Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> leaseIds);
}
