namespace KiraTakip.Models.Dtos.TenantCategory;

public record CreateTenantCategoryInput(string Name, int Order, bool IsActive);

public record EditTenantCategoryInput(int Id, string Name, int Order, bool IsActive);

public record GetTenantCategoryByIdInput(int Id);

public record ToggleTenantCategoryStatusInput(int Id);
