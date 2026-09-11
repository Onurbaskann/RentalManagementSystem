using System.Reflection;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Tests;

public class PaymentUiArchitectureTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void PaymentCreateView_RendersLineItemRadioGroup()
    {
        var view = File.ReadAllText(Path.Combine(RepoRoot, "Views", "Payment", "Create.cshtml"));

        Assert.Contains("name=\"ChargeLineItemId\"", view);
        Assert.Contains("x-model.number=\"lineItemId\"", view);
        Assert.Contains("asp-validation-for=\"ChargeLineItemId\"", view);
    }

    [Fact]
    public void PaymentCreateView_DoesNotFallBackToChargeTotal()
    {
        var controller = File.ReadAllText(
            Path.Combine(RepoRoot, "Controllers", "PaymentController.cs"));

        Assert.DoesNotContain("charge.TotalAmount - charge.PaidAmount", controller);
    }

    [Fact]
    public void TenantChargeDetailsView_RendersLineItemSelectionInPaymentModal()
    {
        var view = File.ReadAllText(
            Path.Combine(RepoRoot, "Views", "TenantCharge", "Details.cshtml"));

        Assert.Contains("name=\"ChargeLineItemId\"", view);
        Assert.Contains("tenantKalemSecimi()", view);
    }

    [Fact]
    public void TenantChargeDetailsView_DoesNotExposeStoreInformation()
    {
        var view = File.ReadAllText(
            Path.Combine(RepoRoot, "Views", "TenantCharge", "Details.cshtml"));

        Assert.DoesNotContain("StoreName", view);
        Assert.DoesNotContain("Mağaza", view);
        Assert.DoesNotContain("Merchant", view);
    }

    [Fact]
    public void PaymentDetailsView_ShowsLineItemAndStoreInformation()
    {
        var view = File.ReadAllText(
            Path.Combine(RepoRoot, "Views", "Payment", "Details.cshtml"));

        Assert.Contains("Model.ChargeTypeName", view);
        Assert.Contains("Model.StoreName", view);
    }

    [Fact]
    public void PaymentDetailDto_DoesNotExposeMerchantSecrets()
    {
        var propertyNames = typeof(PaymentDetailDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToList();

        Assert.DoesNotContain(propertyNames, name => name.Contains("Merchant"));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Password"));
    }

    private static string FindRepoRoot()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "KiraTakip.Web", "Views", "Payment");
                if (Directory.Exists(candidate)) return Path.Combine(directory.FullName, "src", "KiraTakip.Web");
                var directCandidate = Path.Combine(directory.FullName, "Views", "Payment");
                if (Directory.Exists(directCandidate)) return directory.FullName;
                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Views/Payment proje yolu bulunamadı.");
    }
}
