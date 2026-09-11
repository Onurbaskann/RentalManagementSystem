namespace KiraTakip.Tests;

public class LeaseReviewUiArchitectureTests
{
    private static readonly string ViewsRoot = FindViewsRoot();

    [Fact]
    public void CreateAndDraft_UseSharedLeaseForm()
    {
        var create = Read("Create.cshtml");
        var draft = Read("Draft.cshtml");

        Assert.Contains("_LeaseForm", create);
        Assert.Contains("_LeaseForm", draft);
        Assert.Contains("Yeni Sözleşme Başvurusu", create);
        Assert.Contains("Onaya Gönder", create);
        Assert.Contains("tahakkuk oluşmaz", create);
        Assert.Contains("RowVersion", draft);
    }

    [Fact]
    public void Draft_HasStatusPermissionActionsAndSafeTimeline()
    {
        var draft = Read("Draft.cshtml");
        var timeline = Read("_LeaseReviewTimeline.cshtml");

        Assert.Contains("Model.CanApprove", draft);
        Assert.Contains("Model.CanRequestRevision", draft);
        Assert.Contains("Model.CanDelete", draft);
        Assert.Contains("Model.CanEdit", draft);
        Assert.Contains("maxlength=\"1000\"", draft);
        Assert.Contains("required", draft);
        Assert.DoesNotContain("Html.Raw", timeline);
        Assert.Contains("OrderByDescending", timeline);
    }

    [Fact]
    public void Index_HasApplicationFiltersBadgesAndDraftRoute()
    {
        var index = Read("Index.cshtml");

        Assert.Contains("onaybekliyor", index);
        Assert.Contains("revizyon", index);
        Assert.Contains("ONAY BEKLİYOR", index);
        Assert.Contains("REVİZYON İSTENDİ", index);
        Assert.Contains("/Lease/Draft/", index);
        Assert.Contains("?filter=", index);
    }

    private static string Read(string fileName)
        => File.ReadAllText(Path.Combine(ViewsRoot, fileName));

    private static string FindViewsRoot()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "KiraTakip.Web", "Views", "Lease");
                if (Directory.Exists(candidate)) return candidate;
                var directCandidate = Path.Combine(directory.FullName, "Views", "Lease");
                if (Directory.Exists(directCandidate)) return directCandidate;
                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Views/Lease proje yolu bulunamadı.");
    }
}
