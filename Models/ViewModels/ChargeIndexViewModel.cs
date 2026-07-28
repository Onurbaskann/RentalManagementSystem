using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class ChargeIndexViewModel
{
    public PagedResult<ChargeListItemDto> Charges { get; set; } = new();
    public TableQuery Query { get; set; } = new();
    public string Status { get; set; } = "tum";
    public int CancelledCount { get; set; }
    public List<ChargePropertyFilterDto> Properties { get; set; } = [];
    public List<ChargeUnitFilterDto> Units { get; set; } = [];
    public List<ChargeTenantFilterDto> Tenants { get; set; } = [];
    public List<int> AvailableYears { get; set; } = [];
}
