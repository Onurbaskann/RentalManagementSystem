using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public interface IKiraciService
{
    Task<List<Kiraci>> GetAllAsync(string? userId = null);
    Task<Kiraci?> GetByIdAsync(int id);
    Task<Kiraci> CreateAsync(Kiraci k);
    Task UpdateAsync(Kiraci k);
    Task<string> GenerateKiraciNoAsync();
    Task<bool> KiraciNoExistsAsync(string kiraciNo, int? excludeId = null);
}
