using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Common;

namespace KiraTakip.Models.ViewModels;

public class TenantIndexViewModel
{
    public PagedResult<TenantListItemDto> Tenants { get; set; } = new();
    public TableQuery Query { get; set; } = new();
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
}

public class TenantFormViewModel
{
    public int? Id { get; set; }
    public string TenantNo { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? TradeRegistryNo { get; set; }
    public string? TaxNo { get; set; }
    public string? TaxOffice { get; set; }
    public string? MersisNo { get; set; }
    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Address { get; set; }
    public int? TenantCategoryId { get; set; }
    public int? SectorId { get; set; }

    public string? InitialRepresentativeEmail { get; set; }

    public string? InitialRepresentativeFullName { get; set; }
    public List<CategoryListItemDto> TenantCategories { get; set; } = [];
    public List<CategoryListItemDto> Sectors { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}

public class TenantDetailsViewModel
{
    public TenantDetailsDto Tenant { get; set; } = null!;
    public List<LeaseListItemDto> Leases { get; set; } = [];
    public Dictionary<int, decimal?> DepositAmounts { get; set; } = [];
    public List<Document> Documents { get; set; } = [];
    public List<DocumentType> DocumentTypes { get; set; } = [];
}
