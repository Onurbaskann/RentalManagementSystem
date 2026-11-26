using System.Globalization;
using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Services.Interfaces.Spaces;

namespace KiraTakip.Services.Spaces;

public class SpaceService(
    ISpaceRepository spaceRepository,
    ISpaceTypeRepository spaceTypeRepository,
    ISpaceLockRepository lockRepository,
    IUnitOfWork uow,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : ISpaceService
{
    private const string SystemUserId = "system";
    private const string CodeField = "Code";
    private const string NameField = "Name";
    private const string AddressField = "Address";
    private const string SpaceTypeField = "SpaceTypeId";

    public Task<List<SpaceTreeRow>> GetRootsAsync() => spaceRepository.GetRootRowsAsync();

    public async Task<SpaceTreeNodeDto> GetTreeAsync(int spaceId)
    {
        var space = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
        var rows = await spaceRepository.GetTreeRowsAsync(space.Path);
        var nodes = rows.ToDictionary(r => r.Id, ToNode);
        foreach (var row in rows)
        {
            if (row.Id != spaceId && row.ParentId is int parentId && nodes.TryGetValue(parentId, out var parent))
                parent.Children.Add(nodes[row.Id]);
        }
        return nodes[spaceId];
    }

    public async Task<SpaceDetailDto> GetDetailAsync(int spaceId)
    {
        var s = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
        return new SpaceDetailDto(
            s.Id, s.ParentId, s.SpaceTypeId, s.Code, s.Name, s.SortOrder, s.Description,
            s.Area, s.Usage, s.IsPreAllocatable, s.IsShared, s.IsActive, s.Depth);
    }

    public Task<List<SpaceHistoryDto>> GetHistoryAsync(int spaceId) => spaceRepository.GetHistoryAsync(spaceId);

    public Task<SpaceSaveResult> CreateAsync(CreateSpaceInput input)
        => uow.ExecuteInTransactionAsync(_ => CreateCoreAsync(input));

    public Task<SpaceSaveResult> UpdateAsync(UpdateSpaceInput input)
        => uow.ExecuteInTransactionAsync(_ => UpdateCoreAsync(input));

    public async Task<SpaceAddressInput?> GetAddressAsync(int spaceId)
    {
        var address = (await spaceRepository.GetLinkedDefinitionsAsync(spaceId)).Address;
        return address == null ? null : new SpaceAddressInput(address.City, address.District, address.Neighborhood, address.Address);
    }

    public Task UpdateAddressAsync(int spaceId, SpaceAddressInput address)
        => uow.ExecuteInTransactionAsync(_ => UpdateAddressCoreAsync(spaceId, address));

    public Task DeactivateAsync(int spaceId)
        => uow.ExecuteInTransactionAsync(_ => DeactivateCoreAsync(spaceId));

    public Task ActivateAsync(int spaceId)
        => uow.ExecuteInTransactionAsync(_ => ActivateCoreAsync(spaceId));

    public Task DeleteAsync(int spaceId)
        => uow.ExecuteInTransactionAsync(_ => DeleteCoreAsync(spaceId));

    public async Task<SpaceDeletePreviewDto> GetDeletePreviewAsync(int spaceId)
    {
        Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
        var violations = await GetDeleteViolationsAsync(spaceId);
        var links = await spaceRepository.GetLinkedDefinitionsAsync(spaceId);
        return new SpaceDeletePreviewDto(
            violations,
            links.Address != null,
            links.AttributeValues.Select(v => v.AttributeDefinition.Name).ToList(),
            links.Rates.Select(r => $"{r.TenantCategory.Name} / {r.ChargeType.Name}").ToList());
    }

    private async Task<SpaceSaveResult> CreateCoreAsync(CreateSpaceInput input)
    {
        var code = input.Code?.Trim() ?? string.Empty;
        var name = input.Name?.Trim() ?? string.Empty;
        Guard.InvalidField(code.Length is 0 or > 50, CodeField, "Kod zorunludur ve en fazla 50 karakter olabilir.");
        Guard.InvalidField(name.Length is 0 or > 200, NameField, "Ad zorunludur ve en fazla 200 karakter olabilir.");

        var type = Guard.NotFound(await spaceTypeRepository.GetByIdAsync(input.SpaceTypeId), "Alan türü bulunamadı.");
        Guard.InvalidField(!type.IsActive, SpaceTypeField, "Pasif alan türüyle alan eklenemez.", "Space.TypeInactive");

        Space? parent = null;
        if (input.ParentId is int parentId)
        {
            var parentHint = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(parentId), "Üst alan bulunamadı.");
            await lockRepository.AcquireRootLockAsync([SpaceTreePolicy.GetRootId(parentHint.Path)]);

            parent = Guard.NotFound(await spaceRepository.GetByIdAsync(parentId), "Üst alan bulunamadı.");
            Guard.Conflict(!parent.IsActive, "Pasif alanın altına alan eklenemez.", "Space.ParentInactive");
            Guard.Conflict(!SpaceTreePolicy.CanAddChild(parent.Depth),
                $"Alan ağacı en fazla {SpaceTreePolicy.MaxDepth + 1} seviye olabilir.", "Space.MaxDepth");
        }
        else
        {
            var address = input.Address;
            Guard.InvalidField(
                address == null
                || string.IsNullOrWhiteSpace(address.City)
                || string.IsNullOrWhiteSpace(address.District)
                || string.IsNullOrWhiteSpace(address.Neighborhood)
                || string.IsNullOrWhiteSpace(address.Address),
                AddressField,
                "Kök alan için adres zorunludur.",
                "Space.AddressRequired");
        }

        Guard.InvalidField(
            await spaceRepository.CodeExistsAsync(input.ParentId, code),
            CodeField,
            "Bu kod aynı üst alan altında zaten kullanılıyor.",
            "Space.CodeExists");

        var requested = new SpaceState(input.ParentId, input.Usage, input.Area, input.IsPreAllocatable, input.IsShared);
        var hasReservableRelative = parent != null
            && input.Usage == SpaceUsage.Reservable
            && await spaceRepository.AnyReservableAsync(
                SpaceTreePolicy.GetAncestorIds(parent.Path).Append(parent.Id));
        ThrowIfViolations(SpaceChangePolicy.GetUpdateViolations(
            requested with { Usage = SpaceUsage.NotRentable },
            requested,
            SpaceOpenRecords.None,
            hasReservableRelative));

        var space = new Space
        {
            ParentId = input.ParentId,
            SpaceTypeId = type.Id,
            Code = code,
            Name = name,
            SortOrder = input.SortOrder,
            Description = input.Description?.Trim(),
            Area = input.Area,
            Usage = input.Usage,
            IsPreAllocatable = input.IsPreAllocatable,
            IsShared = input.IsShared,
            Depth = parent == null ? 0 : parent.Depth + 1,
            Path = $"/{Guid.NewGuid():N}/"
        };
        await spaceRepository.AddAsync(space);
        await uow.SaveChangesAsync();

        space.Path = SpaceTreePolicy.BuildPath(parent?.Path, space.Id);
        AddHistory(space.Id, SpaceHistoryField.Created, null, space.Name);
        if (parent == null)
        {
            var address = input.Address!;
            spaceRepository.AddAddress(new SpaceAddress
            {
                SpaceId = space.Id,
                City = address.City.Trim(),
                District = address.District.Trim(),
                Neighborhood = address.Neighborhood.Trim(),
                Address = address.Address.Trim()
            });
        }
        await uow.SaveChangesAsync();

        return new SpaceSaveResult(space.Id, await GetWarningsAsync(space));
    }

    private async Task<SpaceSaveResult> UpdateCoreAsync(UpdateSpaceInput input)
    {
        var name = input.Name?.Trim() ?? string.Empty;
        Guard.InvalidField(name.Length is 0 or > 200, NameField, "Ad zorunludur ve en fazla 200 karakter olabilir.");

        var hint = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(input.Id), "Alan bulunamadı.");
        await lockRepository.AcquireRootLockAsync([SpaceTreePolicy.GetRootId(hint.Path)]);
        var space = Guard.NotFound(await spaceRepository.GetByIdAsync(input.Id), "Alan bulunamadı.");

        var typeChanged = space.SpaceTypeId != input.SpaceTypeId;
        var oldType = Guard.NotFound(await spaceTypeRepository.GetByIdAsync(space.SpaceTypeId), "Alan türü bulunamadı.");
        var newType = oldType;
        if (typeChanged)
        {
            newType = Guard.NotFound(await spaceTypeRepository.GetByIdAsync(input.SpaceTypeId), "Alan türü bulunamadı.");
            Guard.InvalidField(!newType.IsActive, SpaceTypeField, "Pasif alan türü seçilemez.", "Space.TypeInactive");
        }

        var current = new SpaceState(space.ParentId, space.Usage, space.Area, space.IsPreAllocatable, space.IsShared);
        var requested = new SpaceState(input.ParentId, input.Usage, input.Area, input.IsPreAllocatable, input.IsShared);
        var hasReservableRelative = input.Usage == SpaceUsage.Reservable
            && space.Usage != SpaceUsage.Reservable
            && (await spaceRepository.AnyReservableAsync(SpaceTreePolicy.GetAncestorIds(space.Path))
                || await spaceRepository.HasReservableDescendantAsync(space.Path));
        ThrowIfViolations(SpaceChangePolicy.GetUpdateViolations(
            current,
            requested,
            await spaceRepository.GetOpenRecordsAsync(space.Id),
            hasReservableRelative));

        if (typeChanged)
            AddHistory(space.Id, SpaceHistoryField.Type, oldType.Name, newType.Name);
        if (space.Name != name)
            AddHistory(space.Id, SpaceHistoryField.Name, space.Name, name);
        if (space.Area != input.Area)
            AddHistory(space.Id, SpaceHistoryField.Area, Format(space.Area), Format(input.Area));
        if (space.Usage != input.Usage)
            AddHistory(space.Id, SpaceHistoryField.Usage, space.Usage.ToString(), input.Usage.ToString());
        if (space.IsPreAllocatable != input.IsPreAllocatable)
            AddHistory(space.Id, SpaceHistoryField.PreAllocatable, Format(space.IsPreAllocatable), Format(input.IsPreAllocatable));
        if (space.IsShared != input.IsShared)
            AddHistory(space.Id, SpaceHistoryField.Shared, Format(space.IsShared), Format(input.IsShared));

        space.SpaceTypeId = newType.Id;
        space.Name = name;
        space.SortOrder = input.SortOrder;
        space.Description = input.Description?.Trim();
        space.Area = input.Area;
        space.Usage = input.Usage;
        space.IsPreAllocatable = input.IsPreAllocatable;
        space.IsShared = input.IsShared;
        await uow.SaveChangesAsync();

        return new SpaceSaveResult(space.Id, await GetWarningsAsync(space));
    }

    private async Task UpdateAddressCoreAsync(int spaceId, SpaceAddressInput input)
    {
        Guard.InvalidField(
            string.IsNullOrWhiteSpace(input.City)
            || string.IsNullOrWhiteSpace(input.District)
            || string.IsNullOrWhiteSpace(input.Neighborhood)
            || string.IsNullOrWhiteSpace(input.Address),
            AddressField,
            "Kök alan için adres zorunludur.",
            "Space.AddressRequired");

        var hint = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
        Guard.Conflict(hint.ParentId != null, "Adres yalnız kök alanda düzenlenebilir.", "Space.AddressRootOnly");
        await lockRepository.AcquireRootLockAsync([spaceId]);

        var address = Guard.NotFound((await spaceRepository.GetLinkedDefinitionsAsync(spaceId)).Address, "Alan adresi bulunamadı.");
        address.City = input.City.Trim();
        address.District = input.District.Trim();
        address.Neighborhood = input.Neighborhood.Trim();
        address.Address = input.Address.Trim();
        await uow.SaveChangesAsync();
    }

    private async Task DeactivateCoreAsync(int spaceId)
    {
        var hint = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
        await lockRepository.AcquireRootLockAsync([SpaceTreePolicy.GetRootId(hint.Path)]);
        var space = Guard.NotFound(await spaceRepository.GetByIdAsync(spaceId), "Alan bulunamadı.");
        Guard.Conflict(!space.IsActive, "Alan zaten pasif.", "Space.AlreadyInactive");

        var openRecords = await spaceRepository.GetOpenRecordsAsync(spaceId);
        ThrowIfConflicts(SpaceChangePolicy.GetDeactivateViolations(openRecords));

        foreach (var item in (await spaceRepository.GetSubtreeForUpdateAsync(space.Path)).Where(s => s.IsActive))
        {
            item.IsActive = false;
            AddHistory(item.Id, SpaceHistoryField.Active, Format(true), Format(false));
        }
        await uow.SaveChangesAsync();
    }

    private async Task ActivateCoreAsync(int spaceId)
    {
        var hint = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
        await lockRepository.AcquireRootLockAsync([SpaceTreePolicy.GetRootId(hint.Path)]);
        var space = Guard.NotFound(await spaceRepository.GetByIdAsync(spaceId), "Alan bulunamadı.");
        Guard.Conflict(space.IsActive, "Alan zaten aktif.", "Space.AlreadyActive");

        var parentIsActive = true;
        if (space.ParentId is int parentId)
            parentIsActive = (await spaceRepository.GetByIdAsync(parentId))?.IsActive ?? false;
        ThrowIfConflicts(SpaceChangePolicy.GetActivateViolations(space.ParentId.HasValue, parentIsActive));

        space.IsActive = true;
        AddHistory(space.Id, SpaceHistoryField.Active, Format(false), Format(true));
        await uow.SaveChangesAsync();
    }

    private async Task DeleteCoreAsync(int spaceId)
    {
        var hint = Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
        await lockRepository.AcquireRootLockAsync([SpaceTreePolicy.GetRootId(hint.Path)]);
        var space = Guard.NotFound(await spaceRepository.GetByIdAsync(spaceId), "Alan bulunamadı.");

        ThrowIfConflicts(await GetDeleteViolationsAsync(spaceId));

        var links = await spaceRepository.GetLinkedDefinitionsAsync(spaceId);
        if (links.Address != null)
            SoftDelete(links.Address);
        links.AttributeValues.ForEach(SoftDelete);
        links.Rates.ForEach(SoftDelete);
        SoftDelete(space);
        await uow.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<SpaceChangeViolation>> GetDeleteViolationsAsync(int spaceId)
        => SpaceChangePolicy.GetDeleteViolations(
            await spaceRepository.GetOpenRecordsAsync(spaceId),
            await spaceRepository.HasHistoryAsync(spaceId),
            await spaceRepository.HasChildrenAsync(spaceId));

    private async Task<IReadOnlyList<SpaceChangeWarning>> GetWarningsAsync(Space space)
    {
        var warnings = new HashSet<SpaceChangeWarning>();
        warnings.UnionWith(SpaceChangePolicy.GetAreaWarnings(
            space.Area,
            (await spaceRepository.GetChildrenAsync(space.Id)).Select(c => c.Area)));

        if (space.ParentId is int parentId)
        {
            var parent = await spaceRepository.GetNoTrackingAsync(parentId);
            if (parent != null)
                warnings.UnionWith(SpaceChangePolicy.GetAreaWarnings(
                    parent.Area,
                    (await spaceRepository.GetChildrenAsync(parentId)).Select(c => c.Area)));
        }
        return warnings.ToList();
    }

    private void AddHistory(int spaceId, SpaceHistoryField field, string? oldValue, string? newValue)
        => spaceRepository.AddHistory(new SpaceHistory
        {
            SpaceId = spaceId,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = timeProvider.GetUtcNow().UtcDateTime,
            UserId = currentUser.UserId ?? SystemUserId
        });

    private static void ThrowIfViolations(IReadOnlyList<SpaceChangeViolation> violations)
    {
        if (violations.Count == 0) return;
        var first = violations[0];
        throw new BusinessValidationException(
            SpaceViolationMessages.Field(first),
            string.Join(" ", violations.Select(SpaceViolationMessages.Message)),
            SpaceViolationMessages.Code(first));
    }

    private static void ThrowIfConflicts(IReadOnlyList<SpaceChangeViolation> violations)
    {
        if (violations.Count == 0) return;
        Guard.Conflict(
            true,
            string.Join(" ", violations.Select(SpaceViolationMessages.Message)),
            SpaceViolationMessages.Code(violations[0]));
    }

    private static void SoftDelete(BaseEntity entity)
    {
        entity.IsDeleted = true;
        entity.IsActive = false;
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Format(bool value) => value ? bool.TrueString : bool.FalseString;

    private static SpaceTreeNodeDto ToNode(SpaceTreeRow row) => new()
    {
        Id = row.Id,
        ParentId = row.ParentId,
        Code = row.Code,
        Name = row.Name,
        SpaceTypeName = row.SpaceTypeName,
        Usage = row.Usage,
        Area = row.Area,
        IsActive = row.IsActive,
        Depth = row.Depth
    };
}
