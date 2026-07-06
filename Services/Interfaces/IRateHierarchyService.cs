using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface IRateHierarchyService
{
    Task<ParentTarifeKartViewModel?> GetParentForAsync(
        TarifeHiyerarsiKatmani katman,
        int? propertyId = null,
        int? unitId    = null,
        int? kategoriId = null,
        int? yil        = null);

    Task<ParentRezervasyonTarifeKartViewModel?> GetRezervasyonParentForAsync(int? yil = null);
}
