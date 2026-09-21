using Castle.DynamicProxy;
using KiraTakip.Data;
using KiraTakip.Auditing;
using KiraTakip.Infrastructure.Auditing;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KiraTakip.Tests.Integration.Auditing;

[Collection("Database collection")]
public sealed class AuditTransactionIntegrationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task AuditInterceptor_ShouldSkipExcludedEntityAndKeepBusinessEntity()
    {
        var auditContext = new TestAuditContext();
        var interceptor = new AuditSaveChangesInterceptor(new MaskingService(), auditContext);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new ApplicationDbContext(
            options,
            new DummyHttpContextAccessor(),
            new DummyCurrentUserContext());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        context.LookupValues.Add(new LookupValue
        {
            EnumName = $"AuditTest{suffix}",
            Value = 987654,
            Name = "Ignored"
        });
        var category = new Category
        {
            Type = CategoryType.Sector,
            Name = $"Audit Test {suffix}",
            Code = $"AUD_{suffix}"
        };
        context.Kategoriler.Add(category);

        await context.SaveChangesAsync();

        Assert.DoesNotContain(context.AuditLogs.Local, log => log.EntityType == nameof(LookupValue));
        var categoryAudit = Assert.Single(
            context.AuditLogs.Local,
            log => log.EntityType == nameof(Category));
        Assert.Equal(category.Id.ToString(), categoryAudit.EntityId);
        Assert.DoesNotContain("\"prop\":\"Id\"", categoryAudit.Details);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task AuditInterceptor_WithoutOuterTransaction_ShouldPersistGeneratedEntityId()
    {
        var auditContext = new TestAuditContext();
        var interceptor = new AuditSaveChangesInterceptor(new MaskingService(), auditContext);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var context = new ApplicationDbContext(
            options,
            new DummyHttpContextAccessor(),
            new DummyCurrentUserContext());
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var category = new Category
        {
            Type = CategoryType.Sector,
            Name = $"Audit Generated Id Test {suffix}",
            Code = $"AGI_{suffix}"
        };

        context.Kategoriler.Add(category);
        await context.SaveChangesAsync();

        var audits = await context.AuditLogs
            .Where(log => log.EntityType == nameof(Category) && log.EntityId == category.Id.ToString())
            .ToListAsync();
        var audit = Assert.Single(audits);
        Assert.DoesNotContain("\"prop\":\"Id\"", audit.Details);

        await context.AuditLogs
            .Where(log => log.Id == audit.Id)
            .ExecuteDeleteAsync();
        await context.Kategoriler
            .Where(item => item.Id == category.Id)
            .ExecuteDeleteAsync();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationFails_RollsBackAuditRecord()
    {
        var eventType = $"Test.Rollback.{Guid.NewGuid():N}";

        await using (var context = fixture.CreateContext())
        {
            var unitOfWork = new UnitOfWork(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    context.AuditLogs.Add(new AuditLog { EventType = eventType });
                    await unitOfWork.SaveChangesAsync(ct);
                    throw new InvalidOperationException("rollback");
                }));
        }

        await using var verificationContext = fixture.CreateContext();
        Assert.False(await verificationContext.AuditLogs.AnyAsync(log => log.EventType == eventType));
    }

    [Fact]
    public async Task MethodLevelTransaction_ShouldWrapOnlyAttributedMethod()
    {
        await using var context = fixture.CreateContext();
        var interceptor = new TransactionInterceptor(
            context,
            NullLogger<TransactionInterceptor>.Instance);
        var target = new SelectiveTransactionProbe(context);
        var proxy = new ProxyGenerator().CreateInterfaceProxyWithTarget<ISelectiveTransactionProbe>(
            target,
            interceptor.ToInterceptor());

        Assert.False(await proxy.ReadHasTransactionAsync());
        Assert.True(await proxy.WriteHasTransactionAsync());
    }
}

internal sealed class TestAuditContext : IAuditContext
{
    public string? UserId => "audit-test-user";
    public UserType? UserType => KiraTakip.Models.Enums.UserType.Internal;
    public int? TenantId => null;
    public string? IpAddress => "127.0.0.1";
    public string? UserAgent => "AuditTests/1.0";
}

public interface ISelectiveTransactionProbe
{
    Task<bool> ReadHasTransactionAsync();
    Task<bool> WriteHasTransactionAsync();
}

public sealed class SelectiveTransactionProbe(ApplicationDbContext context) : ISelectiveTransactionProbe
{
    public Task<bool> ReadHasTransactionAsync()
        => Task.FromResult(context.Database.CurrentTransaction is not null);

    [Transactional]
    public Task<bool> WriteHasTransactionAsync()
        => Task.FromResult(context.Database.CurrentTransaction is not null);
}
