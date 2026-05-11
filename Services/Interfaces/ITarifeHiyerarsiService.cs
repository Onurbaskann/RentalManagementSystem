using KiraTakip.Models.ViewModels;

namespace KiraTakip.Services.Interfaces;

public interface ITarifeHiyerarsiService
{
    Task<ParentTarifeKartViewModel?> GetParentForAsync(
        TarifeHiyerarsiKatmani katman,
        int? tasinmazId = null,
        int? birimId    = null,
        int? kategoriId = null,
        int? yil        = null);
}
