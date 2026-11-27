using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;

namespace KiraTakip.Services.Interfaces.Spaces;

public interface ISpaceService
{
    Task<List<SpaceTreeRow>> GetRootsAsync(SpaceAccessScope scope);
    Task<List<SpaceOptionDto>> GetActiveOptionsAsync();
    Task EnsureInScopeAsync(int spaceId, SpaceAccessScope scope);
    Task<SpaceTreeNodeDto> GetTreeAsync(int spaceId, SpaceAccessScope scope);
    Task<SpaceDetailDto> GetDetailAsync(int spaceId, SpaceAccessScope scope);
    Task<List<SpaceHistoryDto>> GetHistoryAsync(int spaceId, SpaceAccessScope scope);
    Task<SpaceSaveResult> CreateAsync(CreateSpaceInput input, SpaceAccessScope scope);
    Task<SpaceSaveResult> UpdateAsync(UpdateSpaceInput input, SpaceAccessScope scope);
    Task<SpaceAddressInput?> GetAddressAsync(int spaceId, SpaceAccessScope scope);
    Task UpdateAddressAsync(int spaceId, SpaceAddressInput address, SpaceAccessScope scope);
    Task DeactivateAsync(int spaceId, SpaceAccessScope scope);
    Task ActivateAsync(int spaceId, SpaceAccessScope scope);
    Task<SpaceDeletePreviewDto> GetDeletePreviewAsync(int spaceId, SpaceAccessScope scope);
    Task DeleteAsync(int spaceId, SpaceAccessScope scope);
}

public interface ISpaceTypeService
{
    Task<List<SpaceTypeDto>> GetListAsync();
    Task<int> CreateAsync(SpaceTypeInput input);
    Task UpdateAsync(int id, SpaceTypeInput input);
    Task<bool> ToggleStatusAsync(int id);
    Task<SpaceTypeDeleteResult> DeleteAsync(int id);
}

public interface ISpaceAttributeService
{
    Task<List<SpaceAttributeDto>> GetListAsync();
    Task<int> CreateAsync(SpaceAttributeInput input);
    Task UpdateAsync(int id, SpaceAttributeInput input);
    Task<bool> ToggleStatusAsync(int id);
    Task DeleteAsync(int id);
    Task<List<SpaceAttributeValueDto>> GetValuesAsync(int spaceId);
    Task SaveValuesAsync(int spaceId, IReadOnlyList<SpaceAttributeValueInput> values);
    Task<List<SpaceAttributeDto>> GetTypeSuggestionsAsync(int spaceTypeId);
}

public interface ISpaceRateService
{
    Task<List<SpaceRateDto>> GetListAsync(int spaceId);
    Task<int> SaveAsync(SpaceRateInput input);
    Task DeleteAsync(int rateId);
}
