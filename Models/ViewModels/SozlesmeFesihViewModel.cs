namespace KiraTakip.Models.ViewModels;

public class SozlesmeFesihViewModel
{
    public int SozlesmeId { get; set; }
    public DateTime FesihTarihi { get; set; } = DateTime.Today;
    public string FesihNedeni { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
}
