using KiraTakip.Domain.Spaces;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;

namespace KiraTakip.Web.Models.ViewModels;

public class SpaceFormViewModel
{
    public int RootId { get; set; }
    public int? ParentId { get; set; }
    public int SpaceTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 1;
    public string? Description { get; set; }
    public decimal Area { get; set; }
    public SpaceUsage Usage { get; set; } = SpaceUsage.Rentable;
    public bool IsPreAllocatable { get; set; }
    public bool IsShared { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class SpaceAddressFormViewModel
{
    public int RootId { get; set; }
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class SpaceAttributeValueFormItem
{
    public int AttributeDefinitionId { get; set; }
    public string? Value { get; set; }
}

public class SpaceAttributesFormViewModel
{
    public int RootId { get; set; }
    public List<SpaceAttributeValueFormItem> Values { get; set; } = new();
}

public class SpaceRateFormViewModel
{
    public int RootId { get; set; }
    public string Operation { get; set; } = SpaceRateOperations.Save;
    public int RateId { get; set; }
    public int TenantCategoryId { get; set; }
    public int ChargeTypeId { get; set; }
    public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Fixed;
    public decimal UnitValue { get; set; }
    public decimal KdvRate { get; set; }
}

public static class SpaceRateOperations
{
    public const string Save = "Save";
    public const string Delete = "Delete";
}

public class SpaceIndexViewModel
{
    public List<SpaceTreeRow> Roots { get; set; } = new();
    public List<SpaceTypeDto> Types { get; set; } = new();
    public SpaceFormViewModel Form { get; set; } = new();
}

public class SpaceTreeViewModel
{
    public SpaceTreeNodeDto Tree { get; set; } = null!;
    public SpaceDetailDto Selected { get; set; } = null!;
    public bool IsRoot => Selected.ParentId == null;
    public int RootId => Tree.Id;
    public List<SpaceTypeDto> Types { get; set; } = new();
    public List<SpaceHistoryDto> History { get; set; } = new();
    public SpaceAddressInput? Address { get; set; }
    public List<SpaceAttributeDto> Attributes { get; set; } = new();
    public Dictionary<int, string> AttributeValues { get; set; } = new();
    public List<SpaceRateDto> Rates { get; set; } = new();
    public Dictionary<int, string> TenantCategoryNames { get; set; } = new();
    public Dictionary<int, string> ChargeTypeNames { get; set; } = new();
    public SpaceFormViewModel ChildForm { get; set; } = new();
}

public class SpaceDeleteViewModel
{
    public int RootId { get; set; }
    public SpaceDetailDto Detail { get; set; } = null!;
    public SpaceDeletePreviewDto Preview { get; set; } = null!;
}

public class SpaceTypeFormViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 1;
    public SpaceUsage DefaultUsage { get; set; } = SpaceUsage.Rentable;
    public bool DefaultIsPreAllocatable { get; set; }
    public List<int> DefaultAttributeIds { get; set; } = new();
    public List<SpaceAttributeDto> AttributeCandidates { get; set; } = new();
}

public class SpaceAttributeFormViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AttributeDataType DataType { get; set; } = AttributeDataType.Text;
    public string? Options { get; set; }
}

public static class SpaceDisplay
{
    public static string Usage(SpaceUsage usage) => usage switch
    {
        SpaceUsage.Rentable => "Kiralanabilir",
        SpaceUsage.Reservable => "Rezerve Edilebilir",
        _ => "Kiralanamaz"
    };

    public static string UsageBadge(SpaceUsage usage) => usage switch
    {
        SpaceUsage.Rentable => "badge-kirali",
        SpaceUsage.Reservable => "badge-surek",
        _ => "badge-bos"
    };

    public static string DataType(AttributeDataType type) => type switch
    {
        AttributeDataType.Text => "Metin",
        AttributeDataType.Number => "Sayı",
        AttributeDataType.Boolean => "Evet/Hayır",
        _ => "Liste"
    };

    public static string Method(CalculationMethod method) => method == CalculationMethod.M2 ? "m² başına" : "Sabit tutar";

    public static string HistoryField(SpaceHistoryField field) => field switch
    {
        SpaceHistoryField.Created => "Oluşturuldu",
        SpaceHistoryField.Type => "Tür",
        SpaceHistoryField.Name => "Ad",
        SpaceHistoryField.Parent => "Üst alan",
        SpaceHistoryField.Area => "Yüzölçümü",
        SpaceHistoryField.Usage => "Kullanım",
        SpaceHistoryField.PreAllocatable => "Ön tahsis",
        SpaceHistoryField.Shared => "Paylaşımlı",
        _ => "Durum"
    };

    public static string Violation(SpaceChangeViolation violation) => KiraTakip.Services.Spaces.SpaceViolationMessages.Message(violation);

    public static string Warning(SpaceChangeWarning warning) => warning switch
    {
        SpaceChangeWarning.ChildrenAreaExceedsParent => "Alt alanların toplam m²'si üst alanın m²'sini aşıyor.",
        _ => "Uyarı."
    };
}

public record SpaceTreeNodeView(SpaceTreeNodeDto Node, int SelectedId, int RootId);

public record SpaceFieldsModel(SpaceFormViewModel Form, IReadOnlyList<SpaceTypeDto> Types, bool ShowAddress, bool IsEdit);
