using Microsoft.Data.SqlClient;

namespace KiraTakip.Data;

// alan-hiyerarsisi dalı yalnız DBTeknoPark_Test ile çalışır. main'e birleşirken kaldırılır (spec S-14, F6).
public static class BranchDatabaseGuard
{
    public const string AllowedDatabaseName = "DBTeknoPark_Test";

    public static void EnsureAllowed(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Bağlantı dizesi boş; alan-hiyerarsisi dalında veritabanı doğrulanamadı.");

        string databaseName;
        try
        {
            databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("Bağlantı dizesi çözümlenemedi; alan-hiyerarsisi dalında veritabanı doğrulanamadı.");
        }

        if (!string.Equals(databaseName, AllowedDatabaseName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"alan-hiyerarsisi dalı yalnız {AllowedDatabaseName} veritabanıyla çalışır. Yapılandırılan veritabanı: {databaseName}");
    }
}
