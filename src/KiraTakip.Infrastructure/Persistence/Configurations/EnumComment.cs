namespace KiraTakip.Data.Configurations;

internal static class EnumComment
{
    internal static string For<TEnum>() where TEnum : struct, Enum
        => string.Join(", ", Enum.GetValues<TEnum>().Select(value => $"{value}={(int)(object)value}"));
}
