using System.Text.Json;

namespace KiraTakip.Auditing;

/// <summary>
/// Manuel audit detaylarını tutarlı bir JSON nesnesi olarak üretir.
/// </summary>
public static class AuditDetails
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T details)
    {
        ArgumentNullException.ThrowIfNull(details);
        return JsonSerializer.Serialize(details, Options);
    }

    public static void EnsureStructured(string details)
    {
        using var document = JsonDocument.Parse(details);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Audit detayı bir JSON nesnesi olmalıdır.", nameof(details));
    }
}
