using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class DekontService : IDekontService
{
    private readonly IDekontRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DekontService(IDekontRepository repo, IUnitOfWork uow, IWebHostEnvironment env, IConfiguration config)
    {
        _repo = repo;
        _uow = uow;
        _env = env;
        _config = config;
    }

    private string StorageKokYolu()
    {
        var relativePath = _config["DekontStoragePath"] ?? "Storage/Dekontlar";
        return Path.Combine(_env.ContentRootPath, relativePath);
    }

    public string GetTamYol(string dosyaYolu)
        => Path.Combine(_env.ContentRootPath, dosyaYolu);

    public async Task<Dekont> EkleAsync(int odemeId, IFormFile dosya, string userId)
    {
        var odeme = await _repo.GetOdemeInfoAsync(odemeId)
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

        await _repo.AddAsync(dekont);
        await _uow.SaveChangesAsync();
        return dekont;
    }

    public Task<List<DekontListItemDto>> GetByOdemeIdAsync(int odemeId)
        => _repo.GetByOdemeIdAsync(odemeId);

    public Task<DekontDetayDto?> GetByIdAsync(int id)
        => _repo.GetDetayAsync(id);

    public async Task SilAsync(int id)
    {
        var dosyaYolu = await _repo.GetByIdAsync<string?>(id, d => d.DosyaYolu);
        if (dosyaYolu == null) return;

        var tamYol = GetTamYol(dosyaYolu);
        if (File.Exists(tamYol))
            File.Delete(tamYol);

        await _repo.DeleteAsync(id, hardDelete: true);
        await _uow.SaveChangesAsync();
    }
}
