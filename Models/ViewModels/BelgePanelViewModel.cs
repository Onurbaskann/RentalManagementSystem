using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class BelgePanelViewModel
{
    public BelgeOwnerTipi OwnerType { get; set; }
    public int OwnerId { get; set; }
    public List<BelgeTuru> BelgeTurleri { get; set; } = [];
    public List<Belge> Belgeler { get; set; } = [];
    public bool CanEdit { get; set; }
}
