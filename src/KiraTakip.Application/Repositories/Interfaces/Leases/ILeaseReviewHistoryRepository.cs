using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Repositories.Interfaces.Leases;

public interface ILeaseReviewHistoryRepository : IRepositoryBase<LeaseReviewHistory>
{
    Task<List<LeaseReviewHistoryDto>> GetByLeaseIdAsync(int leaseId);
    Task<LeaseReviewHistoryDto?> GetLatestRevisionAsync(int leaseId);
}
