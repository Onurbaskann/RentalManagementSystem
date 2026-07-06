using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces
{
    public interface IPropertyPricingService
    {
        Task<TasinmazFiyatMatrisiViewModel> GetMatrisiAsync(int propertyId, int page = 1, int pageSize = 10);
        Task SaveMatrisiAsync(int propertyId, TasinmazFiyatMatrisiViewModel model, string userId);
    }
}
