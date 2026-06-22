namespace KiraTakip.Models.Entities;

public class BelgeTuru : BaseEntity
{
    public string Kod { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
    public BelgeOwnerTipi HedefEntite { get; set; }
    public bool Zorunlu { get; set; }
    public string IzinVerilenUzantilar { get; set; } = "pdf,jpg,png";
    public int MaxBoyutMb { get; set; } = 5;
    public int? SablonBelgeId { get; set; }
    public int Sira { get; set; }

    public Belge? SablonBelge { get; set; }
}
