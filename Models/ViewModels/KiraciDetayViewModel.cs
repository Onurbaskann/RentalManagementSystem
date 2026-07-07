using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;

namespace KiraTakip.Models.ViewModels;

public class KiraciDetayViewModel
{
    public KiraciDetayDto Tenant { get; set; } = null!;
    public List<LeaseListItemDto> Leases { get; set; } = [];
    public Dictionary<int, decimal?> DepozitoTutarlari { get; set; } = [];
    public List<Document> Belgeler { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}
