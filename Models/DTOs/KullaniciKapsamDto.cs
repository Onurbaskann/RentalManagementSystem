namespace KiraTakip.Models.Dtos;

public class KullaniciKapsamDto
{
    public bool GlobalErisim { get; set; }
    public List<int> TasinmazIds { get; set; } = new();
}
