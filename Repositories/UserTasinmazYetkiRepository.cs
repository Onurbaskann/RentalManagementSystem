using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class UserTasinmazYetkiRepository : IUserTasinmazYetkiRepository
{
    private readonly ApplicationDbContext _ctx;

    public UserTasinmazYetkiRepository(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<int>> GetYetkiliTasinmazIdsAsync(string userId)
        => await _ctx.UserTasinmazYetkileri
            .AsNoTracking()
            .Where(y => y.UserId == userId)
            .Select(y => y.TasinmazId)
            .ToListAsync();

    public async Task<bool> CanViewTasinmazAsync(string userId, int tasinmazId)
        => await _ctx.UserTasinmazYetkileri
            .AsNoTracking()
            .AnyAsync(y => y.UserId == userId && y.TasinmazId == tasinmazId);

    public async Task<List<UserTasinmazYetki>> GetForUserAsync(string userId)
        => await _ctx.UserTasinmazYetkileri
            .Where(y => y.UserId == userId)
            .ToListAsync();

    public Task RemoveRangeAsync(IEnumerable<UserTasinmazYetki> entities)
    {
        _ctx.UserTasinmazYetkileri.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task AddRangeAsync(IEnumerable<UserTasinmazYetki> entities)
        => await _ctx.UserTasinmazYetkileri.AddRangeAsync(entities);
}
