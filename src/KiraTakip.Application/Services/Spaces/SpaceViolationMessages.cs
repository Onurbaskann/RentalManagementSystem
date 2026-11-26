using KiraTakip.Domain.Spaces;

namespace KiraTakip.Services.Spaces;

public static class SpaceViolationMessages
{
    public static string Code(SpaceChangeViolation violation) => $"Space.{violation}";

    public static string Message(SpaceChangeViolation violation) => violation switch
    {
        SpaceChangeViolation.ParentChangeNotAllowed => "Alanın üst alanı değiştirilemez.",
        SpaceChangeViolation.AreaNotPositive => "Yüzölçümü sıfırdan büyük olmalıdır.",
        SpaceChangeViolation.AreaChangeWithOpenLease => "Açık sözleşmesi olan alanın yüzölçümü değiştirilemez.",
        SpaceChangeViolation.UsageChangeWithOpenRecords => "Açık sözleşme, ön tahsis veya rezervasyonu olan alanın kullanımı değiştirilemez.",
        SpaceChangeViolation.UsageChangeWithOpenReservation => "Açık rezervasyonu olan alanın kullanımı değiştirilemez.",
        SpaceChangeViolation.NestedReservable => "Rezerve edilebilir alanın üstünde veya altında başka bir rezerve edilebilir alan olamaz.",
        SpaceChangeViolation.SharedChangeWithOpenRecords => "Açık kaydı olan alanın paylaşımlı bayrağı değiştirilemez.",
        SpaceChangeViolation.PreAllocatableRequiresRentable => "Ön tahsise açık alan kiralanabilir olmalıdır.",
        SpaceChangeViolation.SharedRequiresRentable => "Paylaşımlı alan kiralanabilir olmalıdır.",
        SpaceChangeViolation.PreAllocatableAndSharedConflict => "Alan hem ön tahsise açık hem paylaşımlı olamaz.",
        SpaceChangeViolation.DeactivateWithOpenRecords => "Açık sözleşme, ön tahsis veya rezervasyonu olan alan pasifleştirilemez.",
        SpaceChangeViolation.ActivateWithInactiveParent => "Üst alan pasifken alan etkinleştirilemez.",
        SpaceChangeViolation.DeleteWithOpenRecords => "Açık kaydı olan alan silinemez.",
        SpaceChangeViolation.DeleteWithHistory => "Geçmişi olan alan silinemez; pasifleştirin.",
        SpaceChangeViolation.DeleteWithChildren => "Alt alanı olan alan silinemez.",
        _ => "Alan işlemi yapılamaz."
    };

    public static string Field(SpaceChangeViolation violation) => violation switch
    {
        SpaceChangeViolation.AreaNotPositive or SpaceChangeViolation.AreaChangeWithOpenLease => "Area",
        SpaceChangeViolation.ParentChangeNotAllowed => "ParentId",
        SpaceChangeViolation.PreAllocatableRequiresRentable or SpaceChangeViolation.PreAllocatableAndSharedConflict => "IsPreAllocatable",
        SpaceChangeViolation.SharedRequiresRentable or SpaceChangeViolation.SharedChangeWithOpenRecords => "IsShared",
        _ => "Usage"
    };
}
