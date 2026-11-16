namespace KiraTakip.Services.Interfaces.Leases;

public interface ILeaseExpirationService
{
    Task<List<int>> FindCandidatesAsync(int batchSize);
    Task<bool> ExpireAsync(int leaseId);
}
