using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class BankaEslesmeSecViewModel
{
    public BankaHareketiDetayDto BankTransaction { get; set; } = null!;
    public List<OdemeAdayDto> OdemeAdaylari { get; set; } = [];
}
