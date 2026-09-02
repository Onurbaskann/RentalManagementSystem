using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace KiraTakip.Models.ViewModels;

public class TenantChargeIndexViewModel
{
    public PagedResult<ChargeListItemDto> Charges { get; set; } = new();
    public TenantChargeQueryViewModel Query { get; set; } = new();
    public string Status { get; set; } = "tum";
    public decimal TotalChargeAmount { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal RemainingDebtAmount { get; set; }
    public decimal OverdueRemainingAmount { get; set; }
    public List<TenantChargeUnitOptionDto> Units { get; set; } = [];
    public List<int> AvailableYears { get; set; } = [];
    public bool CanReportPayment { get; set; }
}

public class TenantChargeDetailsViewModel
{
    public ChargeDetailDto Charge { get; set; } = null!;
    public Dictionary<int, List<Document>> PaymentDocuments { get; set; } = [];
    public bool CanReportPayment { get; set; }
}

public class TenantChargeQueryViewModel
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? Q { get; set; }
    public string? Status { get; set; }
    public int? UnitId { get; set; }
    public string? Source { get; set; }
    public int? Year { get; set; }

    public Dictionary<string, string?> ToQueryDict()
    {
        var values = new Dictionary<string, string?>();
        if (Size != 10) values["size"] = Size.ToString();
        if (!string.IsNullOrWhiteSpace(Q)) values["q"] = Q;
        if (!string.IsNullOrWhiteSpace(Status) && Status != "tum") values["status"] = Status;
        if (UnitId.HasValue) values["unitId"] = UnitId.ToString();
        if (!string.IsNullOrWhiteSpace(Source)) values["source"] = Source;
        if (Year.HasValue) values["year"] = Year.ToString();

        return values;
    }
}

public class TenantChargePaymentFormViewModel
{
    public int ChargeId { get; set; }
    public int? ChargeLineItemId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public PaymentChannel PaymentChannel { get; set; } = PaymentChannel.BankTransfer;
    public string? Description { get; set; }
    public IFormFile? Receipt { get; set; }
}
