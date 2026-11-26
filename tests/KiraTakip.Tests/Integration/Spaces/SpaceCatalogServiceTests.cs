using KiraTakip.Models.Entities;
using KiraTakip.Data;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static KiraTakip.Tests.Integration.Spaces.SpaceTestData;

namespace KiraTakip.Tests.Integration.Spaces;

[Collection("Database collection")]
public class SpaceCatalogServiceTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly IDbContextTransaction _tx;
    private readonly SpaceTestServices _svc;
    private readonly string _uid = NewUid();

    public SpaceCatalogServiceTests(DatabaseFixture fixture)
    {
        _ctx = fixture.CreateContext();
        _tx = _ctx.Database.BeginTransaction();
        _svc = new SpaceTestServices(_ctx);
    }

    public void Dispose()
    {
        _tx.Rollback();
        _tx.Dispose();
        _ctx.Dispose();
    }

    private Task<int> NewAttributeAsync(string suffix, AttributeDataType type, string? options = null)
        => _svc.Attributes.CreateAsync(new SpaceAttributeInput($"ZA{_uid}{suffix}", $"Ozellik {suffix}", type, options));

    private async Task<int> NewRootAsync()
    {
        var typeId = await _svc.Types.CreateAsync(TypeInput($"ZT{_uid}"));
        return (await _svc.Spaces.CreateAsync(Root(typeId, $"Z{_uid}R"))).Id;
    }

    [Fact]
    public async Task SpaceType_RejectsDuplicateCode_AndStoresDefaults()
    {
        var attributeId = await NewAttributeAsync("a", AttributeDataType.Text);
        var id = await _svc.Types.CreateAsync(TypeInput($"ZT{_uid}") with { DefaultAttributeIds = [attributeId] });

        await AssertBusinessErrorAsync(() => _svc.Types.CreateAsync(TypeInput($"ZT{_uid}")), "SpaceType.CodeExists");

        var listed = (await _svc.Types.GetListAsync()).Single(t => t.Id == id);
        Assert.Equal([attributeId], listed.DefaultAttributeIds);
        var suggestions = await _svc.Attributes.GetTypeSuggestionsAsync(id);
        Assert.Single(suggestions);
    }

    [Fact]
    public async Task SpaceType_Delete_SoftDeletesWhenUnused_DeactivatesWhenUsed()
    {
        var unusedId = await _svc.Types.CreateAsync(TypeInput($"ZT{_uid}u"));
        Assert.Equal(SpaceTypeDeleteResult.Deleted, await _svc.Types.DeleteAsync(unusedId));
        Assert.False(await _ctx.SpaceTypes.AnyAsync(t => t.Id == unusedId));

        var usedId = await _svc.Types.CreateAsync(TypeInput($"ZT{_uid}x"));
        await _svc.Spaces.CreateAsync(Root(usedId, $"Z{_uid}R"));
        Assert.Equal(SpaceTypeDeleteResult.Deactivated, await _svc.Types.DeleteAsync(usedId));
        var used = await _ctx.SpaceTypes.SingleAsync(t => t.Id == usedId);
        Assert.False(used.IsActive);
        Assert.False(used.IsDeleted);

        Assert.True(await _svc.Types.ToggleStatusAsync(usedId));
    }

    [Fact]
    public async Task SpaceType_Inactive_CannotBeUsedForNewSpace()
    {
        var typeId = await _svc.Types.CreateAsync(TypeInput($"ZT{_uid}"));
        await _svc.Types.ToggleStatusAsync(typeId);

        await AssertBusinessErrorAsync(
            () => _svc.Spaces.CreateAsync(Root(typeId, $"Z{_uid}R")),
            "Space.TypeInactive");
    }

    [Fact]
    public async Task Attribute_ValueValidation_ByDataType()
    {
        var rootId = await NewRootAsync();
        var list = await NewAttributeAsync("l", AttributeDataType.List, "Kuzey\nGuney");
        var number = await NewAttributeAsync("n", AttributeDataType.Number);
        var flag = await NewAttributeAsync("b", AttributeDataType.Boolean);
        var text = await NewAttributeAsync("t", AttributeDataType.Text);

        await Assert.ThrowsAnyAsync<Exception>(() => _svc.Attributes.SaveValuesAsync(rootId, [new(list, "Dogu")]));
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.Attributes.SaveValuesAsync(rootId, [new(number, "abc")]));
        await Assert.ThrowsAnyAsync<Exception>(() => _svc.Attributes.SaveValuesAsync(rootId, [new(flag, "belki")]));

        await _svc.Attributes.SaveValuesAsync(rootId,
            [new(list, "Kuzey"), new(number, "12.5"), new(flag, "true"), new(text, "serbest")]);
        var saved = await _svc.Attributes.GetValuesAsync(rootId);
        Assert.Equal(4, saved.Count);
        Assert.Equal("12.5", saved.Single(v => v.AttributeDefinitionId == number).Value);

        await _svc.Attributes.SaveValuesAsync(rootId, [new(list, "Guney"), new(number, "")]);
        saved = await _svc.Attributes.GetValuesAsync(rootId);
        Assert.Single(saved);
        Assert.Equal("Guney", saved[0].Value);
    }

    [Fact]
    public async Task Attribute_ListRequiresOptions_AndUsedAttributeCannotBeDeleted()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => NewAttributeAsync("l", AttributeDataType.List, "  "));

        var rootId = await NewRootAsync();
        var id = await NewAttributeAsync("t", AttributeDataType.Text);
        await _svc.Attributes.SaveValuesAsync(rootId, [new(id, "x")]);

        await AssertBusinessErrorAsync(() => _svc.Attributes.DeleteAsync(id), "SpaceAttribute.InUse");
        Assert.False(await _svc.Attributes.ToggleStatusAsync(id));
    }

    [Fact]
    public async Task Rate_UpsertsByKey_ValidatesValues_AndSoftDeletes()
    {
        var rootId = await NewRootAsync();
        var category = new Category { Type = CategoryType.Tenant, Name = $"K{_uid}", Code = $"ZK{_uid}", Order = 1 };
        var chargeType = new ChargeType { Name = $"B{_uid}", Code = $"ZB{_uid}", SortOrder = 1 };
        _ctx.AddRange(category, chargeType);
        await _ctx.SaveChangesAsync();

        var first = await _svc.Rates.SaveAsync(new(rootId, category.Id, chargeType.Id, CalculationMethod.M2, 25m, 20m));
        var second = await _svc.Rates.SaveAsync(new(rootId, category.Id, chargeType.Id, CalculationMethod.Fixed, 30m, 10m));
        Assert.Equal(first, second);
        var list = await _svc.Rates.GetListAsync(rootId);
        Assert.Equal(30m, Assert.Single(list).UnitValue);

        await AssertBusinessErrorAsync(
            () => _svc.Rates.SaveAsync(new(rootId, category.Id, chargeType.Id, CalculationMethod.Fixed, -1m, 10m)),
            "SPACE_RATE_INVALID_VALUE");

        await _svc.Rates.DeleteAsync(first);
        Assert.Empty(await _svc.Rates.GetListAsync(rootId));
        Assert.True((await _ctx.SpaceRates.IgnoreQueryFilters().SingleAsync(r => r.Id == first)).IsDeleted);
    }
}
