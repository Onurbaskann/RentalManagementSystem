using KiraTakip.Data;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;

namespace KiraTakip.Tests;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

public class DatabaseFixture : IDisposable
{
    public string ConnectionString { get; }

    public DatabaseFixture()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        var configuredConnectionString = config.GetConnectionString("DefaultConnection")!;
        var connectionBuilder = new SqlConnectionStringBuilder(configuredConnectionString);
        var databaseName = connectionBuilder.InitialCatalog;
        if (!string.Equals(databaseName, "KiraTakipDb_Test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Entegrasyon testleri yalnızca KiraTakipDb_Test veritabanında çalıştırılabilir. Yapılandırılan veritabanı: {databaseName}");
        }

        // Uzak test SQL Server'ın eski TLS yapılandırması yalnız test çalıştırıcısında
        // Microsoft.Data.SqlClient'in varsayılan şifreleme davranışıyla uyuşmuyor.
        // Uygulama connection string'i değiştirilmeden yalnız doğrulanmış test DB bağlantısı uyarlanır.
        connectionBuilder.Encrypt = false;
        ConnectionString = connectionBuilder.ConnectionString;

        // Testler veritabanı şemasını kendiliğinden DEĞİŞTİRMEZ — yalnız güncel olduğunu doğrular.
        // Migration uygulamak geliştiricinin/CI'ın ayrı, bilinçli bir adımıdır:
        //   dotnet ef database update --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj
        //                             --startup-project src/KiraTakip.Web/KiraTakip.Web.csproj
        // (Önceden burada ctx.Database.Migrate() çağrılıyordu; bu, testleri çalıştırmanın paylaşılan test
        // veritabanının şemasını yan etki olarak değiştirmesine yol açıyordu ve kod modeli migration
        // geçmişiyle bir an için uyuşmadığında PendingModelChangesWarning tüm DB'ye dokunan testleri
        // anlaşılmaz biçimde çökertiyordu.)
        using var ctx = CreateContext();

        // (a) Koddaki bazı migration'lar bu DB'ye henüz uygulanmamış mı?
        var pendingMigrations = ctx.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Count > 0)
        {
            throw new InvalidOperationException(
                $"Test veritabanı (KiraTakipDb_Test) güncel değil — bekleyen migration(lar): " +
                $"{string.Join(", ", pendingMigrations)}. Testleri çalıştırmadan önce şu komutla güncelleyin: " +
                "dotnet ef database update --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj " +
                "--startup-project src/KiraTakip.Web/KiraTakip.Web.csproj");
        }

        // (b) Entity model'de migration'a hiç dökülmemiş bir değişiklik mi var (ör. bir property eklenip
        // "dotnet ef migrations add" unutulmuş)? Migrate() bu durumu PendingModelChangesWarning ile
        // anlaşılmaz biçimde tüm DB'ye dokunan testleri çökerterek yakalıyordu; burada erkenden, net bir
        // hatayla yakalanır. IMigrator.HasPendingModelChanges() — Migrate()'in kendi içinde kullandığı
        // aynı kontrol, yalnız DB'yi değiştirmeden.
        var migrator = ctx.GetService<IMigrator>();
        if (migrator.HasPendingModelChanges())
        {
            throw new InvalidOperationException(
                "Entity model'de henüz migration'a dökülmemiş değişiklikler var. Yeni bir migration oluşturun: " +
                "dotnet ef migrations add <İsim> --project src/KiraTakip.Infrastructure/KiraTakip.Infrastructure.csproj " +
                "--startup-project src/KiraTakip.Web/KiraTakip.Web.csproj");
        }
    }

    public ApplicationDbContext CreateContext(ICurrentUserContext? currentUser = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ApplicationDbContext(
            options,
            new DummyHttpContextAccessor(),
            currentUser ?? new DummyCurrentUserContext());
    }

    public void Dispose() { }
}
