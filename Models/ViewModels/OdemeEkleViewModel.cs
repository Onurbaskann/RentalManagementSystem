using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class OdemeEkleViewModel
{
    public int KiraTahakkukId { get; set; }
    public int? KiraSozlesmesiId { get; set; }
    public DateTime OdemeTarihi { get; set; } = DateTime.Today;

    [Range(0.01, double.MaxValue, ErrorMessage = "Tutar sıfırdan büyük olmalıdır.")]
    public decimal Tutar { get; set; }
    public OdemeKanali OdemeKanali { get; set; } = OdemeKanali.EFT;
    public string? Aciklama { get; set; }
    public TahakkukDetayDto? Tahakkuk { get; set; }
}
