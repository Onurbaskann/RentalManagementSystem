using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Spaces;

namespace KiraTakip.Services.Spaces;

public class SpaceTypeService(
    ISpaceTypeRepository spaceTypeRepository,
    ISpaceAttributeRepository attributeRepository,
    ISpaceRepository spaceRepository,
    IUnitOfWork uow) : ISpaceTypeService
{
    private const string CodeField = "Code";
    private const string NameField = "Name";
    private const string DefaultAttributesField = "DefaultAttributeIds";

    public async Task<List<SpaceTypeDto>> GetListAsync()
    {
        var defaults = await spaceTypeRepository.GetDefaultAttributeIdsAsync();
        return (await spaceTypeRepository.GetAllOrderedAsync())
            .Select(t => new SpaceTypeDto(
                t.Id, t.Code, t.Name, t.SortOrder, t.DefaultUsage, t.DefaultIsPreAllocatable, t.IsActive,
                defaults.TryGetValue(t.Id, out var ids) ? ids : []))
            .ToList();
    }

    public Task<int> CreateAsync(SpaceTypeInput input)
        => uow.ExecuteInTransactionAsync(async _ =>
        {
            var (code, name) = await ValidateAsync(input, null);
            var entity = new SpaceType
            {
                Code = code,
                Name = name,
                SortOrder = input.SortOrder,
                DefaultUsage = input.DefaultUsage,
                DefaultIsPreAllocatable = input.DefaultIsPreAllocatable
            };
            await spaceTypeRepository.AddAsync(entity);
            await uow.SaveChangesAsync();
            await SyncDefaultsAsync(entity.Id, input.DefaultAttributeIds);
            await uow.SaveChangesAsync();
            return entity.Id;
        });

    public Task UpdateAsync(int id, SpaceTypeInput input)
        => uow.ExecuteInTransactionAsync(async _ =>
        {
            var entity = Guard.NotFound(await spaceTypeRepository.GetByIdAsync(id), "Alan türü bulunamadı.");
            var (code, name) = await ValidateAsync(input, id);
            entity.Code = code;
            entity.Name = name;
            entity.SortOrder = input.SortOrder;
            entity.DefaultUsage = input.DefaultUsage;
            entity.DefaultIsPreAllocatable = input.DefaultIsPreAllocatable;
            await SyncDefaultsAsync(id, input.DefaultAttributeIds);
            await uow.SaveChangesAsync();
        });

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var entity = Guard.NotFound(await spaceTypeRepository.GetByIdAsync(id), "Alan türü bulunamadı.");
        entity.IsActive = !entity.IsActive;
        await uow.SaveChangesAsync();
        return entity.IsActive;
    }

    public Task<SpaceTypeDeleteResult> DeleteAsync(int id)
        => uow.ExecuteInTransactionAsync(async _ =>
        {
            var entity = Guard.NotFound(await spaceTypeRepository.GetByIdAsync(id), "Alan türü bulunamadı.");
            if (await spaceRepository.IsSpaceTypeUsedAsync(id))
            {
                entity.IsActive = false;
                await uow.SaveChangesAsync();
                return SpaceTypeDeleteResult.Deactivated;
            }

            foreach (var item in await spaceTypeRepository.GetDefaultsAsync(id))
            {
                item.IsDeleted = true;
                item.IsActive = false;
            }
            entity.IsDeleted = true;
            entity.IsActive = false;
            await uow.SaveChangesAsync();
            return SpaceTypeDeleteResult.Deleted;
        });

    private async Task<(string Code, string Name)> ValidateAsync(SpaceTypeInput input, int? excludeId)
    {
        var code = input.Code?.Trim() ?? string.Empty;
        var name = input.Name?.Trim() ?? string.Empty;
        Guard.InvalidField(code.Length is 0 or > 50, CodeField, "Kod zorunludur ve en fazla 50 karakter olabilir.");
        Guard.InvalidField(name.Length is 0 or > 150, NameField, "Ad zorunludur ve en fazla 150 karakter olabilir.");
        Guard.InvalidField(
            await spaceTypeRepository.CodeExistsAsync(code, excludeId),
            CodeField,
            "Bu kod zaten kullanılıyor.",
            "SpaceType.CodeExists");
        Guard.InvalidField(
            input.DefaultIsPreAllocatable && input.DefaultUsage != SpaceUsage.Rentable,
            nameof(input.DefaultIsPreAllocatable),
            "Ön tahsise açık varsayılanı yalnız kiralanabilir kullanımda seçilebilir.");
        return (code, name);
    }

    private async Task SyncDefaultsAsync(int spaceTypeId, IReadOnlyList<int>? attributeIds)
    {
        var wanted = (attributeIds ?? []).Distinct().ToList();
        var definitions = await attributeRepository.GetByIdsAsync(wanted);
        Guard.InvalidField(
            definitions.Count != wanted.Count,
            DefaultAttributesField,
            "Seçilen özelliklerden biri bulunamadı.");

        var existing = await spaceTypeRepository.GetDefaultsAsync(spaceTypeId);
        foreach (var item in existing.Where(e => !wanted.Contains(e.AttributeDefinitionId)))
        {
            item.IsDeleted = true;
            item.IsActive = false;
        }
        foreach (var attributeId in wanted.Where(w => existing.All(e => e.AttributeDefinitionId != w)))
            spaceTypeRepository.AddDefault(new SpaceTypeDefaultAttribute
            {
                SpaceTypeId = spaceTypeId,
                AttributeDefinitionId = attributeId
            });
    }
}
