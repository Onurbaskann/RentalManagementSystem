using KiraTakip.Models;

namespace KiraTakip.Services.Interfaces;

public interface IBirimService
{
    Task<List<Birim>> GetByTasinmazIdAsync(int tasinmazId);
    Task<Birim?> GetByIdAsync(int id);
    Task CreateAsync(Birim b);
    Task UpdateAsync(Birim b);
}
