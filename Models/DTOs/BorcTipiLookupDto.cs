namespace KiraTakip.Models.Dtos;

public class BorcTipiLookupDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Kod { get; set; } = string.Empty;
    public ChargeTypeBehavior Davranis { get; set; }
}
