using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Repositories.Interfaces.Common;

namespace KiraTakip.Repositories.Interfaces.Spaces;

public interface ISpaceRepository : IRepositoryBase<Space>
{
    Task<Space?> GetNoTrackingAsync(int id);
    Task<List<SpaceTreeRow>> GetRootRowsAsync();
    Task<List<SpaceTreeRow>> GetTreeRowsAsync(string rootPath);
    Task<List<Space>> GetAncestorsAsync(string path);
    Task<List<Space>> GetSubtreeForUpdateAsync(string path);
    Task<List<Space>> GetChildrenAsync(int parentId);
    Task<bool> CodeExistsAsync(int? parentId, string code, int? excludeId = null);
    Task<IReadOnlyList<string>> GetPathsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
    Task<bool> HasChildrenAsync(int spaceId);
    Task<bool> AnyReservableAsync(IEnumerable<int> spaceIds);
    Task<bool> HasReservableDescendantAsync(string path);
    Task<bool> IsSpaceTypeUsedAsync(int spaceTypeId);
    Task<SpaceOpenRecords> GetOpenRecordsAsync(int spaceId);
    Task<bool> HasHistoryAsync(int spaceId);
    void AddHistory(SpaceHistory history);
    Task<List<SpaceHistoryDto>> GetHistoryAsync(int spaceId);
    void AddAddress(SpaceAddress address);
    Task<SpaceLinkedDefinitions> GetLinkedDefinitionsAsync(int spaceId);
}
