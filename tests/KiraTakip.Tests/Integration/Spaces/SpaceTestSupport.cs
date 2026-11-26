using KiraTakip.Models.Entities;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Catalog;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Spaces;
using KiraTakip.Services.Spaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests.Integration.Spaces;

internal sealed class SpaceTestServices(ApplicationDbContext ctx)
{
    public SpaceService Spaces { get; } = new(
        new SpaceRepository(ctx),
        new SpaceTypeRepository(ctx),
        new SpaceLockRepository(ctx),
        new UnitOfWork(ctx),
        new DummyCurrentUserContext(),
        TimeProvider.System);

    public SpaceTypeService Types { get; } = new(
        new SpaceTypeRepository(ctx),
        new SpaceAttributeRepository(ctx),
        new SpaceRepository(ctx),
        new UnitOfWork(ctx));

    public SpaceAttributeService Attributes { get; } = new(
        new SpaceAttributeRepository(ctx),
        new SpaceRepository(ctx),
        new UnitOfWork(ctx));

    public SpaceRateService Rates { get; } = new(
        new SpaceRateRepository(ctx),
        new SpaceRepository(ctx),
        new CategoryRepository(ctx),
        new ChargeTypeRepository(ctx),
        new UnitOfWork(ctx));
}

internal static class SpaceTestData
{
    public static SpaceAddressInput Address { get; } = new("Ankara", "Cankaya", "Test Mah.", "Test Sk. 1");

    public static SpaceTypeInput TypeInput(string code, string name = "Test Turu")
        => new(code, name, 1, SpaceUsage.Rentable, false, null);

    public static CreateSpaceInput Root(int typeId, string code, decimal area = 100m, SpaceUsage usage = SpaceUsage.Rentable)
        => new(null, typeId, code, $"Ad {code}", 1, null, area, usage, false, false, Address);

    public static CreateSpaceInput Child(int parentId, int typeId, string code, decimal area = 10m, SpaceUsage usage = SpaceUsage.Rentable)
        => new(parentId, typeId, code, $"Ad {code}", 1, null, area, usage, false, false, null);

    public static UpdateSpaceInput Update(Space space)
        => new(space.Id, space.ParentId, space.SpaceTypeId, space.Name, space.SortOrder, space.Description,
            space.Area, space.Usage, space.IsPreAllocatable, space.IsShared);

    public static string NewUid() => Guid.NewGuid().ToString("N")[..8];

    public static async Task<BusinessException> AssertBusinessErrorAsync(Func<Task> action, string code)
    {
        var exception = await Assert.ThrowsAnyAsync<BusinessException>(action);
        Assert.Equal(code, exception.Code);
        return exception;
    }
}
