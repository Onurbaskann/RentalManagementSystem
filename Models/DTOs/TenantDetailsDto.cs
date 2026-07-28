namespace KiraTakip.Models.Dtos;

public class TenantDetailsDto
{
    public int Id { get; set; }
    public int? TenantCategoryId { get; set; }
    public string? TenantCategoryName { get; set; }
    public int? SectorId { get; set; }
    public string? SectorName { get; set; }
    public string TenantNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TradeRegistryNo { get; set; }
    public string? TaxNo { get; set; }
    public string? TaxOffice { get; set; }
    public string? MersisNo { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime RegistrationDate { get; set; }

    public string DisplayName => Name;
}
