using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class OdemeHareketSecViewModel
{
    public OdemeDetayDto Odeme { get; set; } = null!;
    public List<BankaHareketiListItemDto> HareketAdaylari { get; set; } = [];
}
