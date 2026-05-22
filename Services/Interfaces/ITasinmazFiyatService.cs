using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces
{
    public interface ITasinmazFiyatService
    {
        Task<TasinmazFiyatMatrisiViewModel> GetMatrisiAsync(int tasinmazId, int page = 1, int pageSize = 10);
        Task SaveMatrisiAsync(int tasinmazId, TasinmazFiyatMatrisiViewModel model, string userId);
    }
}
