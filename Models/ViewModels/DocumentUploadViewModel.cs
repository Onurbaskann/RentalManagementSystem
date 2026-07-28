namespace KiraTakip.Models.ViewModels;

public class DocumentUploadViewModel
{
    public DocumentOwnerType OwnerType { get; set; }
    public int OwnerId { get; set; }
    public int DocumentTypeId { get; set; }
    public IFormFile? File { get; set; }
    public string? Description { get; set; }
}
