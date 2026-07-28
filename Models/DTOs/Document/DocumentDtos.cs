using KiraTakip.Models.Entities;

namespace KiraTakip.Models.Dtos;

public record GetDocumentsInput(
    DocumentOwnerType OwnerType,
    int OwnerId,
    DocumentAccessScopeInput? AccessScope = null);

public record UploadDocumentInput(
    DocumentOwnerType OwnerType,
    int OwnerId,
    int DocumentTypeId,
    string FileName,
    string MimeType,
    byte[] Content,
    string? Description = null,
    bool InvalidateOld = true,
    DocumentAccessScopeInput? AccessScope = null);

public record DownloadDocumentInput(
    int DocumentId,
    DocumentAccessScopeInput AccessScope);

public record DeleteDocumentInput(
    int DocumentId,
    DocumentAccessScopeInput AccessScope);

public record GetDocumentTypesInput(
    DocumentOwnerType TargetEntity,
    bool RequiredOnly = false);

public record DocumentDownloadResult(Document Metadata, byte[] Content);

public record DocumentMutationResult(DocumentOwnerType OwnerType, int OwnerId);

public record DocumentAccessScopeInput(
    IReadOnlyList<DocumentOwnerType> AllowedOwnerTypes,
    IReadOnlyList<int>? PropertyIds = null,
    IReadOnlyList<int>? UnitIds = null,
    int? TenantId = null);

public record GetDocumentsForOwnersInput(
    DocumentOwnerType OwnerType,
    IReadOnlyList<int> OwnerIds);

public record DocumentOwnerContextDto(
    int TenantId,
    IReadOnlyList<int> PropertyIds,
    IReadOnlyList<int> UnitIds);
