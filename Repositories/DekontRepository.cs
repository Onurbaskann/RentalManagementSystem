using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class DekontRepository : BaseRepository<Dekont>, IDekontRepository
{
    public DekontRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<DekontListItemDto>> GetByOdemeIdAsync(int odemeId)
        => await _dbSet.AsNoTracking()
                       .Where(d => d.KiraOdemeId == odemeId)
                       .Select(d => new DekontListItemDto
                       {
                           Id = d.Id,
                           KiraOdemeId = d.KiraOdemeId,
                           OrijinalDosyaAdi = d.OrijinalDosyaAdi,
                           DosyaTipi = d.DosyaTipi,
                           DosyaBoyutu = d.DosyaBoyutu,
                           YuklemeTarihi = d.YuklemeTarihi,
                           YukleyenUserAdi = d.YukleyenUser != null ? d.YukleyenUser.UserName : null
                       })
                       .ToListAsync();

    public async Task<DekontDetayDto?> GetDetayAsync(int id)
        => await _dbSet.AsNoTracking()
                       .Where(d => d.Id == id)
                       .Select(d => new DekontDetayDto
                       {
                           Id = d.Id,
                           KiraOdemeId = d.KiraOdemeId,
                           OrijinalDosyaAdi = d.OrijinalDosyaAdi,
                           DiskDosyaAdi = d.DiskDosyaAdi,
                           DosyaYolu = d.DosyaYolu,
                           DosyaTipi = d.DosyaTipi,
                           DosyaBoyutu = d.DosyaBoyutu,
                           YuklemeTarihi = d.YuklemeTarihi,
                           YukleyenUserAdi = d.YukleyenUser != null ? d.YukleyenUser.UserName : null
                       })
                       .FirstOrDefaultAsync();

    public async Task<(int? KiraSozlesmesiId, int TahakkukId)?> GetOdemeInfoAsync(int odemeId)
    {
        var result = await _ctx.KiraOdemeler.AsNoTracking()
            .Where(o => o.Id == odemeId)
            .Select(o => new { o.KiraSozlesmesiId, o.TahakkukId })
            .FirstOrDefaultAsync();
        if (result == null) return null;
        return (result.KiraSozlesmesiId, result.TahakkukId);
    }
}
