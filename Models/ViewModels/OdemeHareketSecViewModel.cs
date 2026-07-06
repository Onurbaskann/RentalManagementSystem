using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class OdemeHareketSecViewModel
{
    public OdemeDetayDto Payment { get; set; } = null!;
    public List<BankaHareketiListItemDto> HareketAdaylari { get; set; } = [];
}
