using Microsoft.AspNetCore.Mvc;

namespace KiraTakip.Models.ViewModels;

public class PaymentPortalRequestViewModel
{
    [FromQuery(Name = "t")]
    public string Token { get; set; } = string.Empty;
}
