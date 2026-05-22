using System.ComponentModel.DataAnnotations;

namespace KiraTakip.Models.ViewModels;

public class ManuelBorcIptalViewModel
{
    public int TahakkukId { get; set; }

    [MaxLength(500, ErrorMessage = "İptal nedeni en fazla 500 karakter olabilir.")]
    public string Neden { get; set; } = string.Empty;
}
