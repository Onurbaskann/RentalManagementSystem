namespace KiraTakip.Models.Entities;

public class TasinmazTipi : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public int Sira { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
    public bool TekParcaDestekli { get; set; }
    public bool BirimBazliDestekli { get; set; }
}
