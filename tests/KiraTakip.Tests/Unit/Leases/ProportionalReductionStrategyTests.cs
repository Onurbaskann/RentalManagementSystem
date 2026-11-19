using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Services.Properties;

namespace KiraTakip.Tests;

public class ProportionalReductionStrategyTests
{
    private static readonly DateTime Today = new(2026, 6, 1);
    private static readonly DateTime Far = new(2027, 6, 1);
    private const decimal Minimum = 4m;

    private readonly ProportionalReductionStrategy strategy = new();

    private static AllocationSuggestionRow Manual(int id, decimal rented, decimal area, DateTime? start = null, DateTime? end = null)
        => new(id, rented, area, LeaseAreaMode.Manual, start ?? Today, end ?? Far);

    private static AllocationSuggestionRow Auto(int id, decimal rented, decimal area, DateTime? start = null, DateTime? end = null)
        => new(id, rented, area, LeaseAreaMode.Automatic, start ?? Today, end ?? Far);

    private static AllocationSuggestionContext Context(
        decimal unitArea,
        int capacity,
        IReadOnlyList<AllocationSuggestionRow> rows,
        Dictionary<int, decimal>? overrides = null)
        => new(
            Math.Floor(unitArea / capacity * 100m) / 100m,
            unitArea,
            capacity,
            Minimum,
            Today,
            rows,
            overrides ?? []);

    private static List<AllocationSuggestionRow> ExampleRows()
    {
        var rows = new List<AllocationSuggestionRow>
        {
            Manual(1, 10m, 9m),
            Manual(2, 10m, 7m)
        };
        rows.AddRange(Enumerable.Range(3, 8).Select(id => Auto(id, 6m, 5m)));
        return rows;
    }

    private static decimal Area(AllocationSuggestionResult result, int id)
        => result.Suggestions.Single(item => item.LeaseId == id).AllocatedArea;

    private static decimal Deficit(AllocationSuggestionContext context, AllocationSuggestionResult result, DateTime day)
    {
        var active = context.Rows.Where(row => row.StartDate.Date <= day && day <= row.EndDate.Date).ToList();
        var total = active.Sum(row => Area(result, row.LeaseId))
            + Math.Max(0, context.Capacity - active.Count) * context.EffectiveNorm;
        return total - context.UnitArea;
    }

    [Fact]
    public void Suggest_ShouldReduceManualAreasProportionally_ForExampleScenario()
    {
        var context = Context(70m, 14, ExampleRows());

        var result = strategy.Suggest(context);

        Assert.Equal(5.25m, Area(result, 1));
        Assert.Equal(4.75m, Area(result, 2));
        Assert.Empty(result.Shortfalls);
        Assert.All(result.Suggestions.Where(item => item.LeaseId > 2), item =>
        {
            Assert.Equal(5m, item.AllocatedArea);
            Assert.Equal(LeaseAreaMode.Automatic, item.Mode);
        });
        Assert.All(result.Suggestions.Where(item => item.LeaseId <= 2), item => Assert.Equal(LeaseAreaMode.Manual, item.Mode));
    }

    [Fact]
    public void Suggest_ShouldChangeNothing_WhenNoDeficit()
    {
        var context = Context(100m, 14, ExampleRows());

        var result = strategy.Suggest(context);

        Assert.Equal(9m, Area(result, 1));
        Assert.Equal(7m, Area(result, 2));
        Assert.Empty(result.Shortfalls);
    }

    [Fact]
    public void Suggest_ShouldNotTouchOverriddenRows()
    {
        var context = Context(70m, 14, ExampleRows(), new Dictionary<int, decimal> { [1] = 9m });

        var result = strategy.Suggest(context);

        Assert.Equal(9m, Area(result, 1));
        Assert.Equal(Minimum, Area(result, 2));
        var shortfall = Assert.Single(result.Shortfalls);
        Assert.Equal(Today, shortfall.Day);
        Assert.Equal(3m, shortfall.MissingArea);
    }

