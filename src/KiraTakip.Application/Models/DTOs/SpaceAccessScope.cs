namespace KiraTakip.Models.Dtos;

public sealed record SpaceAccessScope(bool HasGlobalAccess, IReadOnlyList<string> RootPaths)
{
    public bool Includes(string spacePath)
    {
        if (HasGlobalAccess)
            return true;
        if (string.IsNullOrEmpty(spacePath))
            return false;

        var path = spacePath.EndsWith('/') ? spacePath : spacePath + "/";
        foreach (var root in RootPaths)
        {
            if (string.IsNullOrEmpty(root))
                continue;
            var prefix = root.EndsWith('/') ? root : root + "/";
            if (path.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
