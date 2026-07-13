namespace KiraTakip.Models.Dtos;

public class CategoryListItemDto
{
    public int Id { get; set; }
    public CategoryType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
