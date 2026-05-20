using System.Text;
using System.Text.RegularExpressions;

namespace KiraTakip.Extensions;

public static class StringExtensions
{
    private static readonly Regex BorcTipiKodRegex = new(
        @"^[a-zA-Z0-9_\u00C7\u011E\u0130\u00D6\u015E\u00DC\u00E7\u011F\u0131\u00F6\u015F\u00FC\s]{2,50}$",
        RegexOptions.Compiled);

    public static bool IsValidBorcTipiKod(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return BorcTipiKodRegex.IsMatch(value);
    }
    public static string ToSafeCode(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var sb = new StringBuilder();
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c)) continue;

            var upper = char.ToUpperInvariant(c);
            sb.Append(upper switch
            {
                'Ç' => 'C',
                'Ğ' => 'G',
                'İ' => 'I',
                'Ö' => 'O',
                'Ş' => 'S',
                'Ü' => 'U',
                _ => upper
            });
        }

        var result = sb.ToString();
        // Sadece harf, rakam ve alt çizgi kalsın (güvenlik için)
        var final = new StringBuilder();
        foreach (var c in result)
        {
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')
            {
                final.Append(c);
            }
        }

        return final.ToString();
    }
}
