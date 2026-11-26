using KiraTakip.Authorization;
using KiraTakip.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace KiraTakip.Tests;

public class SpaceControllerArchitectureTests
{
    public static TheoryData<Type, string> Controllers => new()
    {
        { typeof(SpaceController), PermissionCatalog.Space.Module },
        { typeof(AdminSpaceTypeController), PermissionCatalog.SpaceType.Module },
        { typeof(AdminSpaceAttributeController), PermissionCatalog.SpaceAttribute.Module }
    };

    private static IEnumerable<MethodInfo> Endpoints(Type controller)
        => controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => typeof(IActionResult).IsAssignableFrom(m.ReturnType)
                || typeof(Task<IActionResult>).IsAssignableFrom(m.ReturnType));

    [Theory]
    [MemberData(nameof(Controllers))]
    public void HttpPost_ShouldHaveOwnPermissionNamedAfterEndpoint(Type controller, string module)
    {
        var posts = Endpoints(controller).Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null).ToList();
        Assert.NotEmpty(posts);

        foreach (var method in posts)
        {
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
            var policy = method.GetCustomAttribute<AuthorizeAttribute>()?.Policy;
            Assert.Equal($"{module}.{method.Name}", policy);
        }
    }

    [Theory]
    [MemberData(nameof(Controllers))]
    public void HttpGet_ShouldUseModulePermission(Type controller, string module)
    {
        var gets = Endpoints(controller).Where(m => m.GetCustomAttribute<HttpGetAttribute>() != null).ToList();
        Assert.NotEmpty(gets);

        foreach (var method in gets)
            Assert.Equal(module, method.GetCustomAttribute<AuthorizeAttribute>()?.Policy);
    }

    [Theory]
    [MemberData(nameof(Controllers))]
    public void EveryPostPermission_ShouldBeDefinedInCatalog(Type controller, string module)
    {
        var catalogActions = typeof(PermissionCatalog).GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && f.GetRawConstantValue() is string)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet();

        foreach (var method in Endpoints(controller).Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null))
            Assert.Contains($"{module}.{method.Name}", catalogActions);
    }
}
