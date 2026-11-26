using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;

namespace KiraTakip.Tests;

internal static class SpaceTestSeed
{
    public static async Task<SpaceType> AddTypeAsync(ApplicationDbContext ctx)
    {
        var s = Guid.NewGuid().ToString("N")[..8];
        var type = new SpaceType { Code = $"ST_{s}", Name = $"Test Alan Türü {s}" };
        ctx.SpaceTypes.Add(type);
        await ctx.SaveChangesAsync();
        return type;
    }

    public static Task<Space> AddRootAsync(ApplicationDbContext ctx, string name, decimal area = 100m)
        => AddAsync(ctx, null, name, area);

    public static Task<Space> AddChildAsync(ApplicationDbContext ctx, Space parent, string name, decimal area = 50m)
        => AddAsync(ctx, parent, name, area);

    public static async Task<SpaceRate> AddRateAsync(ApplicationDbContext ctx, Space space, int categoryId,
        int chargeTypeId, decimal value, CalculationMethod method = CalculationMethod.Fixed, decimal kdv = 0m,
        bool isActive = true)
    {
        var rate = new SpaceRate
        {
            SpaceId = space.Id, TenantCategoryId = categoryId, ChargeTypeId = chargeTypeId,
            CalculationMethod = method, UnitValue = value, KdvRate = kdv, IsActive = isActive
        };
        ctx.SpaceRates.Add(rate);
        await ctx.SaveChangesAsync();
        return rate;
    }

    private static async Task<Space> AddAsync(ApplicationDbContext ctx, Space? parent, string name, decimal area)
    {
        var type = await AddTypeAsync(ctx);
        var space = new Space
        {
            ParentId = parent?.Id, SpaceTypeId = type.Id, Name = name,
            Code = $"S_{Guid.NewGuid():N}"[..20], Area = area, Depth = parent == null ? 0 : parent.Depth + 1
        };
        ctx.Spaces.Add(space);
        await ctx.SaveChangesAsync();
        space.Path = SpaceTreePolicy.BuildPath(parent?.Path, space.Id);
        await ctx.SaveChangesAsync();
        return space;
    }
}
