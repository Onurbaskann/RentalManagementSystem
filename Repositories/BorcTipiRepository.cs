using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class BorcTipiRepository : BaseRepository<ChargeType>, IBorcTipiRepository
{
    public BorcTipiRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<BorcTipiLookupDto>> GetManuelBorcTipleriAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => b.IsActive && b.Behavior == ChargeTypeBehavior.UserManual)
            .OrderBy(b => b.SortOrder)
            .Select(b => new BorcTipiLookupDto
            {
                Id = b.Id,
                Ad = b.Name,
                Kod = b.Code,
                Davranis = b.Behavior
            })
            .ToListAsync();

    public async Task<ChargeType?> GetActiveManuelByIdAsync(int id)
        => await _dbSet
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive && b.Behavior == ChargeTypeBehavior.UserManual);

    public async Task<List<BorcTipiListItemDto>> GetListAsync()
        => await _dbSet.AsNoTracking()
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .Select(b => new BorcTipiListItemDto
            {
                Id = b.Id,
                Ad = b.Name,
                Kod = b.Code,
                Davranis = b.Behavior,
                Sira = b.SortOrder,
                Sistem = b.IsSystem,
                Aktif = b.IsActive
            })
            .ToListAsync();

    public async Task<int> GetMaxSiraAsync()
        => await _dbSet.AsNoTracking().MaxAsync(b => (int?)b.SortOrder) ?? 0;

    public async Task<bool> KodExistsAsync(string kod, int? excludeId = null)
        => await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Code == kod && (excludeId == null || b.Id != excludeId));

    public async Task<List<BorcTipiLookupDto>> GetRezervasyonAdaylariAsync()
        => await _dbSet.AsNoTracking()
            .Where(b => b.Behavior == ChargeTypeBehavior.ReservationSpecific && b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new BorcTipiLookupDto
            {
                Id = b.Id,
                Ad = b.Name,
                Kod = b.Code,
                Davranis = b.Behavior
            })
            .ToListAsync();
}
