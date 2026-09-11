using KiraTakip.Models.Dtos;

namespace KiraTakip.Web.Models.ViewModels;

public class PropertyDetailsViewModel
{
    public PropertyDetailDto Property { get; set; } = null!;
    public PropertyPricingMatrixViewModel PricingMatrix { get; set; } = null!;
}
