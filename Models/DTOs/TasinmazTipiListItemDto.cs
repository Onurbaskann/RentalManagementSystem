namespace KiraTakip.Models.Dtos;

public class TasinmazTipiListItemDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public int Sira { get; set; }
    public bool Aktif { get; set; }
    public bool TekParcaDestekli { get; set; }
    public bool BirimBazliDestekli { get; set; }
}
