using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BelgeTuruRepository : BaseRepository<BelgeTuru>, IBelgeTuruRepository
{
    public BelgeTuruRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<BelgeTuru>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.Sira).ThenBy(b => b.Ad)
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.Sira) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Kod == kod && !b.IsDeleted && (excludeId == null || b.Id != excludeId));
}
