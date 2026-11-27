namespace KiraTakip.Models.Dtos;

public class UserScopeDto
{
    public bool GlobalAccess { get; set; }
    public IReadOnlyList<string> SpaceRootPaths { get; set; } = [];
}

public record UserScopeAccountDto(bool HasGlobalAccess);
