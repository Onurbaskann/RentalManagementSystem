using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KiraTakip.Repositories.Interfaces;

public interface IInvitationRepository : IBaseRepository<Invitation>
{
    Task<List<TenantInvitationListItemDto>> GetPendingTenantListAsync(int tenantId, DateTime now, CancellationToken ct = default);
    Task<bool> HasPendingForTenantEmailAsync(int tenantId, string email, DateTime now, CancellationToken ct = default);
    Task<Invitation?> GetByIdAndTenantIdAsync(int id, int tenantId, CancellationToken ct = default);
    Task<Invitation?> GetInternalByIdAsync(int id, CancellationToken ct = default);
    Task<Invitation?> GetByTokenHashIgnoringFiltersAsync(string tokenHash, CancellationToken ct = default);
    Task<List<Invitation>> GetPendingInternalAsync(CancellationToken ct = default);
    Task MarkExpiredAsync(DateTime now, CancellationToken ct = default);
}
