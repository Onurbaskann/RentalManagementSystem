using KiraTakip.Data;
using KiraTakip.Models;
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

    public async Task<List<Document>> GetListAsync(DocumentOwnerType ownerType, int ownerId)
        => await _db.Belgeler
            .AsNoTracking()
            .Include(b => b.DocumentType)
            .Where(b => b.OwnerType == ownerType && b.OwnerId == ownerId && !b.IsInvalid)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<Document> UploadAsync(DocumentOwnerType ownerType, int ownerId, int documentTypeId,
        string fileName, string mimeType, byte[] content, string? description = null, bool invalidateOld = true)
    {
        var oldDocument = invalidateOld
            ? await _db.Belgeler
                .Where(b => b.OwnerType == ownerType && b.OwnerId == ownerId
                         && b.DocumentTypeId == documentTypeId && !b.IsInvalid && !b.IsDeleted)
                .FirstOrDefaultAsync()
            : null;

        var newDocument = new Document
        {
            DocumentTypeId = documentTypeId,
            OwnerType = ownerType,
            OwnerId = ownerId,
            FileName = fileName,
            MimeType = mimeType,
            FileSize = content.Length,
            Description = description,
            IsActive = true,
            Content = new DocumentContent { Content = content }
        };

        await _db.Belgeler.AddAsync(newDocument);
        await _uow.SaveChangesAsync(); // Id üretiliyor

        if (oldDocument != null)
        {
            oldDocument.IsInvalid = true;
            oldDocument.InvalidationDate = DateTime.UtcNow;
            oldDocument.ReplacedByDocumentId = newDocument.Id;
            await _uow.SaveChangesAsync();
        }

        return newDocument;
    }

    public async Task<(Document Meta, byte[] Icerik)> DownloadAsync(int documentId)
    {
        var meta = await _db.Belgeler
            .AsNoTracking()
            .Include(b => b.DocumentType)
            .FirstOrDefaultAsync(b => b.Id == documentId)
            ?? throw new KeyNotFoundException($"Belge {documentId} bulunamadı.");

        var icerik = await _db.DocumentContents
            .AsNoTracking()
            .Where(i => i.DocumentId == documentId)
            .Select(i => i.Content)
            .FirstOrDefaultAsync()
            ?? Array.Empty<byte>();

        return (meta, icerik);
    }

    public async Task DeleteAsync(int documentId)
    {
        var document = await _db.Belgeler.FindAsync(documentId);
        if (document == null) return;

        document.IsDeleted = true;
        await _uow.SaveChangesAsync();
    }

    public async Task<List<DocumentType>> GetTurlerAsync(DocumentOwnerType targetEntity, bool sadeceDogru = false)
        => await _db.DocumentTypes
            .AsNoTracking()
            .Where(t => t.TargetEntity == targetEntity && t.IsActive && (!sadeceDogru || t.Required))
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
}
