using System.ComponentModel.DataAnnotations;
using KiraTakip.Models;

namespace KiraTakip.Models.ViewModels;

public class CategoryFormViewModel
{
    public int Id { get; set; }
    public CategoryType Type { get; set; }

    [Required(ErrorMessage = "Name zorunludur.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Order { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
