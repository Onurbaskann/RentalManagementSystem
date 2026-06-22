using KiraTakip.Infrastructure;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class MaskingService : IMaskingService
{
    public string? Mask(string? value, MaskType maskType)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return maskType switch
        {
            MaskType.Email    => MaskEmail(value),
            MaskType.Telefon  => MaskTelefon(value),
            MaskType.TcKimlik => MaskFixed(value, 3, 3),
            MaskType.VergiNo  => MaskFixed(value, 3, 4),
            _                 => value
        };
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        var local = email[..at];
        var domain = email[(at + 1)..];
        var dot = domain.LastIndexOf('.');
        var maskedLocal = local[0] + new string('*', Math.Max(2, local.Length - 1));
        var maskedDomain = dot > 0
            ? domain[0] + new string('*', Math.Max(1, dot - 1)) + domain[dot..]
            : domain[0] + "***";
        return $"{maskedLocal}@{maskedDomain}";
    }

    private static string MaskTelefon(string tel)
    {
        tel = new string(tel.Where(char.IsDigit).ToArray());
        if (tel.Length < 6) return "***";
        return tel[..4] + new string('*', tel.Length - 7) + tel[^3..];
    }

    // keep first `prefix` and last `suffix` chars, mask middle
    private static string MaskFixed(string value, int prefix, int suffix)
    {
        if (value.Length <= prefix + suffix) return value;
        return value[..prefix] + new string('*', value.Length - prefix - suffix) + value[^suffix..];
    }
}
