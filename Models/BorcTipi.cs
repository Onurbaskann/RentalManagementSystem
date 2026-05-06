namespace KiraTakip.Models;

public class BorcTipi
{
    public int Id { get; set; }
    public string Ad { get; set; } = "";
    public string Kod { get; set; } = "";
    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
    public bool TekSeferlikMi { get; set; }
}
