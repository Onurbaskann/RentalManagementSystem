using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _db;
    private readonly IUnitOfWork _uow;

    public DocumentService(ApplicationDbContext db, IUnitOfWork uow)
    {
        _db = db;
        _uow = uow;
    }

    public async Task<List<Belge>> GetListAsync(BelgeOwnerTipi ownerType, int ownerId)
        => await _db.Belgeler
            .AsNoTracking()
            .Include(b => b.DocumentType)
            .Where(b => b.OwnerType == ownerType && b.OwnerId == ownerId && !b.Gecersiz)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<Belge> UploadAsync(BelgeOwnerTipi ownerType, int ownerId, int documentTypeId,
        string dosyaAdi, string mimeType, byte[] icerik, string? aciklama = null, bool invalidateOld = true)
    {
        var eskiBelge = invalidateOld
            ? await _db.Belgeler
                .Where(b => b.OwnerType == ownerType && b.OwnerId == ownerId
                         && b.DocumentTypeId == documentTypeId && !b.Gecersiz && !b.IsDeleted)
                .FirstOrDefaultAsync()
            : null;

        var yeni = new Belge
        {
            DocumentTypeId = documentTypeId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            DosyaAdi = dosyaAdi,
            MimeType = mimeType,
            BoyutByte = icerik.Length,
            Aciklama = aciklama,
            IsActive = true,
            Icerik = new BelgeIcerik { Icerik = icerik }
        };

        await _db.Belgeler.AddAsync(yeni);
        await _uow.SaveChangesAsync(); // Id üretiliyor

        if (eskiBelge != null)
        {
            eskiBelge.Gecersiz = true;
            eskiBelge.GecersizlikTarihi = DateTime.UtcNow;
            eskiBelge.DegistirenBelgeId = yeni.Id;
            await _uow.SaveChangesAsync();
        }

        return yeni;
    }

    public async Task<(Belge Meta, byte[] Icerik)> DownloadAsync(int belgeId)
    {
        var meta = await _db.Belgeler
            .AsNoTracking()
            .Include(b => b.DocumentType)
            .FirstOrDefaultAsync(b => b.Id == belgeId)
            ?? throw new KeyNotFoundException($"Belge {belgeId} bulunamadı.");

        var icerik = await _db.BelgeIcerikleri
            .AsNoTracking()
            .Where(i => i.BelgeId == belgeId)
            .Select(i => i.Icerik)
            .FirstOrDefaultAsync()
            ?? Array.Empty<byte>();

        return (meta, icerik);
    }

    public async Task DeleteAsync(int belgeId)
    {
        var belge = await _db.Belgeler.FindAsync(belgeId);
        if (belge == null) return;

        belge.IsDeleted = true;
        await _uow.SaveChangesAsync();
    }

    public async Task<List<DocumentType>> GetTurlerAsync(BelgeOwnerTipi hedefEntite, bool sadeceDogru = false)
        => await _db.DocumentTypes
            .AsNoTracking()
            .Where(t => t.TargetEntity == hedefEntite && t.IsActive && (!sadeceDogru || t.Required))
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
}
