using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IUnitTypeService
{
    Task<List<UnitTypeListItemDto>> GetListAsync();
    Task<int> GetNextSortOrderAsync();
    Task<List<UnitTypeChargeTypeCandidateDto>> GetChargeTypeCandidatesAsync();
    Task<UnitTypeDetailDto?> GetByIdAsync(GetUnitTypeByIdInput input);
    Task CreateAsync(CreateUnitTypeInput input);
    Task UpdateAsync(EditUnitTypeInput input);
    Task<bool> ToggleStatusAsync(ToggleUnitTypeStatusInput input);
}
