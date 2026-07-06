namespace KiraTakip.Models.Entities;

public class Kategori : BaseEntity
{
    public KategoriTipi Tipi { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public int Sira { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}

public enum KategoriTipi
{
    Tenant = 1,
    Sektor = 2
}
