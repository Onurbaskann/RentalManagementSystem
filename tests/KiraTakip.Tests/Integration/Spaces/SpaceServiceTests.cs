using KiraTakip.Models.Entities;
using KiraTakip.Data;
using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static KiraTakip.Tests.Integration.Spaces.SpaceTestData;

namespace KiraTakip.Tests.Integration.Spaces;

[Collection("Database collection")]
public class SpaceServiceTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly IDbContextTransaction _tx;
    private readonly SpaceTestServices _svc;
    private readonly string _uid = NewUid();

    public SpaceServiceTests(DatabaseFixture fixture)
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

    private async Task<int> NewTypeAsync(string suffix = "T")
        => await _svc.Types.CreateAsync(TypeInput($"ZT{_uid}{suffix}", $"Tur {_uid}{suffix}"));

    private string Code(string suffix) => $"Z{_uid}{suffix}";

    private Task<Space> LoadAsync(int id) => _ctx.Spaces.AsNoTracking().SingleAsync(s => s.Id == id);

    private async Task<int> AddChildAsync(int parentId, int typeId, string code, decimal area = 10m, SpaceUsage usage = SpaceUsage.Rentable)
        => (await _svc.Spaces.CreateAsync(Child(parentId, typeId, code, area, usage))).Id;

    [Fact]
    public async Task Create_BuildsPathAndDepth_ForRootAndFourLevels()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R")))).Id;
        var a = await AddChildAsync(root, typeId, Code("A"));
        var b = await AddChildAsync(a, typeId, Code("B"));
        var c = await AddChildAsync(b, typeId, Code("C"));
        var d = await AddChildAsync(c, typeId, Code("D"));

        var rootEntity = await LoadAsync(root);
        Assert.Equal($"/{root}/", rootEntity.Path);
        Assert.Equal(0, rootEntity.Depth);
        var leaf = await LoadAsync(d);
        Assert.Equal($"/{root}/{a}/{b}/{c}/{d}/", leaf.Path);
        Assert.Equal(4, leaf.Depth);
        Assert.Equal(c, leaf.ParentId);

        var tree = await _svc.Spaces.GetTreeAsync(root);
        Assert.Equal(d, tree.Children[0].Children[0].Children[0].Children[0].Id);
        Assert.True(await _ctx.SpaceAddresses.AnyAsync(x => x.SpaceId == root));
    }

    [Fact]
    public async Task UpdateAddress_RootOnly_UpdatesAndRejectsChild()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R")))).Id;
        var child = await AddChildAsync(root, typeId, Code("A"));

        await _svc.Spaces.UpdateAddressAsync(root, new SpaceAddressInput("Izmir", "Konak", "Yeni Mah.", "Yeni Sk. 2"));

        var saved = await _svc.Spaces.GetAddressAsync(root);
        Assert.Equal("Izmir", saved!.City);
        Assert.Equal("Yeni Sk. 2", saved.Address);
        await AssertBusinessErrorAsync(
            () => _svc.Spaces.UpdateAddressAsync(child, Address),
            "Space.AddressRootOnly");
    }

    [Fact]
    public async Task Create_RootWithoutAddress_IsRejected()
    {
        var typeId = await NewTypeAsync();
        await AssertBusinessErrorAsync(
            () => _svc.Spaces.CreateAsync(Root(typeId, Code("R")) with { Address = null }),
            "Space.AddressRequired");
    }

    [Fact]
    public async Task Create_RejectsTwentyFirstLevel()
    {
        var typeId = await NewTypeAsync();
        var current = (await _svc.Spaces.CreateAsync(Root(typeId, Code("L0")))).Id;
        for (var level = 1; level < SpaceTreePolicy.MaxDepth + 1; level++)
            current = await AddChildAsync(current, typeId, Code($"L{level}"));

        Assert.Equal(SpaceTreePolicy.MaxDepth, (await LoadAsync(current)).Depth);
        await AssertBusinessErrorAsync(() => AddChildAsync(current, typeId, Code("L20")), "Space.MaxDepth");
    }

    [Fact]
    public async Task Create_RejectsNestedReservable_UnderAndAboveReservable()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R"), usage: SpaceUsage.Reservable))).Id;
        var rentable = (await _svc.Spaces.CreateAsync(Root(typeId, Code("S")))).Id;
        var rentableChild = await AddChildAsync(rentable, typeId, Code("SC"));

        await AssertBusinessErrorAsync(
            () => AddChildAsync(root, typeId, Code("X"), usage: SpaceUsage.Reservable),
            "Space.NestedReservable");

        var belowReservable = await AddChildAsync(root, typeId, Code("Y"));
        await AssertBusinessErrorAsync(
            async () => await _svc.Spaces.UpdateAsync(Update(await LoadAsync(belowReservable)) with { Usage = SpaceUsage.Reservable }),
            "Space.NestedReservable");

        await AddChildAsync(rentableChild, typeId, Code("Z"), usage: SpaceUsage.Reservable);
        await AssertBusinessErrorAsync(
            async () => await _svc.Spaces.UpdateAsync(Update(await LoadAsync(rentable)) with { Usage = SpaceUsage.Reservable }),
            "Space.NestedReservable");
    }

    [Fact]
    public async Task Update_RejectsParentChange()
    {
        var typeId = await NewTypeAsync();
        var r1 = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R1")))).Id;
        var r2 = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R2")))).Id;
        var child = await AddChildAsync(r1, typeId, Code("C"));

        await AssertBusinessErrorAsync(
            async () => await _svc.Spaces.UpdateAsync(Update(await LoadAsync(child)) with { ParentId = r2 }),
            "Space.ParentChangeNotAllowed");
        await AssertBusinessErrorAsync(
            async () => await _svc.Spaces.UpdateAsync(Update(await LoadAsync(child)) with { ParentId = null }),
            "Space.ParentChangeNotAllowed");
    }

    [Fact]
    public async Task Deactivate_PropagatesToSubtree_NotToParent()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R")))).Id;
        var a = await AddChildAsync(root, typeId, Code("A"));
        var b = await AddChildAsync(a, typeId, Code("B"));
        var sibling = await AddChildAsync(root, typeId, Code("S"));

        await _svc.Spaces.DeactivateAsync(a);

        _ctx.ChangeTracker.Clear();
        Assert.True((await LoadAsync(root)).IsActive);
        Assert.False((await LoadAsync(a)).IsActive);
        Assert.False((await LoadAsync(b)).IsActive);
        Assert.True((await LoadAsync(sibling)).IsActive);
        Assert.Equal(2, await _ctx.SpaceHistories.CountAsync(h => h.Field == SpaceHistoryField.Active && (h.SpaceId == a || h.SpaceId == b)));
    }

    [Fact]
    public async Task Activate_OpensOnlySelectedSpace_AndRejectsUnderInactiveParent()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R")))).Id;
        var a = await AddChildAsync(root, typeId, Code("A"));
        var b = await AddChildAsync(a, typeId, Code("B"));
        await _svc.Spaces.DeactivateAsync(a);

        await AssertBusinessErrorAsync(() => _svc.Spaces.ActivateAsync(b), "Space.ActivateWithInactiveParent");

        await _svc.Spaces.ActivateAsync(a);

        _ctx.ChangeTracker.Clear();
        Assert.True((await LoadAsync(a)).IsActive);
        Assert.False((await LoadAsync(b)).IsActive);
    }

    [Fact]
    public async Task Create_RejectsInactiveParent()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R")))).Id;
        var a = await AddChildAsync(root, typeId, Code("A"));
        await _svc.Spaces.DeactivateAsync(a);

        await AssertBusinessErrorAsync(() => AddChildAsync(a, typeId, Code("B")), "Space.ParentInactive");
    }

    [Fact]
    public async Task Delete_IsRejected_WhenChildrenExist()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R")))).Id;
        await AddChildAsync(root, typeId, Code("A"));

        var preview = await _svc.Spaces.GetDeletePreviewAsync(root);
        Assert.False(preview.CanDelete);
        await AssertBusinessErrorAsync(() => _svc.Spaces.DeleteAsync(root), "Space.DeleteWithChildren");
    }

    [Fact]
    public async Task Delete_SoftDeletesSpaceAndLinkedDefinitions()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R")))).Id;
        var attributeId = await _svc.Attributes.CreateAsync(new SpaceAttributeInput($"ZA{_uid}", "Ozellik", AttributeDataType.Text, null));
        await _svc.Attributes.SaveValuesAsync(root, [new SpaceAttributeValueInput(attributeId, "deger")]);
        var category = new Category { Type = CategoryType.Tenant, Name = $"K{_uid}", Code = $"ZK{_uid}", Order = 1 };
        var chargeType = new ChargeType { Name = $"B{_uid}", Code = $"ZB{_uid}", SortOrder = 1 };
        _ctx.AddRange(category, chargeType);
        await _ctx.SaveChangesAsync();
        await _svc.Rates.SaveAsync(new SpaceRateInput(root, category.Id, chargeType.Id, CalculationMethod.Fixed, 10m, 20m));

        var preview = await _svc.Spaces.GetDeletePreviewAsync(root);
        Assert.True(preview.CanDelete);
        Assert.True(preview.HasAddress);
        Assert.Single(preview.AttributeNames);
        Assert.Single(preview.RateDescriptions);

        await _svc.Spaces.DeleteAsync(root);

        _ctx.ChangeTracker.Clear();
        Assert.True((await _ctx.Spaces.IgnoreQueryFilters().SingleAsync(s => s.Id == root)).IsDeleted);
        Assert.True(await _ctx.SpaceAddresses.IgnoreQueryFilters().Where(x => x.SpaceId == root).AllAsync(x => x.IsDeleted));
        Assert.True(await _ctx.SpaceAttributeValues.IgnoreQueryFilters().Where(x => x.SpaceId == root).AllAsync(x => x.IsDeleted));
        Assert.True(await _ctx.SpaceRates.IgnoreQueryFilters().Where(x => x.SpaceId == root).AllAsync(x => x.IsDeleted));
        Assert.False(await _ctx.Spaces.AnyAsync(s => s.Id == root));
    }

    [Fact]
    public async Task Create_And_Update_ReturnAreaWarning_WithoutBlocking()
    {
        var typeId = await NewTypeAsync();
        var root = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R"), area: 100m))).Id;
        var first = await _svc.Spaces.CreateAsync(Child(root, typeId, Code("A"), area: 60m));
        Assert.Empty(first.Warnings);

        var second = await _svc.Spaces.CreateAsync(Child(root, typeId, Code("B"), area: 50m));
        Assert.Contains(SpaceChangeWarning.ChildrenAreaExceedsParent, second.Warnings);

        var update = await _svc.Spaces.UpdateAsync(Update(await LoadAsync(first.Id)) with { Area = 70m });
        Assert.Contains(SpaceChangeWarning.ChildrenAreaExceedsParent, update.Warnings);
    }

    [Fact]
    public async Task History_RecordsCreatedNameAndTypeWithNames()
    {
        var oldTypeId = await NewTypeAsync("1");
        var newTypeId = await NewTypeAsync("2");
        var root = (await _svc.Spaces.CreateAsync(Root(oldTypeId, Code("R")))).Id;

        await _svc.Spaces.UpdateAsync(Update(await LoadAsync(root)) with { Name = "Yeni Ad", SpaceTypeId = newTypeId });

        var history = await _svc.Spaces.GetHistoryAsync(root);
        Assert.Contains(history, h => h.Field == SpaceHistoryField.Created);
        var name = Assert.Single(history, h => h.Field == SpaceHistoryField.Name);
        Assert.Equal($"Ad {Code("R")}", name.OldValue);
        Assert.Equal("Yeni Ad", name.NewValue);
        var type = Assert.Single(history, h => h.Field == SpaceHistoryField.Type);
        Assert.Equal($"Tur {_uid}1", type.OldValue);
        Assert.Equal($"Tur {_uid}2", type.NewValue);
        Assert.DoesNotContain(history, h => h.Field == SpaceHistoryField.Area);
    }

    [Fact]
    public async Task Code_MustBeUnique_UnderSameParentAndAmongRoots()
    {
        var typeId = await NewTypeAsync();
        var r1 = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R1")))).Id;
        var r2 = (await _svc.Spaces.CreateAsync(Root(typeId, Code("R2")))).Id;

        await AssertBusinessErrorAsync(() => _svc.Spaces.CreateAsync(Root(typeId, Code("R1"))), "Space.CodeExists");

        await AddChildAsync(r1, typeId, Code("C"));
        await AssertBusinessErrorAsync(() => AddChildAsync(r1, typeId, Code("C")), "Space.CodeExists");
        await AddChildAsync(r2, typeId, Code("C"));
    }
}
