using KiraTakip.Models.Enums;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KiraTakip.Data;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var workingDirectory = Directory.GetCurrentDirectory();
        var currentDir = new DirectoryInfo(workingDirectory);
        string? projectDirectory = null;
        while (currentDir != null)
        {
            if (File.Exists(Path.Combine(currentDir.FullName, "appsettings.json")))
            {
                projectDirectory = currentDir.FullName;
                break;
            }
            if (Directory.Exists(Path.Combine(currentDir.FullName, "KiraTakip")) &&
                File.Exists(Path.Combine(currentDir.FullName, "KiraTakip", "appsettings.json")))
            {
                projectDirectory = Path.Combine(currentDir.FullName, "KiraTakip");
                break;
            }
            currentDir = currentDir.Parent;
        }
        projectDirectory ??= workingDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(
            options,
            new HttpContextAccessor(),
            new DesignTimeCurrentUserContext());
    }

    private sealed class DesignTimeCurrentUserContext : ICurrentUserContext
    {
        public string? UserId => null;
        public UserType? UserType => null;
        public int? TenantId => null;
        public bool IsKiraciUser => false;
    }
}
