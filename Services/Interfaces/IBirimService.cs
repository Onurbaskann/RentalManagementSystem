using System.Collections.Generic;
using System.Threading.Tasks;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IBirimService
{
    Task<List<BirimListItemDto>> GetByTasinmazIdAsync(int tasinmazId);
    Task<BirimDetayDto?> GetByIdAsync(int id);
    Task CreateAsync(Birim b);
    Task UpdateAsync(Birim b);
}
