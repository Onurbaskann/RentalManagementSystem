using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces.Common;
using KiraTakip.Models.Dtos.TenantUser;

namespace KiraTakip.Repositories.Interfaces.Identity;

public interface IInvitationRepository : IRepositoryBase<Invitation>
{
    Task<List<TenantInvitationListItemDto>> GetPendingTenantListAsync(int tenantId, DateTime now, CancellationToken ct = default);
    Task<bool> HasPendingForTenantEmailAsync(int tenantId, string email, DateTime now, CancellationToken ct = default);
    Task<Invitation?> GetByIdAndTenantIdAsync(int id, int tenantId, CancellationToken ct = default);
    Task<Invitation?> GetInternalByIdAsync(int id, CancellationToken ct = default);
    Task<Invitation?> GetByTokenHashIgnoringFiltersAsync(string tokenHash, CancellationToken ct = default);
    Task<List<Invitation>> GetPendingInternalAsync(CancellationToken ct = default);
    Task MarkExpiredAsync(DateTime now, CancellationToken ct = default);
}
