using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IBelgeService
{
    Task<List<Belge>> GetListAsync(BelgeOwnerTipi ownerType, int ownerId);

    Task<Belge> UploadAsync(BelgeOwnerTipi ownerType, int ownerId, int documentTypeId,
        string dosyaAdi, string mimeType, byte[] icerik, string? aciklama = null, bool invalidateOld = true);

    Task<(Belge Meta, byte[] Icerik)> DownloadAsync(int belgeId);

    Task DeleteAsync(int belgeId);

    Task<List<DocumentType>> GetTurlerAsync(BelgeOwnerTipi hedefEntite, bool sadeceDogru = false);
}
