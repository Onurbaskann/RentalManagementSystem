namespace KiraTakip.Models.Entities;

public class EnumDegeri : BaseEntity
{
    public string EnumAdi { get; set; } = null!;
    public int Deger { get; set; }
    public string Ad { get; set; } = null!;
    public string? Aciklama { get; set; }
}
