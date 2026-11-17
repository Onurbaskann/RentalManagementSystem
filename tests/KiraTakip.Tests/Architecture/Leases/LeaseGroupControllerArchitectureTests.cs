using KiraTakip.Authorization;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.DependencyInjection;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Web.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace KiraTakip.Tests;

public class LeaseGroupControllerArchitectureTests
{
    [Theory]
    [InlineData(nameof(LeaseController.CreateGroup), PermissionCatalog.Lease.CreateGroup)]
    [InlineData(nameof(LeaseController.UpdateGroup), PermissionCatalog.Lease.UpdateGroup)]
    [InlineData(nameof(LeaseController.ResubmitGroup), PermissionCatalog.Lease.ResubmitGroup)]
    [InlineData(nameof(LeaseController.DeleteGroupDraft), PermissionCatalog.Lease.DeleteGroupDraft)]
    [InlineData(nameof(LeaseController.ApproveGroup), PermissionCatalog.Lease.ApproveGroup)]
    [InlineData(nameof(LeaseController.ExtendGroup), PermissionCatalog.Lease.ExtendGroup)]
    [InlineData(nameof(LeaseController.TerminateGroup), PermissionCatalog.Lease.TerminateGroup)]
    [InlineData(nameof(LeaseController.RestructureGroup), PermissionCatalog.Lease.RestructureGroup)]
    public void GroupMutations_ArePostAntiforgeryAndOwnPermissionProtected(string actionName, string policy)
    {
        var method = Assert.Single(Actions(actionName), candidate =>
            candidate.GetCustomAttribute<HttpPostAttribute>() != null);
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal(policy, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Equal(typeof(Task<IActionResult>), method.ReturnType);
    }

    [Fact]
    public void GroupMutationPermissions_AreDistinct()
    {
        var policies = new[]
        {
            PermissionCatalog.Lease.CreateGroup, PermissionCatalog.Lease.UpdateGroup,
            PermissionCatalog.Lease.ResubmitGroup, PermissionCatalog.Lease.DeleteGroupDraft,
            PermissionCatalog.Lease.ApproveGroup,
            PermissionCatalog.Lease.ExtendGroup, PermissionCatalog.Lease.TerminateGroup,
            PermissionCatalog.Lease.RestructureGroup
        };
        Assert.Equal(policies.Length, policies.Distinct().Count());
    }

    [Theory]
    [InlineData(nameof(LeaseController.CreateGroup))]
    [InlineData(nameof(LeaseController.Group))]
    [InlineData(nameof(LeaseController.EditGroup))]
    [InlineData(nameof(LeaseController.RestructureGroup))]
    public void GroupScreens_AreLeaseModuleGetEndpoints(string actionName)
    {
        var method = Assert.Single(Actions(actionName), candidate =>
            candidate.GetCustomAttribute<HttpPostAttribute>() == null);
        Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal(
            PermissionCatalog.Lease.Module,
            Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
    }

    [Fact]
    public void GroupDetailRoute_IsLeaseGroupId()
    {
        var method = Assert.Single(Actions(nameof(LeaseController.Group)));
        Assert.Equal("Lease/Group/{id}", method.GetCustomAttribute<HttpGetAttribute>()!.Template);
    }

    [Fact]
    public void GroupInputs_HaveValidators()
    {
        var services = new ServiceCollection()
            .AddValidationModule().BuildServiceProvider();
        Assert.NotNull(services.GetService<KiraTakip.Infrastructure.Validation.IValidator<LeaseGroupFormViewModel>>());
        Assert.NotNull(services.GetService<KiraTakip.Infrastructure.Validation.IValidator<ApproveLeaseGroupViewModel>>());
        Assert.NotNull(services.GetService<KiraTakip.Infrastructure.Validation.IValidator<ResubmitLeaseGroupViewModel>>());
        Assert.NotNull(services.GetService<KiraTakip.Infrastructure.Validation.IValidator<DeleteLeaseGroupDraftViewModel>>());
        Assert.NotNull(services.GetService<KiraTakip.Infrastructure.Validation.IValidator<ExtendLeaseGroupViewModel>>());
        Assert.NotNull(services.GetService<KiraTakip.Infrastructure.Validation.IValidator<TerminateLeaseGroupViewModel>>());
        Assert.NotNull(services.GetService<KiraTakip.Infrastructure.Validation.IValidator<LeaseGroupRestructureViewModel>>());
    }

    [Fact]
    public void DeleteGroupDraftValidator_RequiresExplanation()
    {
        var validator = new DeleteLeaseGroupDraftViewModelValidator();
        Assert.False(validator.Validate(new DeleteLeaseGroupDraftViewModel { GroupId = 1, Explanation = "  " }).IsValid);
        Assert.True(validator.Validate(new DeleteLeaseGroupDraftViewModel { GroupId = 1, Explanation = "Hatalı giriş" }).IsValid);
    }

    private static IEnumerable<MethodInfo> Actions(string actionName)
        => typeof(LeaseController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == actionName);
}
