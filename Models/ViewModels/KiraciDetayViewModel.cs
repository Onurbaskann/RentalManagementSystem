using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class KiraciDetayViewModel
{
    public KiraciDetayDto Kiraci { get; set; } = null!;
    public List<SozlesmeListItemDto> Sozlesmeler { get; set; } = [];
    public Dictionary<int, decimal?> DepozitoTutarlari { get; set; } = [];
}
