using System.Reflection;
using KiraTakip.Auditing;
using KiraTakip.Domain.Auditing;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Entities.Interfaces;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Auditing;

namespace KiraTakip.Tests.Auditing;

public sealed class AuditDisplayNamesTests
{
    [Fact]
    public void EveryDefinedEventType_ShouldHaveDisplayName()
    {
        var eventTypes = typeof(AuditEventTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!);

        Assert.All(eventTypes, eventType =>
            Assert.NotEqual(eventType, AuditDisplayNames.EventDisplay(eventType)));
    }

    [Fact]
    public void EveryAuditedEntity_ShouldHaveDisplayName()
    {
        var auditedEntityTypes = typeof(ApplicationUser).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => typeof(IAuditable).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttribute<AuditExcludeAttribute>() is null);

        Assert.All(auditedEntityTypes, entityType =>
            Assert.NotEqual(entityType.Name, AuditDisplayNames.EntityDisplay(entityType.Name)));
    }

    [Theory]
    [InlineData(UserType.Internal, "İç kullanıcı")]
    [InlineData(UserType.Tenant, "Kiracı kullanıcısı")]
    public void UserTypeDisplay_ShouldUseTurkishNames(UserType userType, string expected)
        => Assert.Equal(expected, AuditDisplayNames.UserTypeDisplay(userType));
}
