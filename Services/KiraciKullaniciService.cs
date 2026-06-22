using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class KiraciKullaniciService : IKiraciKullaniciService
{
    private readonly ApplicationDbContext _db;

    public KiraciKullaniciService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasSonYetkiliAsync(int kiraciId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default)
    {
        var query = from u in _db.Users
                    join ur in _db.UserRoller on u.Id equals ur.UserId
                    join r in _db.Roller on ur.RolId equals r.Id
                    join rp in _db.RolPermissions on r.Id equals rp.RolId
                    where u.KiraciId == kiraciId
                          && u.IsActive
                          && r.IsActive
                          && !r.IsDeleted
                          && rp.Permission == PermissionCatalog.KiraciPortal.Kullanici.Manage
                    select new { UserId = u.Id, RolId = r.Id };

        if (excludeUserId != null)
            query = query.Where(x => x.UserId != excludeUserId);

        if (excludeRolId != null)
            query = query.Where(x => x.RolId != excludeRolId);

        return await query.AnyAsync(ct);
    }

    public async Task EnsureSonYetkiliAsync(int kiraciId, string? excludeUserId = null, int? excludeRolId = null, CancellationToken ct = default)
    {
        if (!await HasSonYetkiliAsync(kiraciId, excludeUserId, excludeRolId, ct))
            throw new InvalidOperationException("Sistemde en az bir aktif Firma Yetkilisi bulunmalıdır. Bu işlem onaylanamadı.");
    }
}
