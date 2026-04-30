using KiraTakip.Data;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class UserTasinmazYetkiService
{
    private readonly ApplicationDbContext _context;

    public UserTasinmazYetkiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<int>> GetYetkiliTasinmazIdsAsync(string userId)
    {
        return await _context.UserTasinmazYetkileri
            .Where(y => y.UserId == userId)
            .Select(y => y.TasinmazId)
            .ToListAsync();
    }

    public async Task<bool> CanViewTasinmazAsync(string userId, int tasinmazId)
    {
        return await _context.UserTasinmazYetkileri
            .AnyAsync(y => y.UserId == userId && y.TasinmazId == tasinmazId);
    }

    public async Task SetUserTasinmazYetkileriAsync(string userId, List<int> tasinmazIds, string atayanUserId)
    {
        var existing = await _context.UserTasinmazYetkileri
            .Where(y => y.UserId == userId)
            .ToListAsync();

        _context.UserTasinmazYetkileri.RemoveRange(existing);

        if (tasinmazIds != null && tasinmazIds.Any())
        {
            var newRecords = tasinmazIds.Select(tId => new UserTasinmazYetki
            {
                UserId = userId,
                TasinmazId = tId,
                AtanmaTarihi = DateTime.Now,
                AtayanUserId = atayanUserId
            });
            await _context.UserTasinmazYetkileri.AddRangeAsync(newRecords);
        }

        await _context.SaveChangesAsync();
    }
}
