namespace KiraTakip.Models;

public class EnumDegeri
{
    public int Id { get; set; }
    public string EnumAdi { get; set; } = null!;
    public int Deger { get; set; }
    public string Ad { get; set; } = null!;
    public string? Aciklama { get; set; }
}
