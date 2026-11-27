using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Leases;

public interface ILeaseGroupRepository : IRepositoryBase<LeaseGroup>
{
    /// <summary>İzlenen grup; silinmemiş sözleşmeleri (alan satırlarıyla) ve alanı yüklü. Kapsam dışıysa null.</summary>
    Task<LeaseGroup?> GetForMutationAsync(int groupId, SpaceAccessScope? scope);

    Task<LeaseGroupDetailDto?> GetDetailAsync(int groupId, SpaceAccessScope? scope);

    Task<LeaseGroupLinkDto?> GetLinkByLeaseIdAsync(int leaseId);

    /// <summary>Gruptaki (silinmemiş) tüm sözleşmelerin id'leri; durumdan bağımsız.</summary>
    Task<List<int>> GetLeaseIdsAsync(int groupId);
}
