using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class BankaEslesmeSecViewModel
{
    public BankaHareketiDetayDto BankaHareketi { get; set; } = null!;
    public List<OdemeAdayDto> OdemeAdaylari { get; set; } = [];
}
