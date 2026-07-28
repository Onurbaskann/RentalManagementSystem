using KiraTakip.Models.Dtos.DocumentType;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IDocumentTypeService
{
    Task<List<DocumentType>> GetListAsync();
    Task<int> GetMaxSortOrderAsync();
    Task CreateAsync(CreateInput input);
    Task<DocumentType?> GetByIdAsync(int id);
    Task UpdateAsync(int id, EditInput input);
    Task<bool> ToggleStatusAsync(int id);
    Task DeleteAsync(int id);
}
