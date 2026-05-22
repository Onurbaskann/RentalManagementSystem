using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IOdemeService
{
    Task<List<OdemeListItemDto>> GetAllAsync(int? tahakkukId = null, string? userId = null);
    Task<PagedResult<OdemeListItemDto>> GetPagedAsync(TableQuery q, int? tahakkukId = null, string? userId = null);
    Task<OdemeDetayDto?> GetByIdAsync(int id);
    Task<KiraOdeme> EkleAsync(KiraOdeme odeme);
    Task<bool> OnaylaAsync(int id, string onaylayanUserId);
    Task<bool> ReddetAsync(int id, string neden);
}
