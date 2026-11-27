using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Spaces;

public class SpaceRepository(ApplicationDbContext ctx) : RepositoryBase<Space>(ctx), ISpaceRepository
{
    public Task<Space?> GetNoTrackingAsync(int id)
        => _dbSet.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<SpaceTreeRow>> GetRootRowsAsync(SpaceAccessScope scope)
    {
        if (scope.HasGlobalAccess)
            return await ToRows(_dbSet.AsNoTracking().Where(s => s.ParentId == null)).ToListAsync();

        var roots = scope.RootPaths.Where(p => !string.IsNullOrEmpty(p)).ToList();
        var rows = await ToRows(_dbSet.AsNoTracking().Where(s => roots.Any(p => s.Path.StartsWith(p)))).ToListAsync();
        var ids = rows.Select(r => r.Id).ToHashSet();
        return rows.Where(r => r.ParentId is not int parentId || !ids.Contains(parentId)).ToList();
    }

    public Task<List<SpaceTreeRow>> GetTreeRowsAsync(string rootPath)
        => ToRows(_dbSet.AsNoTracking().Where(s => s.Path.StartsWith(rootPath))).ToListAsync();

    public Task<List<Space>> GetAncestorsAsync(string path)
    {
        var ids = SpaceTreePolicy.GetAncestorIds(path);
        return _dbSet.AsNoTracking().Where(s => ids.Contains(s.Id)).OrderBy(s => s.Depth).ToListAsync();
    }

    public Task<List<Space>> GetSubtreeForUpdateAsync(string path)
        => _dbSet.Where(s => s.Path.StartsWith(path)).OrderBy(s => s.Depth).ToListAsync();

    public Task<List<Space>> GetChildrenAsync(int parentId)
        => _dbSet.AsNoTracking().Where(s => s.ParentId == parentId).OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();

    public Task<bool> CodeExistsAsync(int? parentId, string code, int? excludeId = null)
        => _dbSet.AsNoTracking().AnyAsync(s => s.ParentId == parentId && s.Code == code
                                               && (excludeId == null || s.Id != excludeId));

    public Task<bool> HasChildrenAsync(int spaceId)
        => _dbSet.AsNoTracking().AnyAsync(s => s.ParentId == spaceId);

    public async Task<IReadOnlyList<string>> GetPathsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
            .Where(s => !s.IsDeleted && ids.Contains(s.Id))
            .Select(s => s.Path)
            .ToListAsync(ct);

    public async Task<List<SpaceOptionDto>> GetActiveOptionsAsync()
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name, s.Path })
            .ToListAsync();
        var names = rows.ToDictionary(row => row.Id, row => row.Name);
        return rows
            .Select(row => new SpaceOptionDto(
                row.Id,
                row.Name,
                string.Join(" / ", SpaceTreePolicy.GetAncestorIds(row.Path)
                    .Where(names.ContainsKey)
                    .Select(id => names[id])
                    .Append(row.Name))))
            .OrderBy(option => option.NamePath, StringComparer.CurrentCulture)
            .ToList();
    }

    public Task<bool> AnyReservableAsync(IEnumerable<int> spaceIds)
    {
        var ids = spaceIds.ToList();
        return _dbSet.AsNoTracking().AnyAsync(s => ids.Contains(s.Id) && s.Usage == SpaceUsage.Reservable);
    }

    public Task<bool> HasReservableDescendantAsync(string path)
        => _dbSet.AsNoTracking().AnyAsync(s => s.Path.StartsWith(path) && s.Path != path && s.Usage == SpaceUsage.Reservable);

    public Task<bool> IsSpaceTypeUsedAsync(int spaceTypeId)
        => _dbSet.AsNoTracking().AnyAsync(s => s.SpaceTypeId == spaceTypeId);

    public async Task<SpaceOpenRecords> GetOpenRecordsAsync(int spaceId)
    {
        var today = DateTime.Today;
        var openLease = await ctx.LeaseAreas.AsNoTracking().AnyAsync(a =>
            !a.IsDeleted && a.SpaceId == spaceId && !a.Lease.IsDeleted
            && (a.Lease.Status == LeaseStatus.Active
                || a.Lease.Status == LeaseStatus.Draft
                || a.Lease.Status == LeaseStatus.RevisionRequested
                || (a.Lease.Status == LeaseStatus.Terminated && a.Lease.TerminationDate > today)));

        var now = DateTime.Now;
        var openReservation = await ctx.Reservations.AsNoTracking().AnyAsync(r =>
            !r.IsDeleted && r.SpaceId == spaceId && r.EndDate > now
            && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.PendingApproval));

        // ActivePreAllocation: ön tahsis tablosu henüz yok (F2b); bayrak false kalır.
        return new SpaceOpenRecords(new OpenRecordFlags(openLease, false, openReservation), OpenRecordFlags.None, OpenRecordFlags.None);
    }

    public async Task<bool> HasHistoryAsync(int spaceId)
        => await ctx.LeaseAreas.AsNoTracking().AnyAsync(a => !a.IsDeleted && a.SpaceId == spaceId)
           || await ctx.Reservations.AsNoTracking().AnyAsync(r => !r.IsDeleted && r.SpaceId == spaceId)
           || await ctx.Charges.AsNoTracking().AnyAsync(c => !c.IsDeleted && c.SpaceId == spaceId);

    public void AddHistory(SpaceHistory history) => ctx.SpaceHistories.Add(history);

    public Task<List<SpaceHistoryDto>> GetHistoryAsync(int spaceId)
        => ctx.SpaceHistories.AsNoTracking()
            .Where(h => h.SpaceId == spaceId)
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .Select(h => new SpaceHistoryDto(h.Field, h.OldValue, h.NewValue, h.ChangedAt, h.UserId))
            .ToListAsync();

    public void AddAddress(SpaceAddress address) => ctx.SpaceAddresses.Add(address);

    public async Task<SpaceLinkedDefinitions> GetLinkedDefinitionsAsync(int spaceId)
        => new(
            await ctx.SpaceAddresses.FirstOrDefaultAsync(a => a.SpaceId == spaceId),
            await ctx.SpaceAttributeValues.Include(v => v.AttributeDefinition).Where(v => v.SpaceId == spaceId).ToListAsync(),
            await ctx.SpaceRates.Include(r => r.TenantCategory).Include(r => r.ChargeType).Where(r => r.SpaceId == spaceId).ToListAsync(),
            await ctx.RezervasyonTarifeler.Include(r => r.ChargeType).Where(r => r.SpaceId == spaceId).ToListAsync(),
            await ctx.PaymentStoreRoutings.Include(r => r.ChargeType).Include(r => r.Store).Where(r => r.SpaceId == spaceId).ToListAsync());

    private static IQueryable<SpaceTreeRow> ToRows(IQueryable<Space> query)
        => query.OrderBy(s => s.Depth).ThenBy(s => s.SortOrder).ThenBy(s => s.Name)
            .Select(s => new SpaceTreeRow(
                s.Id, s.ParentId, s.Code, s.Name, s.SpaceType.Name, s.Usage, s.Area, s.IsActive, s.Depth, s.SortOrder));
}
