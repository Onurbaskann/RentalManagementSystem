using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class KiraciDetayViewModel
{
    public KiraciDetayDto Kiraci { get; set; } = null!;
    public List<SozlesmeListItemDto> Sozlesmeler { get; set; } = [];
    public Dictionary<int, decimal?> DepozitoTutarlari { get; set; } = [];
    public List<Belge> Belgeler { get; set; } = [];
    public List<BelgeTuru> BelgeTurleri { get; set; } = [];
}
