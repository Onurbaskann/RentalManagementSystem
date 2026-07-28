using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class DocumentRepository(ApplicationDbContext context)
    : BaseRepository<Document>(context), IDocumentRepository
{
    public async Task<List<Document>> GetListAsync(DocumentOwnerType ownerType, int ownerId)
        => await _dbSet
            .AsNoTracking()
            .Include(document => document.DocumentType)
            .Where(document => document.OwnerType == ownerType && document.OwnerId == ownerId && !document.IsInvalid)
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync();

    public async Task<List<Document>> GetListAsync(
        DocumentOwnerType ownerType,
        IReadOnlyCollection<int> ownerIds)
        => await _dbSet
            .AsNoTracking()
            .Include(document => document.DocumentType)
            .Where(document => document.OwnerType == ownerType
                && ownerIds.Contains(document.OwnerId)
                && !document.IsInvalid)
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync();

    public async Task<Document?> GetCurrentAsync(DocumentOwnerType ownerType, int ownerId, int documentTypeId)
        => await _dbSet
            .Where(document => document.OwnerType == ownerType && document.OwnerId == ownerId
                && document.DocumentTypeId == documentTypeId && !document.IsInvalid && !document.IsDeleted)
            .FirstOrDefaultAsync();

    public async Task<Document?> GetMetadataAsync(int documentId)
        => await _dbSet
            .AsNoTracking()
            .Include(document => document.DocumentType)
            .FirstOrDefaultAsync(document => document.Id == documentId);

    public async Task<Document?> FindAsync(int documentId)
        => await _dbSet.FindAsync(documentId);
}
