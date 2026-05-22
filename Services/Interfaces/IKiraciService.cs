using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IKiraciService
{
    Task<List<KiraciListItemDto>> GetAllAsync(string? userId = null);
    Task<KiraciDetayDto?> GetDetayAsync(int id);
    Task<Kiraci> CreateAsync(Kiraci k);
    Task UpdateAsync(Kiraci k);
    Task<string> GenerateKiraciNoAsync();
    Task<bool> KiraciNoExistsAsync(string kiraciNo, int? excludeId = null);
}
