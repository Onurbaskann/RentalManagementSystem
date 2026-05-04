using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class KiraciService : IKiraciService
{
    private readonly ApplicationDbContext _ctx;

    public KiraciService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<Kiraci>> GetAllAsync(string? userId = null)
    {
        if (userId != null)
        {
            var yetkiliTasinmazIds = await _ctx.UserTasinmazYetkileri
                .Where(u => u.UserId == userId)
                .Select(u => u.TasinmazId)
                .ToListAsync();

            var yetkiliKiraciIds = await _ctx.Sozlesmeler
                .Where(s => yetkiliTasinmazIds.Contains(s.Birim.TasinmazId))
                .Select(s => s.KiraciId)
                .Distinct()
                .ToListAsync();

            return await _ctx.Kiraciler
                .Where(k => yetkiliKiraciIds.Contains(k.Id))
                .OrderBy(k => k.Ad)
                .ToListAsync();
        }

        return await _ctx.Kiraciler.OrderBy(k => k.Ad).ToListAsync();
    }

    public async Task<Kiraci?> GetByIdAsync(int id)
    {
        return await _ctx.Kiraciler.FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<Kiraci> CreateAsync(Kiraci k)
    {
        if (string.IsNullOrWhiteSpace(k.KiraciNo))
            k.KiraciNo = await GenerateKiraciNoAsync();
        k.KayitTarihi = DateTime.Now;
        _ctx.Kiraciler.Add(k);
        await _ctx.SaveChangesAsync();
        return k;
    }

    public async Task UpdateAsync(Kiraci k)
    {
        _ctx.Kiraciler.Update(k);
        await _ctx.SaveChangesAsync();
    }

    public async Task<string> GenerateKiraciNoAsync()
    {
        var existing = await _ctx.Kiraciler.Select(k => k.KiraciNo).ToListAsync();
        var usedSet = existing.ToHashSet();
        for (int i = 1; i <= 999999; i++)
        {
            var no = $"KRC-{i:D6}";
            if (!usedSet.Contains(no)) return no;
        }
        throw new InvalidOperationException("KiraciNo üretilemedi.");
    }

    public async Task<bool> KiraciNoExistsAsync(string kiraciNo, int? excludeId = null)
    {
        return await _ctx.Kiraciler.AnyAsync(k =>
            k.KiraciNo == kiraciNo && (excludeId == null || k.Id != excludeId));
    }
}
