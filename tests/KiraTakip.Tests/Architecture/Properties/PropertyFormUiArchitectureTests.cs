namespace KiraTakip.Tests;

public class PropertyFormUiArchitectureTests
{
    private static readonly string ViewsRoot = FindViewsRoot();
    private static readonly string CssPath = FindCssPath();

    [Theory]
    [InlineData("Create.cshtml")]
    [InlineData("Edit.cshtml")]
    public void PropertyForm_ShouldKeepSaveAndAddUnitActionsReachable(string fileName)
    {
        var view = File.ReadAllText(Path.Combine(ViewsRoot, fileName));

        Assert.Contains("property-form-actions", view);
        Assert.Contains("data-property-sticky-add-unit", view);
        Assert.Contains("x-show=\"unitStructure === '2'\"", view);
        Assert.Contains("type=\"submit\"", view);
    }

    [Theory]
    [InlineData("Create.cshtml")]
    [InlineData("Edit.cshtml")]
    public void AddUnit_ShouldNavigateToAndFocusTheNewUnit(string fileName)
    {
        var view = File.ReadAllText(Path.Combine(ViewsRoot, fileName));

        Assert.Contains("data-unit-card", view);
        Assert.Contains("this.focusNewUnit();", view);
        Assert.Contains("scrollIntoView", view);
        Assert.Contains(".UnitNo", view);
        Assert.Contains("preventScroll: true", view);
    }

    [Fact]
    public void PropertyActions_ShouldBeViewportFixedAndReserveFormSpace()
    {
        var css = File.ReadAllText(CssPath);

        Assert.Contains(".property-form { padding-bottom:", css);
        Assert.Contains(".property-form-actions", css);
        Assert.Contains("position: fixed", css);
        Assert.Contains("left: calc(var(--sidebar-width)", css);
    }

    private static string FindViewsRoot()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "KiraTakip.Web", "Views", "Property");
                if (Directory.Exists(candidate)) return candidate;
                var directCandidate = Path.Combine(directory.FullName, "Views", "Property");
                if (Directory.Exists(directCandidate)) return directCandidate;
                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Views/Property proje yolu bulunamadı.");
    }

    private static string FindCssPath()
    {
        var directory = new DirectoryInfo(ViewsRoot);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "wwwroot", "css", "app.css");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException("wwwroot/css/app.css proje yolu bulunamadı.");
    }
}
