using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.UnitType;

namespace KiraTakip.Services.Interfaces.Properties;

public interface IUnitTypeService
{
    Task<List<UnitTypeListItemDto>> GetListAsync();
    Task<PagedResult<UnitTypeListItemDto>> GetPagedListAsync(TableQuery query);
    Task<int> GetNextSortOrderAsync();
    Task<List<UnitTypeChargeTypeCandidateDto>> GetChargeTypeCandidatesAsync();
    Task<UnitTypeDetailDto?> GetByIdAsync(GetUnitTypeByIdInput input);
    Task CreateAsync(CreateUnitTypeInput input);
    Task UpdateAsync(EditUnitTypeInput input);
    Task<bool> ToggleStatusAsync(ToggleUnitTypeStatusInput input);
}
