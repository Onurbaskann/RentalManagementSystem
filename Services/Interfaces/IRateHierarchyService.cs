using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IRateHierarchyService
{
    Task<ParentTarifeKartViewModel?> GetParentForAsync(
        TarifeHiyerarsiKatmani katman,
        int? propertyId = null,
        int? unitId    = null,
        int? tenantCategoryId = null,
        int? yil        = null);

    Task<ParentReservationRateOverrideCardViewModel?> GetRezervasyonParentForAsync(int? yil = null);
}