    [Fact]
    public void Suggest_ShouldNotTouchAutomaticRows()
    {
        var rows = new List<AllocationSuggestionRow>
        {
            Auto(1, 12m, 10m),
            Manual(2, 12m, 12m)
        };
        var context = Context(30m, 3, rows);

        var result = strategy.Suggest(context);

        Assert.Equal(10m, Area(result, 1));
        Assert.Equal(LeaseAreaMode.Automatic, result.Suggestions.Single(item => item.LeaseId == 1).Mode);
        Assert.Equal(10m, Area(result, 2));
        Assert.Empty(result.Shortfalls);
    }

    [Fact]
    public void Suggest_ShouldUseMaxAlpha_WhenManualSetsDifferAcrossDays()
    {
        var later = Today.AddDays(20);
        var rows = new List<AllocationSuggestionRow>
        {
            Manual(1, 12m, 12m, Today, Today.AddDays(9)),
            Manual(2, 12m, 12m),
            Manual(3, 14m, 14m, later, Far)
        };
        var context = Context(30m, 3, rows);

        var result = strategy.Suggest(context);

        Assert.Equal(10m, Area(result, 1));
        Assert.Equal(9.33m, Area(result, 2));
        Assert.Equal(10.66m, Area(result, 3));
        Assert.Empty(result.Shortfalls);
        Assert.True(Deficit(context, result, Today) <= 0);
        Assert.True(Deficit(context, result, later) <= 0);
    }

    [Fact]
    public void Suggest_ShouldRespectBoundsAndTarget_AfterRounding()
    {
        var rows = new List<AllocationSuggestionRow>
        {
            Manual(1, 12m, 12m),
            Manual(2, 12m, 11.5m),
            Auto(3, 20m, 10m)
        };
        var context = Context(30m, 3, rows);

        var result = strategy.Suggest(context);

        Assert.Equal(10.19m, Area(result, 1));
        Assert.Equal(9.8m, Area(result, 2));
        Assert.True(Deficit(context, result, Today) <= 0);
        foreach (var row in rows.Where(row => row.CurrentMode == LeaseAreaMode.Manual))
        {
            var area = Area(result, row.LeaseId);
            Assert.True(area >= Minimum);
            Assert.True(area <= row.CurrentAllocatedArea);
        }
    }

    [Fact]
    public void Suggest_ShouldClampToMinimum_AndReportShortfall_WhenTargetUnreachable()
    {
        var rows = new List<AllocationSuggestionRow>
        {
            Manual(1, 8m, 8m),
            Auto(2, 8m, 5m)
        };
        var context = Context(10m, 2, rows, new Dictionary<int, decimal> { [2] = 8m });

        var result = strategy.Suggest(context);

        Assert.Equal(Minimum, Area(result, 1));
        Assert.Equal(8m, Area(result, 2));
        var shortfall = Assert.Single(result.Shortfalls);
        Assert.Equal(Today, shortfall.Day);
        Assert.Equal(2m, shortfall.MissingArea);
    }

    [Fact]
    public void DefaultStrategy_ShouldKeepBehavior_AndReturnNoShortfalls()
    {
        var rows = new List<AllocationSuggestionRow>
        {
            Auto(1, 10m, 6m),
            Manual(2, 10m, 9m),
            Manual(3, 10m, 7m),
            new(4, null, 3m, null, Today, Far)
        };
        var context = Context(70m, 14, rows, new Dictionary<int, decimal> { [3] = 6m });

        var result = new DefaultAllocationSuggestionStrategy().Suggest(context);

        Assert.Empty(result.Shortfalls);
        Assert.Equal(5m, Area(result, 1));
        Assert.Equal(LeaseAreaMode.Automatic, result.Suggestions[0].Mode);
        Assert.Equal(9m, Area(result, 2));
        Assert.Equal(6m, Area(result, 3));
        Assert.Equal(LeaseAreaMode.Manual, result.Suggestions[2].Mode);
        Assert.Equal(3m, Area(result, 4));
        Assert.Null(result.Suggestions[3].Mode);
    }
}
