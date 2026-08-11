using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Models.Common;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class DocumentTypeRepository : RepositoryBase<DocumentType>, IDocumentTypeRepository
{
    public DocumentTypeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<DocumentType>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.SortOrder) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Code == kod && !b.IsDeleted && (excludeId == null || b.Id != excludeId));

    public Task<List<DocumentType>> GetForTargetAsync(KiraTakip.Models.DocumentOwnerType targetEntity, bool requiredOnly)
        => _dbSet
            .AsNoTracking()
            .Where(type => type.TargetEntity == targetEntity && type.IsActive && (!requiredOnly || type.Required))
            .OrderBy(type => type.SortOrder)
            .ToListAsync();

    public Task<PagedResult<DocumentType>> GetPagedListAsync(TableQuery tableQuery)
    {
        var query = _dbSet.AsNoTracking().Where(type => !type.IsDeleted);
        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(type => type.Name.Contains(search) || type.Code.Contains(search));
        }
        return GetPagedResultAsync(
            query,
            query.OrderBy(type => type.SortOrder).ThenBy(type => type.Name).ThenBy(type => type.Id),
            tableQuery);
    }
}
