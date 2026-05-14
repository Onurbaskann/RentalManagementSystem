using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models;

public class BirimTuru
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Ad { get; set; } = string.Empty;
    [Required, MaxLength(20)]
    public string Kod { get; set; } = string.Empty;
    public bool Aktif { get; set; } = true;
    public bool KiralanabilirMi { get; set; } = true;
    public bool RezervasyonYapilabilirMi { get; set; } = false;
    public int? BorcTipiId { get; set; }
    public BorcTipi? BorcTipi { get; set; }

    public int Sira { get; set; }
    public DateTime OlusturmaTarihi { get; set; }
}
