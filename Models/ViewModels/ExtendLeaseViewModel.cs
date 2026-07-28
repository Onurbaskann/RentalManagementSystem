using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class ExtendLeaseViewModel
{
    public int LeaseId { get; set; }
    public DateTime NewEndDate { get; set; }
    public bool ApplyInflation { get; set; }
    public decimal? InflationRate { get; set; }
    public bool ApplyVat { get; set; }
    public decimal? VatRate { get; set; }
    public string? Description { get; set; }
    public bool UpdateRate { get; set; }
    public List<LeaseLineItemInputDto> LeaseLineItems { get; set; } = [];
}
