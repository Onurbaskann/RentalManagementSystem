using System.Globalization;

namespace KiraTakip.Helpers;

public static class FormatHelpers
{
    private static readonly CultureInfo TrCulture = new("tr-TR");

    public static string Tl(this decimal value) =>
        value.ToString("N0", TrCulture) + " ₺";

    public static string Tl(this decimal? value) =>
        value.HasValue ? value.Value.Tl() : "—";

    public static string M2(this decimal value) =>
        value.ToString("N0", TrCulture) + " m²";

    public static string Tarih(this DateTime value) =>
        value.ToString("dd.MM.yyyy", TrCulture);

    public static string Tarih(this DateTime? value) =>
        value.HasValue ? value.Value.Tarih() : "—";

    public static string Yuzde(this decimal value) =>
        "%" + value.ToString("N2", TrCulture).TrimEnd('0').TrimEnd(',');

    public static string Yuzde(this decimal? value) =>
        value.HasValue ? value.Value.Yuzde() : "—";
}
