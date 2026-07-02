using KiraTakip.Models;

namespace KiraTakip.Models.Dtos;

public class BorcTipiListItemDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public ChargeTypeBehavior Davranis { get; set; }
    public int Sira { get; set; }
    public bool Sistem { get; set; }
    public bool Aktif { get; set; }
}
