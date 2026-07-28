using System.Collections.Generic;
using System.Threading.Tasks;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IUnitService
{
    Task<List<UnitListItemDto>> GetByPropertyIdAsync(int propertyId);
    Task<UnitDetailDto?> GetByIdAsync(int id);
    Task<List<UnitListItemDto>> GetReservableUnitsAsync();
    Task CreateAsync(Unit b);
    Task UpdateAsync(Unit b);
}
