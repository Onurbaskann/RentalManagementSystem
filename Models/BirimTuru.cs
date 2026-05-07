namespace KiraTakip.Models;

public class BirimTuru
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public bool KiralanabilirMi { get; set; } = true;
    public bool RezervasyonYapilabilirMi { get; set; } = false;
    public int Sira { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}
