using Microsoft.EntityFrameworkCore;
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace KiraTakip.Services;

public class DekontService : IDekontService
{
    private readonly ApplicationDbContext _ctx;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DekontService(ApplicationDbContext ctx, IWebHostEnvironment env, IConfiguration config)
    {
        _ctx = ctx;
        _env = env;
        _config = config;
    }

    private string StorageKokYolu()
    {
        var relativePath = _config["DekontStoragePath"] ?? "Storage/Dekontlar";
        return Path.Combine(_env.ContentRootPath, relativePath);
    }

    public string GetTamYol(Dekont dekont)
        => Path.Combine(_env.ContentRootPath, dekont.DosyaYolu);

    public async Task<Dekont> EkleAsync(int odemeId, IFormFile dosya, string userId)
    {
        var odeme = await _ctx.KiraOdemeler.FindAsync(odemeId)
            ?? throw new InvalidOperationException("Ödeme bulunamadı.");

        var kok = StorageKokYolu();
        var folderName = odeme.KiraSozlesmesiId?.ToString() ?? $"t{odeme.KiraTahakkukId}";
        var klasor = Path.Combine(kok, folderName);
        Directory.CreateDirectory(klasor);

        var uzanti = Path.GetExtension(dosya.FileName);
        var diskAdi = $"{Guid.NewGuid()}{uzanti}";
        var tamYol = Path.Combine(klasor, diskAdi);

        await using (var stream = File.Create(tamYol))
            await dosya.CopyToAsync(stream);

        var relativePath = Path.Combine(
            _config["DekontStoragePath"] ?? "Storage/Dekontlar",
            folderName,
            diskAdi);

        var dekont = new Dekont
        {
            KiraOdemeId = odemeId,
            OrijinalDosyaAdi = dosya.FileName,
            DiskDosyaAdi = diskAdi,
            DosyaYolu = relativePath,
            DosyaTipi = dosya.ContentType,
            DosyaBoyutu = dosya.Length,
            YukleyenUserId = userId,
            YuklemeTarihi = DateTime.Now
        };

        _ctx.Dekontlar.Add(dekont);
        await _ctx.SaveChangesAsync();
        return dekont;
    }

    public async Task<List<Dekont>> GetByOdemeIdAsync(int odemeId)
        => await _ctx.Dekontlar
            .Include(d => d.YukleyenUser)
            .Where(d => d.KiraOdemeId == odemeId)
            .ToListAsync();

    public async Task<Dekont?> GetByIdAsync(int id)
        => await _ctx.Dekontlar
            .Include(d => d.KiraOdeme)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task SilAsync(int id)
    {
        var dekont = await _ctx.Dekontlar.FindAsync(id);
        if (dekont == null) return;

        var tamYol = GetTamYol(dekont);
        if (File.Exists(tamYol))
            File.Delete(tamYol);

        _ctx.Dekontlar.Remove(dekont);
        await _ctx.SaveChangesAsync();
    }
}
