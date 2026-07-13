using System.Collections.Generic;
using System.Threading.Tasks;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IPropertyService
{
    Task<List<TasinmazListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null);
    Task<PropertyDetailDto?> GetByIdAsync(int id);
    Task<Property> CreateAsync(Property t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null, int? kompleUnitTypeId = null);
    Task UpdateAsync(Property t);
    Task<TasinmazDuzenleViewModel?> GetForEditAsync(int id);
    Task<bool> CanChangeUnitStructureAsync(int propertyId);
    Task UpdateWithChildrenAsync(TasinmazDuzenleViewModel vm);
    Task<List<UnitLookupDto>> GetBosBirimlerAsync(IReadOnlyList<int>? tasinmazIds = null);
}
