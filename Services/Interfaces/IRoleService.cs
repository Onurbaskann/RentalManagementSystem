using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IRoleService
{
    Task<List<Role>> GetInternalRollerAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<Role> CreateAsync(string ad, string? aciklama, string createdBy);
    Task UpdateAsync(int id, string ad, string? aciklama, string updatedBy);
    Task SilAsync(int id, string deletedBy);
    Task<List<string>> GetRolPermissionsAsync(int rolId);
    Task SetRolPermissionsAsync(int rolId, IEnumerable<string> permissions, string updatedBy);
    Task<List<Role>> GetKiraciRollerAsync(int tenantId);
    Task EnsureGlobalKiraciRolleriAsync(string createdBy);
}
