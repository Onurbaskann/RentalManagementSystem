using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentContentRepository documentContentRepository,
    IDocumentTypeRepository documentTypeRepository,
    ITenantRepository tenantRepository,
    ILeaseRepository leaseRepository,
    IPaymentAllocationRepository paymentAllocationRepository,
    IUnitOfWork unitOfWork) : IDocumentService
{
    public async Task<List<Document>> GetListAsync(GetDocumentsInput input)
    {
        if (input.AccessScope != null)
        {
            var ownerContext = Guard.NotFound(
                await GetOwnerContextAsync(
                    input.OwnerType,
                    input.OwnerId,
                    input.AccessScope.TenantId.HasValue),
                "Belge sahibi kayıt bulunamadı.",
                "Document.OwnerNotFound");
            EnsureAccess(input.OwnerType, ownerContext, input.AccessScope);
        }

        return await documentRepository.GetListAsync(input.OwnerType, input.OwnerId);
    }

    public async Task<Dictionary<int, List<Document>>> GetListsAsync(
        GetDocumentsForOwnersInput input)
    {
        if (input.OwnerIds.Count == 0) return [];

        var documents = await documentRepository.GetListAsync(input.OwnerType, input.OwnerIds);
        return input.OwnerIds
            .Distinct()
            .ToDictionary(
                ownerId => ownerId,
                ownerId => documents.Where(document => document.OwnerId == ownerId).ToList());
    }

    public async Task UploadAsync(UploadDocumentInput input)
    {
        Guard.Against(
            input.OwnerType is not DocumentOwnerType.Tenant
                and not DocumentOwnerType.Lease
                and not DocumentOwnerType.Payment,
            "Geçersiz belge sahibi türü.",
            "Document.InvalidOwnerType");

        var documentType = Guard.NotFound(
            await documentTypeRepository.GetByIdAsync(input.DocumentTypeId),
            "Belge türü bulunamadı.",
            "DocumentType.NotFound");

        Guard.Conflict(
            !documentType.IsActive,
            "Pasif belge türüne dosya yüklenemez.",
            "DocumentType.Inactive");
        Guard.Conflict(
            documentType.TargetEntity != input.OwnerType,
            "Seçilen belge türü bu kayıt için kullanılamaz.",
            "DocumentType.OwnerMismatch");

        var extension = Path.GetExtension(input.FileName).TrimStart('.');
        var allowedExtensions = documentType.AllowedExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.TrimStart('.'));
        Guard.Against(
            string.IsNullOrWhiteSpace(extension)
                || !allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase),
            "Desteklenmeyen dosya türü.",
            "Document.UnsupportedExtension");
        Guard.Against(
            input.Content.LongLength > documentType.MaxSizeMb * 1024L * 1024L,
            $"Dosya boyutu {documentType.MaxSizeMb} MB sınırını aşıyor.",
            "Document.FileTooLarge");

        var ownerContext = Guard.NotFound(
            await GetOwnerContextAsync(
                input.OwnerType,
                input.OwnerId,
                input.AccessScope?.TenantId.HasValue == true),
            "Belge sahibi kayıt bulunamadı.",
            "Document.OwnerNotFound");
        if (input.AccessScope != null)
            EnsureAccess(input.OwnerType, ownerContext, input.AccessScope);

        var oldDocument = input.InvalidateOld
            ? await documentRepository.GetCurrentAsync(input.OwnerType, input.OwnerId, input.DocumentTypeId)
            : null;

        var newDocument = new Document
        {
            DocumentTypeId = input.DocumentTypeId,
            OwnerType = input.OwnerType,
            OwnerId = input.OwnerId,
            FileName = Path.GetFileName(input.FileName),
            MimeType = input.MimeType,
            FileSize = input.Content.Length,
            Description = input.Description,
            IsActive = true,
            Content = new DocumentContent { Content = input.Content }
        };

        await documentRepository.AddAsync(newDocument);
        await unitOfWork.SaveChangesAsync(); // Id üretiliyor

        if (oldDocument != null)
        {
            oldDocument.IsInvalid = true;
            oldDocument.InvalidationDate = DateTime.UtcNow;
            oldDocument.ReplacedByDocumentId = newDocument.Id;

            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<DocumentDownloadResult> DownloadAsync(DownloadDocumentInput input)
    {
        var metadata = Guard.NotFound(
            await documentRepository.GetMetadataAsync(input.DocumentId),
            $"Belge {input.DocumentId} bulunamadı.",
            "Document.NotFound");

        var ownerContext = Guard.NotFound(
            await GetOwnerContextAsync(
                metadata.OwnerType,
                metadata.OwnerId,
                input.AccessScope.TenantId.HasValue),
            "Belge sahibi kayıt bulunamadı.",
            "Document.OwnerNotFound");
        EnsureAccess(metadata.OwnerType, ownerContext, input.AccessScope);

        var content = Guard.NotFound(
            await documentContentRepository.GetContentAsync(input.DocumentId),
            "Belge içeriği bulunamadı.",
            "Document.ContentNotFound");

        return new DocumentDownloadResult(metadata, content);
    }

    public async Task<DocumentMutationResult> DeleteAsync(DeleteDocumentInput input)
    {
        var metadata = Guard.NotFound(
            await documentRepository.GetMetadataAsync(input.DocumentId),
            "Belge bulunamadı.",
            "Document.NotFound");

        var ownerContext = Guard.NotFound(
            await GetOwnerContextAsync(
                metadata.OwnerType,
                metadata.OwnerId,
                input.AccessScope.TenantId.HasValue),
            "Belge sahibi kayıt bulunamadı.",
            "Document.OwnerNotFound");
        EnsureAccess(metadata.OwnerType, ownerContext, input.AccessScope);
        Guard.Conflict(
            metadata.DocumentType.Required,
            "Zorunlu belge silinemez.",
            "Document.Required");

        var document = Guard.NotFound(
            await documentRepository.FindAsync(input.DocumentId),
            "Belge bulunamadı.",
            "Document.NotFound");
        document.IsDeleted = true;

        await unitOfWork.SaveChangesAsync();

        return new DocumentMutationResult(document.OwnerType, document.OwnerId);
    }

    public Task<List<DocumentType>> GetTypesAsync(GetDocumentTypesInput input)
        => documentTypeRepository.GetForTargetAsync(input.TargetEntity, input.RequiredOnly);

    private Task<DocumentOwnerContextDto?> GetOwnerContextAsync(
        DocumentOwnerType ownerType,
        int ownerId,
        bool tenantPortalOnly)
        => ownerType switch
        {
            DocumentOwnerType.Tenant => tenantRepository.GetDocumentOwnerContextAsync(ownerId),
            DocumentOwnerType.Lease => leaseRepository.GetDocumentOwnerContextAsync(
                ownerId,
                tenantPortalOnly),
            DocumentOwnerType.Payment => paymentAllocationRepository.GetDocumentOwnerContextAsync(ownerId),
            _ => Task.FromResult<DocumentOwnerContextDto?>(null)
        };

    private static void EnsureAccess(
        DocumentOwnerType ownerType,
        DocumentOwnerContextDto ownerContext,
        DocumentAccessScopeInput accessScope)
    {
        Guard.Forbidden(
            !accessScope.AllowedOwnerTypes.Contains(ownerType),
            "Bu belge üzerinde işlem yapma yetkiniz bulunmuyor.",
            "Document.Forbidden");

        Guard.Forbidden(
            accessScope.TenantId.HasValue
                && ownerContext.TenantId != accessScope.TenantId.Value,
            "Bu belge yetki kapsamınızın dışındadır.",
            "Document.TenantOutOfScope");

        var hasScopeRestriction = accessScope.PropertyIds != null || accessScope.UnitIds != null;
        if (!hasScopeRestriction) return;

        var propertyAccess = accessScope.PropertyIds?.Intersect(ownerContext.PropertyIds).Any() == true;
        var unitAccess = accessScope.UnitIds?.Intersect(ownerContext.UnitIds).Any() == true;
        Guard.Forbidden(
            !propertyAccess && !unitAccess,
            "Bu belge yetki kapsamınızın dışındadır.",
            "Document.OutOfScope");
    }
}
