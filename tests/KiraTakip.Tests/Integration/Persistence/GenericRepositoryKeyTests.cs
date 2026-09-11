using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Repositories.Auditing;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Documents;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Leases;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Tests;

public class GenericRepositoryArchitectureTests
{
    [Fact]
    public void KeyedReadBase_ShouldNotExposeWriteOrDeleteOperations()
    {
        var declaredMethods = typeof(Repository<,>)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(Repository<,>))
            .Select(method => method.Name)
            .ToHashSet();

        Assert.DoesNotContain("AddAsync", declaredMethods);
        Assert.DoesNotContain("UpdateAsync", declaredMethods);
        Assert.DoesNotContain("DeleteAsync", declaredMethods);

        Assert.Contains(
            typeof(RepositoryBase<,>).GetMethods(),
            method => method.Name == "DeleteAsync");
    }

    [Fact]
    public void FormerDirectContextRepositories_ShouldInheritTheCorrectBase()
    {
        Assert.True(typeof(Repository<ApplicationUser, string>)
            .IsAssignableFrom(typeof(ApplicationUserRepository)));
        Assert.True(typeof(Repository<AuditLog, long>)
            .IsAssignableFrom(typeof(AuditLogRepository)));
        Assert.True(typeof(Repository<DocumentContent, int>)
            .IsAssignableFrom(typeof(DocumentContentRepository)));
        Assert.True(typeof(Repository<RolePermission, int>)
            .IsAssignableFrom(typeof(RolePermissionRepository)));
        Assert.True(typeof(RepositoryBase<LeaseReviewHistory>)
            .IsAssignableFrom(typeof(LeaseReviewHistoryRepository)));
        Assert.True(typeof(RepositoryBase<UserPermission>)
            .IsAssignableFrom(typeof(UserPermissionRepository)));
    }

    [Fact]
    public async Task GuidBaseEntity_ShouldSupportCrudAndSoftDelete()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=GuidRepositoryModelOnly;Trusted_Connection=True")
            .Options;
        await using var context = new GuidRepositoryTestDbContext(options);
        var repository = new GuidEntityRepository(context);
        var entity = new GuidTestEntity { Id = Guid.NewGuid() };

        await repository.AddAsync(entity);
        await repository.DeleteAsync(entity.Id);

        Assert.True(entity.IsDeleted);
        Assert.False(entity.IsActive);
        Assert.NotNull(context.Model.FindEntityType(typeof(GuidTestEntity))!.GetQueryFilter());
        Assert.True(typeof(BaseEntity<int>).IsAssignableFrom(typeof(Lease)));
    }
}

[Collection("Database collection")]
public sealed class GenericRepositoryKeySqlTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public GenericRepositoryKeySqlTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetById_ShouldTranslateIntLongStringAndCustomNamedKeys()
    {
        var applicationUser = await new ApplicationUserRepository(_context)
            .GetByIdAsync("missing-user-id");
        var auditLog = await new AuditLogRepository(_context)
            .GetByIdAsync(-1L);
        var rolePermission = await new RolePermissionRepository(_context)
            .GetByIdAsync(-1);
        var documentRepository = new DocumentContentRepository(_context);
        var documentContent = await documentRepository.GetByIdAsync(-1);
        var documentId = await documentRepository
            .GetByIdAsync<int?>(-1, content => content.DocumentId);

        Assert.Null(applicationUser);
        Assert.Null(auditLog);
        Assert.Null(rolePermission);
        Assert.Null(documentContent);
        Assert.Null(documentId);
    }
}

internal sealed class GuidTestEntity : BaseEntity<Guid>
{
}

internal sealed class GuidEntityRepository(ApplicationDbContext context)
    : RepositoryBase<GuidTestEntity, Guid>(context);

internal sealed class GuidRepositoryTestDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : ApplicationDbContext(
        options,
        new DummyHttpContextAccessor(),
        new DummyCurrentUserContext())
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<GuidTestEntity>();
        base.OnModelCreating(builder);
    }
}
