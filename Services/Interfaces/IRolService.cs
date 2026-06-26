using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IRolService
{
    Task<List<Rol>> GetInternalRollerAsync();
    Task<Rol?> GetByIdAsync(int id);
    Task<Rol> CreateAsync(string ad, string? aciklama, string createdBy);
    Task UpdateAsync(int id, string ad, string? aciklama, string updatedBy);
    Task SilAsync(int id, string deletedBy);
    Task<List<string>> GetRolPermissionsAsync(int rolId);
    Task SetRolPermissionsAsync(int rolId, IEnumerable<string> permissions, string updatedBy);
    Task<List<Rol>> GetKiraciRollerAsync(int kiraciId);
    Task EnsureGlobalKiraciRolleriAsync(string createdBy);
}
