namespace KiraTakip.Models.Entities;

public class BelgeIcerik
{
    public int BelgeId { get; set; }
    public byte[] Icerik { get; set; } = Array.Empty<byte>();

    public Belge Belge { get; set; } = null!;
}
