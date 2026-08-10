using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Repositories.Interfaces;

public interface ILeaseReviewHistoryRepository
{
    Task AddAsync(LeaseReviewHistory history);
    Task<List<LeaseReviewHistoryDto>> GetByLeaseIdAsync(int leaseId);
    Task<LeaseReviewHistoryDto?> GetLatestRevisionAsync(int leaseId);
}
