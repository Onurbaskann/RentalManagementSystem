using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Leases;

public interface ILeaseGroupRepository : IRepositoryBase<LeaseGroup>
{
    /// <summary>İzlenen grup; silinmemiş sözleşmeleri, birimi ve taşınmazı yüklü. Kapsam dışıysa null.</summary>
    Task<LeaseGroup?> GetForMutationAsync(
        int groupId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);

    Task<LeaseGroupDetailDto?> GetDetailAsync(
        int groupId,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null);

    Task<LeaseGroupLinkDto?> GetLinkByLeaseIdAsync(int leaseId);
}
