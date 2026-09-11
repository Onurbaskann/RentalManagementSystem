using KiraTakip.Domain.Documents;
using KiraTakip.Models.Enums;
using Xunit;

namespace KiraTakip.Tests;

public class DocumentTypeLifecyclePolicyTests
{
    [Fact]
    public void ResolveTargetEntity_WhenIsSystem_PreservesCurrentTarget()
    {
        var current = DocumentOwnerType.Tenant;
        var requested = DocumentOwnerType.Lease;

        var result = DocumentTypeLifecyclePolicy.ResolveTargetEntity(isSystem: true, current, requested);

        Assert.Equal(current, result);
    }

    [Fact]
    public void ResolveTargetEntity_WhenNotSystem_UsesRequestedTarget()
    {
        var current = DocumentOwnerType.Tenant;
        var requested = DocumentOwnerType.Payment;

        var result = DocumentTypeLifecyclePolicy.ResolveTargetEntity(isSystem: false, current, requested);

        Assert.Equal(requested, result);
    }

    [Theory]
    // Sistem kayıtları:
    [InlineData(true, true, false, false)] // Aktif sistem kaydı pasif yapılamaz
    [InlineData(true, true, true, true)]   // Aktif sistem kaydı aktif kalabilir
    [InlineData(true, false, true, true)]  // Pasif sistem kaydı tekrar aktif yapılabilir
    [InlineData(true, false, false, true)] // Pasif sistem kaydı pasif kalabilir
    // Kullanıcı kayıtları:
    [InlineData(false, true, false, true)] // Kullanıcı kaydı pasif yapılabilir
    [InlineData(false, true, true, true)]  // Kullanıcı kaydı aktif kalabilir
    [InlineData(false, false, true, true)] // Kullanıcı kaydı aktif yapılabilir
    [InlineData(false, false, false, true)]// Kullanıcı kaydı pasif kalabilir
    public void CanChangeActiveState_Combinations_ShouldFollowPolicyRules(
        bool isSystem,
        bool currentIsActive,
        bool requestedIsActive,
        bool expected)
    {
        var result = DocumentTypeLifecyclePolicy.CanChangeActiveState(
            isSystem,
            currentIsActive,
            requestedIsActive);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CanDelete_ShouldOnlyAllowNonSystemRecords(bool isSystem, bool expected)
    {
        Assert.Equal(expected, DocumentTypeLifecyclePolicy.CanDelete(isSystem));
    }
}