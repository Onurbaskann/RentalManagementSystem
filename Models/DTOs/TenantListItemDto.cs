namespace KiraTakip.Models.Dtos;

public class TenantListItemDto
{
    public int Id { get; set; }
    public string TenantNo { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? TaxNo { get; set; }
    public string? TenantCategoryName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public int ActiveLeaseCount { get; set; }
}
