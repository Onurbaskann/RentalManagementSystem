using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using Microsoft.EntityFrameworkCore;
using static KiraTakip.Tests.Integration.Spaces.SpaceTestData;

namespace KiraTakip.Tests.Integration.Spaces;

[Collection("Database collection")]
public class SpaceConcurrencyTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private readonly string _prefix = $"ZC{NewUid()}";
    private int _counter;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var ctx = fixture.CreateContext();
        var spaceIds = await ctx.Spaces.IgnoreQueryFilters()
            .Where(s => s.Code.StartsWith(_prefix)).Select(s => s.Id).ToListAsync();
        await ctx.SpaceHistories.IgnoreQueryFilters().Where(x => spaceIds.Contains(x.SpaceId)).ExecuteDeleteAsync();
        await ctx.SpaceAddresses.IgnoreQueryFilters().Where(x => spaceIds.Contains(x.SpaceId)).ExecuteDeleteAsync();
        await ctx.Spaces.IgnoreQueryFilters().Where(s => s.Code.StartsWith(_prefix)).ExecuteDeleteAsync();
        await ctx.SpaceTypes.IgnoreQueryFilters().Where(t => t.Code.StartsWith(_prefix)).ExecuteDeleteAsync();
    }

    private string NextCode() => $"{_prefix}{Interlocked.Increment(ref _counter)}";

    private async Task<(int TypeId, int RootId)> NewRootAsync()
    {
        await using var ctx = fixture.CreateContext();
        var svc = new SpaceTestServices(ctx);
        var typeId = await svc.Types.CreateAsync(TypeInput(NextCode()));
        var rootId = (await svc.Spaces.CreateAsync(Root(typeId, NextCode()))).Id;
        return (typeId, rootId);
    }

    private async Task<int> AddChildAsync(int parentId, int typeId)
    {
        await using var ctx = fixture.CreateContext();
        return (await new SpaceTestServices(ctx).Spaces.CreateAsync(Child(parentId, typeId, NextCode()))).Id;
    }

    [Fact]
    public async Task ParallelChildCreation_UnderSameRoot_BothSucceedWithCorrectPaths()
    {
        var (typeId, rootId) = await NewRootAsync();

        var ids = await Task.WhenAll(AddChildAsync(rootId, typeId), AddChildAsync(rootId, typeId));

        await using var ctx = fixture.CreateContext();
        foreach (var id in ids)
        {
            var space = await ctx.Spaces.AsNoTracking().SingleAsync(s => s.Id == id);
            Assert.Equal($"/{rootId}/{id}/", space.Path);
            Assert.Equal(1, space.Depth);
        }
        Assert.NotEqual(ids[0], ids[1]);
    }

    [Fact]
    public async Task ParallelChildCreationAndParentDeactivation_NeverLeavesActiveChildUnderInactiveParent()
    {
        for (var round = 0; round < 5; round++)
        {
            var (typeId, rootId) = await NewRootAsync();
            var parentId = await AddChildAsync(rootId, typeId);

            var create = Task.Run(async () =>
            {
                try { await AddChildAsync(parentId, typeId); }
                catch (BusinessException) { }
            });
            var deactivate = Task.Run(async () =>
            {
                await using var ctx = fixture.CreateContext();
                await new SpaceTestServices(ctx).Spaces.DeactivateAsync(parentId);
            });
            await Task.WhenAll(create, deactivate);

            await using var verify = fixture.CreateContext();
            var parent = await verify.Spaces.AsNoTracking().SingleAsync(s => s.Id == parentId);
            Assert.False(parent.IsActive);
            Assert.False(await verify.Spaces.AnyAsync(s => s.Path.StartsWith(parent.Path) && s.IsActive));
        }
    }
}
