using System.Text;

namespace KiraTakip.Helpers;

public static class CodeSlugger
{
    private static readonly Dictionary<char, char> TurkishMap = new()
    {
        ['ç'] = 'C', ['Ç'] = 'C',
        ['ğ'] = 'G', ['Ğ'] = 'G',
        ['ı'] = 'I', ['İ'] = 'I',
        ['ö'] = 'O', ['Ö'] = 'O',
        ['ş'] = 'S', ['Ş'] = 'S',
        ['ü'] = 'U', ['Ü'] = 'U',
    };

    /// <summary>
    /// Adı sistem kodu formatına çevirir.
    /// Örnek: "Ofis Kira Bedeli" → "OFIS_KIRA_BEDELI"
    /// </summary>
    public static string ToCode(string? ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return string.Empty;

        var sb = new StringBuilder(ad.Length);
        foreach (var ch in ad.Trim())
        {
            if (TurkishMap.TryGetValue(ch, out var mapped))
                sb.Append(mapped);
            else if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToUpperInvariant(ch));
            else
                sb.Append('_');
        }

        // Ardışık _ tekrarlarını teke indir, baş/son _'leri kırp
        var raw = sb.ToString();
        while (raw.Contains("__")) raw = raw.Replace("__", "_");
        return raw.Trim('_');
    }
}
