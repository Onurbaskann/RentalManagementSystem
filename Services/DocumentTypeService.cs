using KiraTakip.Data;
using KiraTakip.Helpers;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.DocumentType;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using KiraTakip.Models.Common;

namespace KiraTakip.Services;

public class DocumentTypeService(
    IDocumentTypeRepository documentTypeRepository,
    IUnitOfWork uow) : IDocumentTypeService
{
    public async Task<List<DocumentType>> GetListAsync()
    {
        return await documentTypeRepository.GetListAsync();
    }

    public Task<PagedResult<DocumentType>> GetPagedListAsync(TableQuery query)
        => documentTypeRepository.GetPagedListAsync(query);

    public async Task<int> GetMaxSortOrderAsync()
    {
        return await documentTypeRepository.GetMaxSiraAsync();
    }

    public async Task CreateAsync(CreateInput input)
    {
        var kod = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await documentTypeRepository.KodExistsAsync(kod),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        var entity = new DocumentType
        {
            Code = kod,
            Name = input.Name.Trim(),
            Description = input.Description?.Trim(),
            TargetEntity = input.TargetEntity,
            Required = input.Required,
            AllowedExtensions = input.AllowedExtensions.Trim().ToLowerInvariant(),
            MaxSizeMb = input.MaxSizeMb,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive,
            IsSystem = false
        };

        await documentTypeRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
    }

    public async Task<DocumentType?> GetByIdAsync(int id)
    {
        return await documentTypeRepository.GetByIdAsync(id);
    }

    public async Task UpdateAsync(int id, EditInput input)
    {
        var entity = await documentTypeRepository.GetByIdAsync(id);
        entity = Guard.NotFound(entity != null && !entity.IsDeleted ? entity : null, "Belge türü bulunamadı.");

        var kod = CodeSlugger.ToCode(input.Name);
        Guard.InvalidField(
            await documentTypeRepository.KodExistsAsync(kod, excludeId: id),
            nameof(input.Name),
            "Bu ad zaten kullanılıyor. Farklı bir ad girin.");

        // Sistem tiplerinde HedefEntite değiştirilemez
        var targetEntity = entity.IsSystem ? entity.TargetEntity : input.TargetEntity;

        entity.Code = kod;
        entity.Name = input.Name.Trim();
        entity.Description = input.Description?.Trim();
        entity.TargetEntity = targetEntity;
        entity.Required = input.Required;
        entity.AllowedExtensions = input.AllowedExtensions.Trim().ToLowerInvariant();
        entity.MaxSizeMb = input.MaxSizeMb;
        entity.SortOrder = input.SortOrder;
        entity.IsActive = input.IsActive;

        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var entity = await documentTypeRepository.GetByIdAsync(id);
        entity = Guard.NotFound(entity != null && !entity.IsDeleted ? entity : null, "Belge türü bulunamadı.");

        Guard.Conflict(entity.IsActive && entity.IsSystem, "Sistem kaydı pasif yapılamaz.");

        entity.IsActive = !entity.IsActive;
        await uow.SaveChangesAsync();

        return entity.IsActive;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await documentTypeRepository.GetByIdAsync(id);
        entity = Guard.NotFound(entity != null && !entity.IsDeleted ? entity : null, "Belge türü bulunamadı.");

        Guard.Conflict(entity.IsSystem, "Sistem kaydı silinemez.");

        entity.IsDeleted = true;
        await uow.SaveChangesAsync();
    }
}
