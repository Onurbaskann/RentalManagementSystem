namespace KiraTakip.Models;

public class TasinmazTipi
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public int Sira { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    public ICollection<TasinmazTipiKiralamaSekli> KiralamaSekilleri { get; set; } = new List<TasinmazTipiKiralamaSekli>();
}
