using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class TenantUserService : ITenantUserService
{
    private readonly ApplicationDbContext _db;

    public TenantUserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasSonYetkiliAsync(int tenantId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default)
    {
        var query = from u in _db.Users
                    join ur in _db.UserRoller on u.Id equals ur.UserId
                    join r in _db.Roller on ur.RolId equals r.Id
                    where u.KiraciId == tenantId
                          && u.IsActive
                          && r.IsActive
                          && !r.IsDeleted
                          && r.Ad == RoleNames.KiraciYoneticisi
                    select new { UserId = u.Id, RolId = r.Id };

        if (excludeUserId != null)
            query = query.Where(x => x.UserId != excludeUserId);

        if (excludeRolId != null)
            query = query.Where(x => x.RolId != excludeRolId);

        return await query.AnyAsync(ct);
    }

    public async Task EnsureSonYetkiliAsync(int tenantId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default)
    {
        if (!await HasSonYetkiliAsync(tenantId, excludeUserId, excludeRolId, ct))
            throw new InvalidOperationException("Sistemde en az bir aktif Firma Yetkilisi bulunmalıdır. Bu işlem onaylanamadı.");
    }
}
