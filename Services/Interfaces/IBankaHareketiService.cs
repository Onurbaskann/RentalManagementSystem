using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Interfaces;

public interface IBankaHareketiService
{
    Task<(int Adet, Guid BatchId)> ImportAsync(Stream dosya, string bankaKodu, string userId);
    Task<List<BankaHareketi>> GetAllAsync(BankaEslesmeDurumu? durum = null);
    Task<PagedResult<BankaHareketi>> GetPagedAsync(TableQuery q);
    Task<BankaHareketi?> GetByIdAsync(int id);
    Task EslestirAsync(int odemeId, int bankaHareketiId, string userId);
    Task EslesmeCozAsync(int eslesmeId);
    Task<List<KiraOdeme>> GetOdemeAdaylariAsync(int bankaHareketiId, string? userId = null);
    Task<List<BankaHareketi>> GetHareketAdaylariAsync(int odemeId);
}
