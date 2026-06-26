using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Banka;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class BankaHareketiService : IBankaHareketiService
{
    private readonly IBankaHareketiRepository _repo;
    private readonly IEnumerable<IBankaHareketiParser> _parsers;
    private readonly IUnitOfWork _uow;

    public BankaHareketiService(IBankaHareketiRepository repo, IEnumerable<IBankaHareketiParser> parsers, IUnitOfWork uow)
    {
        _repo = repo;
        _parsers = parsers;
        _uow = uow;
    }

    public async Task<int> ImportAsync(Stream dosya, string bankaKodu)
    {
        var parser = _parsers.FirstOrDefault(p =>
            p.BankaKodu.Equals(bankaKodu, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"'{bankaKodu}' için parser bulunamadı.");

        var hareketler = parser.Parse(dosya).ToList();
        await _repo.AddRangeAsync(hareketler);
        await _uow.SaveChangesAsync();
        return hareketler.Count;
    }

    public Task<List<BankaHareketiListItemDto>> GetAllAsync(BankaEslesmeDurumu? durum = null)
        => _repo.GetListAsync(durum);

    public Task<PagedResult<BankaHareketiListItemDto>> GetPagedAsync(TableQuery q)
        => _repo.GetPagedListAsync(q);

    public Task<BankaHareketiDetayDto?> GetByIdAsync(int id)
        => _repo.GetDetayAsync(id);

    public async Task EslestirAsync(int odemeId, int bankaHareketiId)
    {
        if (await _repo.EslesmeVarMiAsync(odemeId, bankaHareketiId)) return;

        var hareketi = await _repo.GetByIdAsync(bankaHareketiId)
            ?? throw new InvalidOperationException("Banka hareketi bulunamadı.");

        var eslesme = new OdemeBankaEslesme
        {
            TahakkukOdemeId = odemeId,
            BankaHareketiId = bankaHareketiId,
            EslesmeTipi = EslesmeTipi.Manuel,
        };

        hareketi.EslesmeDurumu = BankaEslesmeDurumu.ManuelEslesti;
        await _repo.AddEslesmeAsync(eslesme);
        await _uow.SaveChangesAsync();
    }

    public async Task EslesmeCozAsync(int eslesmeId)
    {
        var eslesme = await _repo.GetEslesmeWithBankaHareketiAsync(eslesmeId);
        if (eslesme == null) return;

        await _repo.RemoveEslesmeAsync(eslesme);

        if (!await _repo.KalanEslesmeVarMiAsync(eslesme.BankaHareketiId, eslesmeId))
            eslesme.BankaHareketi.EslesmeDurumu = BankaEslesmeDurumu.Eslestirilmedi;

        await _uow.SaveChangesAsync();
    }

    public Task<List<OdemeAdayDto>> GetOdemeAdaylariAsync(int bankaHareketiId, IReadOnlyList<int>? tasinmazIds = null)
        => _repo.GetOdemeAdaylariAsync(bankaHareketiId, tasinmazIds);

    public Task<List<BankaHareketiListItemDto>> GetHareketAdaylariAsync(int odemeId)
        => _repo.GetHareketAdaylariAsync(odemeId);
}
