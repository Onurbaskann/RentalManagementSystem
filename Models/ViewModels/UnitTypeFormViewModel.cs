using System.ComponentModel.DataAnnotations;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public class UnitTypeFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 1;
    public bool CanBeRented { get; set; } = true;
    public bool CanBeReserved { get; set; }
    public int? ChargeTypeId { get; set; }
    public bool IsActive { get; set; } = true;

    public List<BorcTipiLookupDto> ChargeTypeCandidates { get; set; } = [];
}
