using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class TasinmazDetayViewModel
{
    public TasinmazDetayDto Tasinmaz { get; set; } = null!;
    public TasinmazFiyatMatrisiViewModel FiyatMatrisi { get; set; } = null!;
}
