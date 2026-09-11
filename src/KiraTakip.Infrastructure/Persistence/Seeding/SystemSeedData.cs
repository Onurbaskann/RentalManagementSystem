using KiraTakip.Models.Enums;

namespace KiraTakip.Data.Seeding;

internal static class SystemSeedData
{
    internal static readonly ChargeType[] ChargeTypes =
    [
        new()
        {
            Id = 1,
            Code = "KIRA",
            Name = "Kira Bedeli",
            IsActive = true,
            SortOrder = 1,
            Behavior = ChargeTypeBehavior.MonthlyFixed,
            IsSystem = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "System",
            IsDeleted = false
        },
        new()
        {
            Id = 2,
            Code = "DEPOZITO",
            Name = "Depozito",
            IsActive = true,
            SortOrder = 99,
            Behavior = ChargeTypeBehavior.FirstMonthOneTime,
            IsSystem = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "System",
            IsDeleted = false
        },
        new()
        {
            Id = 3,
            Code = "DIGER",
            Name = "Diğer",
            IsActive = true,
            SortOrder = 100,
            Behavior = ChargeTypeBehavior.UserManual,
            IsSystem = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "System",
            IsDeleted = false
        }
    ];

    internal static readonly DocumentType[] DocumentTypes =
    [
        new()
        {
            Id = 1,
            Code = "ODEME_DEKONT",
            Name = "Ödeme Dekontu",
            TargetEntity = DocumentOwnerType.Payment,
            AllowedExtensions = "pdf,jpg,jpeg,png",
            MaxSizeMb = 5,
            SortOrder = 1,
            IsSystem = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "System",
            IsActive = true,
            IsDeleted = false
        }
    ];
}
