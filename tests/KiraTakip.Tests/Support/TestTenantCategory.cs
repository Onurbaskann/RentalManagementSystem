using KiraTakip.Data;
using KiraTakip.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

internal static class TestTenantCategory
{
    public static Category New()
        => new()
        {
            Type = CategoryType.Tenant,
            Name = "Test Kategori",
            Code = $"TK_{Guid.NewGuid():N}"[..20],
            IsActive = true,
            Order = 1
        };

    public static Task<List<int>> IdsOfAsync(ApplicationDbContext ctx, IEnumerable<int> tenantIds)
        => ctx.Tenants.IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .Select(t => t.TenantCategoryId)
            .ToListAsync();

    public static async Task DeleteAsync(ApplicationDbContext ctx, List<int> categoryIds)
    {
        ctx.Kategoriler.RemoveRange(ctx.Kategoriler.IgnoreQueryFilters().Where(c => categoryIds.Contains(c.Id)));
        await ctx.SaveChangesAsync();
    }
}
