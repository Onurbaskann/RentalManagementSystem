using KiraTakip.Models;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class RateResolverService : IRateResolverService
{
    private readonly ISozlesmeTarifeRepository _sozlesmeTarifeRepo;
    private readonly IBirimTarifeRepository _birimTarifeRepo;
    private readonly ITasinmazTarifeRepository _tasinmazTarifeRepo;
    private readonly IGenelTarifeRepository _genelTarifeRepo;
    private readonly ISozlesmeRepository _sozlesmeRepo;
    private readonly IBirimRepository _birimRepo;
    private readonly IKiraciRepository _kiraciRepo;

    public RateResolverService(
        ISozlesmeTarifeRepository sozlesmeTarifeRepo,
        IBirimTarifeRepository birimTarifeRepo,
        ITasinmazTarifeRepository tasinmazTarifeRepo,
        IGenelTarifeRepository genelTarifeRepo,
        ISozlesmeRepository sozlesmeRepo,
        IBirimRepository birimRepo,
        IKiraciRepository kiraciRepo)
    {
        _sozlesmeTarifeRepo = sozlesmeTarifeRepo;
        _birimTarifeRepo = birimTarifeRepo;
        _tasinmazTarifeRepo = tasinmazTarifeRepo;
        _genelTarifeRepo = genelTarifeRepo;
        _sozlesmeRepo = sozlesmeRepo;
        _birimRepo = birimRepo;
        _kiraciRepo = kiraciRepo;
    }

    public async Task<RateSnapshot?> ResolveAsync(int? sozlesmeId, int? kiraciId, int birimId, int borcTipiId, DateTime donem)
    {
        if (sozlesmeId.HasValue)
        {
            var sozRate = await _sozlesmeTarifeRepo.GetRateAsync(sozlesmeId.Value, borcTipiId);
            if (sozRate != null)
                return Wrap(sozRate, LineItemSourceType.LeaseRateOverride);
        }

        int? tasinmazId = null;
        int? kategoriId = null;

        if (sozlesmeId.HasValue)
        {
            var info = await _sozlesmeRepo.GetTasinmazVeKategoriAsync(sozlesmeId.Value);
            if (info != null)
            {
                tasinmazId = info.Value.TasinmazId;
                kategoriId = info.Value.KategoriId;
            }
        }
        else if (kiraciId.HasValue)
        {
            tasinmazId = await _birimRepo.GetTasinmazIdAsync(birimId);
            kategoriId = await _kiraciRepo.GetKategoriIdAsync(kiraciId.Value);
        }

        if (kategoriId.HasValue)
        {
            var birimRate = await _birimTarifeRepo.GetRateAsync(birimId, kategoriId.Value, borcTipiId);
            if (birimRate != null)
                return Wrap(birimRate, LineItemSourceType.UnitRateOverride);
        }

        if (tasinmazId.HasValue && kategoriId.HasValue)
        {
            var fiyatMatrisi = await _tasinmazTarifeRepo.GetRateAsync(tasinmazId.Value, kategoriId.Value, borcTipiId);
            if (fiyatMatrisi != null)
                return Wrap(fiyatMatrisi, LineItemSourceType.PropertyRateOverride);
        }

        if (!kategoriId.HasValue) return null;

        var kalem = await _genelTarifeRepo.GetRateAsync(kategoriId.Value, borcTipiId, donem.Year);
        if (kalem == null) return null;

        return Wrap(kalem, LineItemSourceType.RateSchedule);
    }

    private static RateSnapshot Wrap(Models.Dtos.RateValueDto v, LineItemSourceType kaynak)
        => new RateSnapshot
        {
            CalculationMethod = v.CalculationMethod,
            BirimDeger = v.BirimDeger,
            KdvOrani = v.KdvOrani,
            KaynakTipi = kaynak
        };
}
