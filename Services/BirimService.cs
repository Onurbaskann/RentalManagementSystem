using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services;

public class BirimService : IBirimService
{
    private readonly ApplicationDbContext _ctx;

    public BirimService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<Birim>> GetByTasinmazIdAsync(int tasinmazId)
    {
        return await _ctx.Birimler
            .Include(b => b.Sozlesmeler)
                .ThenInclude(s => s.Kiraci)
            .Where(b => b.TasinmazId == tasinmazId)
            .ToListAsync();
    }

    public async Task<Birim?> GetByIdAsync(int id)
    {
        return await _ctx.Birimler
            .Include(b => b.Tasinmaz)
            .Include(b => b.Sozlesmeler)
                .ThenInclude(s => s.Kiraci)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task CreateAsync(Birim b)
    {
        _ctx.Birimler.Add(b);
        await _ctx.SaveChangesAsync();
    }

    public async Task UpdateAsync(Birim b)
    {
        _ctx.Birimler.Update(b);
        await _ctx.SaveChangesAsync();
    }
}
