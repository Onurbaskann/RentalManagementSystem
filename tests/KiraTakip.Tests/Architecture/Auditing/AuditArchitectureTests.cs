using System.Reflection;
using KiraTakip.Auditing;
using KiraTakip.Domain.Auditing;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Entities;
using KiraTakip.Services.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Tests.Architecture.Auditing;

public class AuditArchitectureTests
{
    [Fact]
    public void AuditEventTypes_ShouldBeUniqueAndFollowHierarchicalNaming()
    {
        var values = typeof(AuditEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(values);
        Assert.Equal(values.Count, values.Distinct(StringComparer.Ordinal).Count());
        Assert.All(values, value =>
        {
            Assert.Contains('.', value);
            Assert.DoesNotContain(' ', value);
        });
    }

    [Fact]
    public void ExistingMutationServicesWithManualAudit_ShouldBeTransactional()
    {
        Assert.True(typeof(ITransactionalService).IsAssignableFrom(typeof(RoleService)));

        Assert.False(typeof(ITransactionalService).IsAssignableFrom(typeof(InvitationService)));
        Assert.NotNull(typeof(InvitationService)
            .GetMethod(nameof(InvitationService.AcceptAsync))!
            .GetCustomAttribute<TransactionalAttribute>());
        Assert.NotNull(typeof(InvitationService)
            .GetMethod(nameof(InvitationService.CancelAsync))!
            .GetCustomAttribute<TransactionalAttribute>());
        Assert.Null(typeof(InvitationService)
            .GetMethod(nameof(InvitationService.SendAsync))!
            .GetCustomAttribute<TransactionalAttribute>());
        Assert.Null(typeof(InvitationService)
            .GetMethod(nameof(InvitationService.ResendAsync))!
            .GetCustomAttribute<TransactionalAttribute>());

        Assert.NotNull(typeof(AdminUserService)
            .GetMethod(nameof(AdminUserService.UpdateAsync))!
            .GetCustomAttribute<TransactionalAttribute>());
        Assert.NotNull(typeof(AdminUserService)
            .GetMethod(nameof(AdminUserService.ToggleActiveAsync))!
            .GetCustomAttribute<TransactionalAttribute>());
        Assert.NotNull(typeof(PasswordResetService)
            .GetMethod(nameof(PasswordResetService.ResetPasswordAsync))!
            .GetCustomAttribute<TransactionalAttribute>());

        Assert.Null(typeof(PasswordResetService)
            .GetMethod(nameof(PasswordResetService.RequestAsync))!
            .GetCustomAttribute<TransactionalAttribute>());
    }

    [Fact]
    public void AuditCatalogs_ShouldRejectUnknownValues()
    {
        Assert.True(AuditEventTypes.IsDefined(AuditEventTypes.UserLoginSuccess));
        Assert.False(AuditEventTypes.IsDefined("User.Misspelled"));
        Assert.True(AuditEntityTypes.IsDefined(AuditEntityTypes.ApplicationUser));
        Assert.False(AuditEntityTypes.IsDefined("UnknownEntity"));
    }

    [Fact]
    public void AuditDetails_ShouldRequireStructuredJsonObject()
    {
        var details = AuditDetails.Serialize(new { reason = "test", count = 2 });

        AuditDetails.EnsureStructured(details);
        Assert.ThrowsAny<Exception>(() => AuditDetails.EnsureStructured("serbest metin"));
        Assert.Throws<ArgumentException>(() => AuditDetails.EnsureStructured("[1,2]"));
    }

    [Fact]
    public void AddTransactionalProxies_ShouldProxyServicesWithMethodLevelAttribute()
    {
        var services = new ServiceCollection();
        services.AddScoped<ISelectiveTransactionService, SelectiveTransactionService>();

        services.AddTransactionalProxies();

        var descriptor = Assert.Single(services, item => item.ServiceType == typeof(ISelectiveTransactionService));
        Assert.NotNull(descriptor.ImplementationFactory);
    }

    [Theory]
    [InlineData(typeof(LookupValue))]
    [InlineData(typeof(LeaseActivityLog))]
    [InlineData(typeof(LeaseReviewHistory))]
    public void TechnicalOrAlreadyHistoricalEntities_ShouldBeExcludedFromGeneralAudit(Type entityType)
    {
        Assert.NotNull(entityType.GetCustomAttribute<AuditExcludeAttribute>());
    }
}

internal interface ISelectiveTransactionService
{
    Task ReadAsync();
    Task MutateAsync();
}

internal sealed class SelectiveTransactionService : ISelectiveTransactionService
{
    public Task ReadAsync() => Task.CompletedTask;

    [Transactional]
    public Task MutateAsync() => Task.CompletedTask;
}
