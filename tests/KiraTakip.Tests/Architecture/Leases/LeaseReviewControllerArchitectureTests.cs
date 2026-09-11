using KiraTakip.Authorization;
using KiraTakip.Web.Controllers;
using KiraTakip.Web.DependencyInjection;
using KiraTakip.Infrastructure.Validation;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Web.Validators;
using KiraTakip.Services.Interfaces.Leases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace KiraTakip.Tests;

public class LeaseReviewControllerArchitectureTests
{
    [Theory]
    [InlineData(nameof(LeaseController.UpdateDraft), PermissionCatalog.Lease.Create)]
    [InlineData(nameof(LeaseController.ResubmitRevision), PermissionCatalog.Lease.Create)]
    [InlineData(nameof(LeaseController.Approve), PermissionCatalog.Lease.Approve)]
    [InlineData(nameof(LeaseController.RequestRevision), PermissionCatalog.Lease.RequestRevision)]
    [InlineData(nameof(LeaseController.DeleteDraft), PermissionCatalog.Lease.DeleteDraft)]
    public void MutationActions_ArePostAntiforgeryAndPolicyProtected(string actionName, string policy)
    {
        var method = GetAction(actionName);
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal(policy, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Equal(typeof(Task<IActionResult>), method.ReturnType);
    }

    [Fact]
    public void Draft_IsInternalLeaseModuleEndpoint()
    {
        var method = GetAction(nameof(LeaseController.Draft));
        Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>());
        Assert.Equal(
            PermissionCatalog.Lease.Module,
            Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
    }

    [Fact]
    public void EveryLeaseMutation_HasAntiforgeryProtection()
    {
        var mutations = typeof(LeaseController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.NotEmpty(mutations);
        Assert.All(mutations, method =>
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
    }

    [Fact]
    public void DraftDocumentUpload_IsOwnerWorkflowProtected()
    {
        var method = GetAction(nameof(LeaseController.UploadDraftDocument));
        Assert.NotNull(method.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        Assert.Equal(
            PermissionCatalog.Lease.Create,
            Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
    }

    [Fact]
    public void DecisionValidators_RejectWhitespaceAndLongReasons_AndAreDiscovered()
    {
        var revisionValidator = new RequestLeaseRevisionViewModelValidator();
        Assert.False(revisionValidator.Validate(new RequestLeaseRevisionViewModel
        {
            LeaseId = 1,
            RowVersion = [1],
            Reason = "   "
        }).IsValid);
        Assert.False(revisionValidator.Validate(new RequestLeaseRevisionViewModel
        {
            LeaseId = 1,
            RowVersion = [1],
            Reason = new string('a', 1001)
        }).IsValid);

        var services = new ServiceCollection().AddValidationModule().BuildServiceProvider();
        Assert.NotNull(services.GetService<IValidator<RequestLeaseRevisionViewModel>>());
        Assert.NotNull(services.GetService<IValidator<DeleteLeaseDraftViewModel>>());
        Assert.NotNull(services.GetService<IValidator<ApproveLeaseViewModel>>());
        Assert.NotNull(services.GetService<IValidator<LeaseDraftViewModel>>());
    }

    [Fact]
    public void Controller_DoesNotDependOnRepositoryOrDbContext()
    {
        var constructor = Assert.Single(typeof(LeaseController).GetConstructors());
        Assert.DoesNotContain(constructor.GetParameters(), parameter =>
            parameter.ParameterType.Name.Contains("Repository", StringComparison.Ordinal)
            || parameter.ParameterType.Name.Contains("DbContext", StringComparison.Ordinal));
        Assert.Contains(constructor.GetParameters(), parameter => parameter.ParameterType == typeof(ILeaseService));
    }

    private static MethodInfo GetAction(string actionName)
        => Assert.Single(typeof(LeaseController).GetMethods(BindingFlags.Instance | BindingFlags.Public),
            method => method.Name == actionName);
}
