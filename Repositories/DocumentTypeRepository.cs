using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class DocumentTypeRepository : BaseRepository<DocumentType>, IDocumentTypeRepository
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
}
