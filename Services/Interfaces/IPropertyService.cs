using System.Collections.Generic;
using System.Threading.Tasks;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IPropertyService
{
    Task<List<TasinmazListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null);
    Task<TasinmazDetayDto?> GetByIdAsync(int id);
    Task<Property> CreateAsync(Property t, List<BirimInputViewModel>? birimler = null, List<RezervasyonAlaniInputViewModel>? rezervasyonAlanlari = null);
    Task UpdateAsync(Property t);
    Task<TasinmazDuzenleViewModel?> GetForEditAsync(int id);
    Task UpdateWithChildrenAsync(TasinmazDuzenleViewModel vm);
    Task<List<BirimLookupDto>> GetBosBirimlerAsync(IReadOnlyList<int>? tasinmazIds = null);
}
