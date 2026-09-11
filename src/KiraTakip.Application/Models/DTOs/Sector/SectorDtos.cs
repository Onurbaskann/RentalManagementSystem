namespace KiraTakip.Models.Dtos.Sector;

public record CreateSectorInput(string Name, int Order, bool IsActive);

public record EditSectorInput(int Id, string Name, int Order, bool IsActive);

public record GetSectorByIdInput(int Id);

public record ToggleSectorStatusInput(int Id);
