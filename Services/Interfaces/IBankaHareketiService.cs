using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IBankaHareketiService
{
    Task<(int Adet, Guid BatchId)> ImportAsync(Stream dosya, string bankaKodu, string userId);
    Task<List<BankaHareketiListItemDto>> GetAllAsync(BankaEslesmeDurumu? durum = null);
    Task<PagedResult<BankaHareketiListItemDto>> GetPagedAsync(TableQuery q);
    Task<BankaHareketiDetayDto?> GetByIdAsync(int id);
    Task EslestirAsync(int odemeId, int bankaHareketiId, string userId);
    Task EslesmeCozAsync(int eslesmeId);
    Task<List<OdemeAdayDto>> GetOdemeAdaylariAsync(int bankaHareketiId, IReadOnlyList<int>? tasinmazIds = null);
    Task<List<BankaHareketiListItemDto>> GetHareketAdaylariAsync(int odemeId);
}
