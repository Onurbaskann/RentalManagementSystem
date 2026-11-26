using System.Globalization;

namespace KiraTakip.Domain.Spaces;

public static class SpaceTreePolicy
{
    public const int MaxDepth = 19;

    private const char Separator = '/';

    public static string BuildPath(string? parentPath, int id)
        => string.Create(CultureInfo.InvariantCulture, $"{(string.IsNullOrEmpty(parentPath) ? "/" : parentPath)}{id}/");

    public static IReadOnlyList<int> GetAncestorIds(string path)
    {
        var ids = ParseIds(path);
        return ids.Count <= 1 ? [] : ids.Take(ids.Count - 1).ToList();
    }

    public static bool IsAncestorOrSelf(string ancestorPath, string path)
        => path.StartsWith(ancestorPath, StringComparison.Ordinal);

    public static bool CanAddChild(int parentDepth)
        => parentDepth >= 0 && parentDepth < MaxDepth;

    public static IReadOnlyList<(int A, int B)> FindNestedPairs(IEnumerable<(int Id, string Path)> spaces)
    {
        var list = spaces.ToList();
        var pairs = new List<(int A, int B)>();

        for (var i = 0; i < list.Count; i++)
        {
            for (var j = 0; j < list.Count; j++)
            {
                if (i == j || list[i].Id == list[j].Id)
                    continue;

                if (IsAncestorOrSelf(list[i].Path, list[j].Path))
                    pairs.Add((list[i].Id, list[j].Id));
            }
        }

        return pairs;
    }

    public static int GetRootId(string path)
    {
        var ids = ParseIds(path);
        if (ids.Count == 0)
            throw new ArgumentException("Yol geçerli bir alan kimliği içermiyor.", nameof(path));
        return ids[0];
    }

    private static List<int> ParseIds(string path)
        => path.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
               .Select(s => int.Parse(s, CultureInfo.InvariantCulture))
               .ToList();
}
