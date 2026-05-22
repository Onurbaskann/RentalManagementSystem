using KiraTakip.Models.Dtos;

namespace KiraTakip.Repositories.Interfaces;

public interface IBirimRepository : IBaseRepository<Birim>
{
    Task<List<BirimListItemDto>> GetByTasinmazIdAsync(int tasinmazId);
    Task<BirimDetayDto?> GetDetayAsync(int id);
    Task<List<BirimListItemDto>> GetRezervasyonBirimleriAsync();
    Task<int?> GetTasinmazIdAsync(int birimId);
}
