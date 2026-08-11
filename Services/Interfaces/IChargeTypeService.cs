using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.ChargeType;

namespace KiraTakip.Services.Interfaces;

public interface IChargeTypeService
{
    Task<List<ChargeTypeListItemDto>> GetListAsync();
    Task<PagedResult<ChargeTypeListItemDto>> GetPagedListAsync(TableQuery query);
    Task<KiraTakip.Models.Entities.ChargeType?> GetByIdAsync(int id);
    Task<int> GetNextSortOrderAsync();
    Task CreateAsync(CreateInput input);
    Task UpdateAsync(int id, EditInput input);
    Task<bool> ToggleStatusAsync(int id);
    Task ChangeSortOrderAsync(int id, int newSortOrder);
}
