using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("BelgeIcerikleri")]
public class DocumentContent
{
    [Column("BelgeId")]
    public int DocumentId { get; set; }
    
    [Column("Icerik")]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public Document Document { get; set; } = null!;
}
