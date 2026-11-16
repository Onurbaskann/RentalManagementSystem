using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Services.Interfaces.Leases;

namespace KiraTakip.Services.Leases;

public class LeaseExpirationService(
    ILeaseRepository leaseRepository,
    IUnitOfWork unitOfWork,
    IConcurrencyViolationDetector concurrencyDetector) : ILeaseExpirationService, ITransactionalService
{
    public Task<List<int>> FindCandidatesAsync(int batchSize)
    {
        Guard.Against(
            batchSize <= 0,
            "Sözleşme sona erme batch boyutu pozitif olmalıdır.",
            "LEASE_EXPIRATION_INVALID_BATCH_SIZE");
        return leaseRepository.GetExpiredActiveLeaseIdsAsync(DateTime.Now.Date, batchSize);
    }

    public async Task<bool> ExpireAsync(int leaseId)
    {
        Guard.Against(
            leaseId <= 0,
            "Sözleşme bilgisi geçersizdir.",
            "LEASE_EXPIRATION_INVALID_ID");

        var lease = await leaseRepository.GetForExpirationAsync(leaseId);
        if (lease == null
            || lease.Status != LeaseStatus.Active
            || lease.EndDate.Date >= DateTime.Now.Date)
        {
            return false;
        }

        lease.Status = LeaseStatus.Ended;
        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = lease.Id,
            ActivityType = LeaseActivityType.Expiration,
            TransactionDate = DateTime.Now,
            Description = "Bitiş tarihi geçtiği için otomatik sona erdi.",
            OldEndDate = lease.EndDate,
            NewEndDate = lease.EndDate
        });

        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception exception) when (concurrencyDetector.IsConcurrencyViolation(exception))
        {
            return false;
        }

        return true;
    }
}
