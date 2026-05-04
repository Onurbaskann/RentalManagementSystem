using KiraTakip.Models;
using Microsoft.AspNetCore.Http;

namespace KiraTakip.Services.Interfaces;

public interface IDekontService
{
    Task<Dekont> EkleAsync(int odemeId, IFormFile dosya, string userId);
    Task<List<Dekont>> GetByOdemeIdAsync(int odemeId);
    Task<Dekont?> GetByIdAsync(int id);
    Task SilAsync(int id);
    string GetTamYol(Dekont dekont);
}
