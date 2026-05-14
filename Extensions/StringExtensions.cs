using System.Text;

namespace KiraTakip.Extensions;

public static class StringExtensions
{
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
