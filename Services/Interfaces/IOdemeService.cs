using KiraTakip.Models;
using KiraTakip.Models.Common;

namespace KiraTakip.Services.Interfaces;

public interface IOdemeService
{
    Task<List<KiraOdeme>> GetAllAsync(int? tahakkukId = null, string? userId = null);
    Task<PagedResult<KiraOdeme>> GetPagedAsync(TableQuery q, int? tahakkukId = null, string? userId = null);
    Task<KiraOdeme?> GetByIdAsync(int id);
    Task<KiraOdeme> EkleAsync(KiraOdeme odeme);
    Task<bool> OnaylaAsync(int id, string onaylayanUserId);
    Task<bool> ReddetAsync(int id, string neden);
}
