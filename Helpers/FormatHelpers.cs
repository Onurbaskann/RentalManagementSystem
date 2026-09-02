using System.Globalization;
using KiraTakip.Models.Settings;

namespace KiraTakip.Helpers;

public static class FormatHelpers
{
    private static readonly CultureInfo TrCulture = new("tr-TR");
    private static readonly Lazy<TimeZoneInfo> TurkeyTimeZone = new(ResolveTurkeyTimeZone);

    public static string Tl(this decimal value, int decimalPlaces = 2) =>
        value.ToString($"N{decimalPlaces}", TrCulture) + " ₺";

    public static string Tl(this decimal? value, int decimalPlaces = 2) =>
        value.HasValue ? value.Value.Tl(decimalPlaces) : "—";

    public static string Tl(this double value, int decimalPlaces = 2) =>
        value.ToString($"N{decimalPlaces}", TrCulture) + " ₺";

    public static string Tl(this double? value, int decimalPlaces = 2) =>
        value.HasValue ? value.Value.Tl(decimalPlaces) : "—";

    public static string Tl(this int value) =>
        value.ToString("N0", TrCulture) + " ₺";

    public static string Tl(this int? value) =>
        value.HasValue ? value.Value.Tl() : "—";

    public static string M2(this decimal value, int decimalPlaces = 0) =>
        value.ToString($"N{decimalPlaces}", TrCulture) + " m²";

    public static string M2(this decimal? value, int decimalPlaces = 0) =>
        value.HasValue ? value.Value.M2(decimalPlaces) : "—";

    public static string M2(this double value, int decimalPlaces = 0) =>
        value.ToString($"N{decimalPlaces}", TrCulture) + " m²";

    public static string M2(this double? value, int decimalPlaces = 0) =>
        value.HasValue ? value.Value.M2(decimalPlaces) : "—";

    public static string M2(this int value) =>
        value.ToString("N0", TrCulture) + " m²";

    public static string M2(this int? value) =>
        value.HasValue ? value.Value.M2() : "—";

    public static string Tarih(this DateTime value) =>
        value.ToString("dd.MM.yyyy", TrCulture);

    public static string Tarih(this DateTime? value) =>
        value.HasValue ? value.Value.Tarih() : "—";

    public static DateTime TurkiyeSaati(this DateTime utcValue)
    {
        var utc = utcValue.Kind == DateTimeKind.Utc
            ? utcValue
            : DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TurkeyTimeZone.Value);
    }

    public static string TurkiyeTarihSaat(this DateTime utcValue) =>
        utcValue.TurkiyeSaati().ToString("dd.MM.yyyy HH:mm", TrCulture);

    public static string TurkiyeTarihSaat(this DateTime? utcValue) =>
        utcValue.HasValue ? utcValue.Value.TurkiyeTarihSaat() : "—";

    public static string Yuzde(this decimal value, int decimalPlaces = 2) =>
        FormatPercentage(value.ToString($"N{decimalPlaces}", TrCulture), decimalPlaces);

    public static string Yuzde(this decimal? value, int decimalPlaces = 2) =>
        value.HasValue ? value.Value.Yuzde(decimalPlaces) : "—";

    public static string Yuzde(this double value, int decimalPlaces = 2) =>
        FormatPercentage(value.ToString($"N{decimalPlaces}", TrCulture), decimalPlaces);

    public static string Yuzde(this double? value, int decimalPlaces = 2) =>
        value.HasValue ? value.Value.Yuzde(decimalPlaces) : "—";

    private static string FormatPercentage(string formattedValue, int decimalPlaces) =>
        "%" + (decimalPlaces > 0 ? formattedValue.TrimEnd('0').TrimEnd(',') : formattedValue);

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ReservationPolicySettings.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
    }
}
