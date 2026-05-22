using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class SozlesmeTarifeRepository : BaseRepository<SozlesmeTarife>, ISozlesmeTarifeRepository
{
    public SozlesmeTarifeRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<RateValueDto?> GetRateAsync(int sozlesmeId, int borcTipiId)
        => await _dbSet.AsNoTracking()
            .Where(r => r.KiraSozlesmesiId == sozlesmeId && r.BorcTipiId == borcTipiId)
            .Select(r => new RateValueDto
            {
                HesaplamaYontemi = r.HesaplamaYontemi,
                BirimDeger = r.BirimDeger,
                KdvOrani = r.KdvOrani
            })
            .FirstOrDefaultAsync();
}
