using KiraTakip.Models.Entities;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class DocumentPanelViewModel
{
    public DocumentOwnerType OwnerType { get; set; }
    public int OwnerId { get; set; }
    public List<DocumentType> DocumentTypes { get; set; } = [];
    public List<Document> Documents { get; set; } = [];
    public bool CanEdit { get; set; }
}
