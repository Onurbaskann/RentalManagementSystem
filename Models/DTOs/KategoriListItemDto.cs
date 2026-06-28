namespace KiraTakip.Models.Dtos;

public class KategoriListItemDto
{
    public int Id { get; set; }
    public KategoriTipi Tipi { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public int Sira { get; set; }
    public bool Aktif { get; set; }
}
