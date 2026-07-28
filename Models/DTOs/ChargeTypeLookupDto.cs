namespace KiraTakip.Models.Dtos;

public class ChargeTypeLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ChargeTypeBehavior Behavior { get; set; }
}
