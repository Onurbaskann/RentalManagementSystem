using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class TasinmazDetayViewModel
{
    public PropertyDetailDto Property { get; set; } = null!;
    public TasinmazFiyatMatrisiViewModel FiyatMatrisi { get; set; } = null!;
}
