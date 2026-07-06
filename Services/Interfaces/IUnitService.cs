using System.Collections.Generic;
using System.Threading.Tasks;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IUnitService
{
    Task<List<BirimListItemDto>> GetByPropertyIdAsync(int propertyId);
    Task<BirimDetayDto?> GetByIdAsync(int id);
    Task CreateAsync(Unit b);
    Task UpdateAsync(Unit b);
}
