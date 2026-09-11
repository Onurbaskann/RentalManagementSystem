using KiraTakip.Data;
using KiraTakip.Models.Common;
using KiraTakip.Web.Models.Common;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Auditing;
using KiraTakip.Repositories.Catalog;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Documents;
using KiraTakip.Repositories.Identity;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Repositories.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

public class ServerSidePaginationUiTests
{
    private static readonly string ProjectRoot = FindProjectRoot();

    [Fact]
    public void PaginatedIndexes_ShouldUseServerPaginationWithoutClientTable()
    {
        string[] relativePaths =
        [
            "Views/Lease/Index.cshtml",
            "Views/Tenant/Index.cshtml",
            "Views/TenantLease/Index.cshtml",
            "Views/TenantReservation/Index.cshtml",
            "Views/AdminUser/Index.cshtml",
            "Views/TenantUser/Index.cshtml",
            "Views/AdminTenantUser/Index.cshtml",
            "Views/Reservation/Index.cshtml",
            "Views/ManualCharge/Index.cshtml",
            "Views/Property/Index.cshtml",
            "Views/AdminAuditLog/Index.cshtml",
            "Views/Charge/Index.cshtml",
            "Views/TenantCharge/Index.cshtml",
            "Views/Payment/Index.cshtml",
            "Views/BankTransaction/Index.cshtml",
            "Views/AdminChargeType/Index.cshtml",
            "Views/AdminDocumentType/Index.cshtml",
            "Views/AdminPropertyType/Index.cshtml",
            "Views/AdminUnitType/Index.cshtml",
            "Views/AdminSector/Index.cshtml",
            "Views/AdminTenantCategory/Index.cshtml",
            "Views/AdminRate/Index.cshtml",
            "Views/AdminReservationRateRule/Index.cshtml",
            "Views/AdminRole/Index.cshtml",
            "Views/TenantRole/Index.cshtml"
        ];

        foreach (var relativePath in relativePaths)
        {
            var content = File.ReadAllText(Path.Combine(ProjectRoot, relativePath));
            Assert.DoesNotContain("clientTable", content);
            Assert.Contains("_Pagination", content);
        }

        Assert.DoesNotContain(
            "clientTable",
            File.ReadAllText(Path.Combine(ProjectRoot, "wwwroot/js/site.js")));

        var paginationPartial = File.ReadAllText(Path.Combine(ProjectRoot, "Views/Shared/_Pagination.cshtml"));
        Assert.Contains("UrlForSize", paginationPartial);
        Assert.Contains("10, 25, 50, 100, 200", paginationPartial);

        var reservationView = File.ReadAllText(Path.Combine(ProjectRoot, "Views/Reservation/Index.cshtml"));
        Assert.Contains("onaylandi", reservationView);
        Assert.Contains("onaybekliyor", reservationView);
        Assert.Contains("tamamlandi", reservationView);
        Assert.Contains("reddedildi", reservationView);
        Assert.Contains("name=\"status\"", reservationView);
    }

    [Fact]
    public void RepositoryPagination_ShouldUseSharedInfrastructure()
    {
        var repositoriesPath = Path.Combine(ProjectRoot, "src", "KiraTakip.Infrastructure", "Persistence", "Repositories");
        if (!Directory.Exists(repositoriesPath))
        {
            repositoriesPath = Path.GetFullPath(Path.Combine(ProjectRoot, "..", "KiraTakip.Infrastructure", "Persistence", "Repositories"));
        }
        var baseRepository = File.ReadAllText(Path.Combine(repositoriesPath, "Common", "Repository.cs"));
        var pagedQuery = File.ReadAllText(Path.Combine(repositoriesPath, "Common", "PagedQuery.cs"));

        Assert.Contains("GetPagedResultAsync", baseRepository);
        Assert.Contains("Math.Clamp(page, 1, totalPages)", pagedQuery);
        Assert.Contains("total == 0", pagedQuery);

        var repositoryFiles = Directory.GetFiles(repositoriesPath, "*Repository.cs", SearchOption.AllDirectories)
            .Where(path => !string.Equals(
                Path.GetFileName(path),
                "Repository.cs",
                StringComparison.OrdinalIgnoreCase));

        foreach (var repositoryFile in repositoryFiles)
        {
            var content = File.ReadAllText(repositoryFile);
            Assert.DoesNotContain("new PagedResult<", content);
            Assert.DoesNotContain(".Skip(", content);
        }

        Assert.Contains("ThenBy(user => user.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Identity", "ApplicationUserRepository.cs")));
        Assert.Contains("ThenByDescending(lease => lease.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Leases", "LeaseRepository.cs")));
        Assert.Contains("ThenByDescending(reservation => reservation.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Reservations", "ReservationRepository.cs")));
        Assert.Contains("ThenBy(property => property.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Properties", "PropertyRepository.cs")));
        Assert.Contains("ThenBy(tenant => tenant.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Tenants", "TenantRepository.cs")));
        Assert.Contains("ThenByDescending(charge => charge.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Charges", "ChargeRepository.cs")));
        Assert.Contains("ThenByDescending(o => o.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Payments", "PaymentAllocationRepository.cs")));
        Assert.Contains("ThenByDescending(transaction => transaction.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Banking", "BankTransactionRepository.cs")));
        Assert.Contains("ThenByDescending(a => a.Id)", File.ReadAllText(Path.Combine(repositoriesPath, "Auditing", "AuditLogRepository.cs")));
    }

    [Fact]
    public void PaginationUrl_ShouldWritePageAndSizeOnlyOnce()
    {
        var pagination = new PaginationModel
        {
            Page = 1,
            Size = 20,
            Total = 100,
            BasePath = "/Test",
            Extra = new Dictionary<string, string?>
            {
                ["size"] = "20",
                ["q"] = "arama"
            }
        };

        var url = pagination.Url(2);

        Assert.Equal(1, url.Split("size=20").Length - 1);
        Assert.Contains("page=2", url);
        Assert.Contains("q=arama", url);

        var sizeUrl = pagination.UrlForSize(50);
        Assert.Contains("page=1", sizeUrl);
        Assert.Contains("size=50", sizeUrl);
        Assert.Contains("q=arama", sizeUrl);
        Assert.DoesNotContain("size=20", sizeUrl);
    }

    private static string FindProjectRoot()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory != null)
            {
                var webCandidate = Path.Combine(directory.FullName, "src", "KiraTakip.Web", "KiraTakip.Web.csproj");
                if (File.Exists(webCandidate)) return Path.GetDirectoryName(webCandidate)!;

                var directCandidate = Path.Combine(directory.FullName, "KiraTakip.Web.csproj");
                if (File.Exists(directCandidate)) return Path.GetDirectoryName(directCandidate)!;

                directory = directory.Parent;
            }
        }

        throw new DirectoryNotFoundException("KiraTakip.Web.csproj proje yolu bulunamadı.");
    }
}

[Collection("Database collection")]
public class ServerSidePaginationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ServerSidePaginationRepositoryTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task RepositoryPages_ShouldExecuteCountSearchSkipAndTakeOnSqlServer()
    {
        var pageQuery = new TableQuery { Page = 1, Size = 2 };
        var searchQuery = new TableQuery { Page = 1, Size = 2, Q = "a" };

        var leases = await new LeaseRepository(_context)
            .GetPagedListAsync(pageQuery, "tum", null);
        var searchedLeases = await new LeaseRepository(_context)
            .GetPagedListAsync(searchQuery, "tum", null);
        var tenants = await new TenantRepository(_context)
            .GetPagedListAsync(pageQuery, null);
        var searchedTenants = await new TenantRepository(_context)
            .GetPagedListAsync(searchQuery, null);
        var scopedUnitId = await _context.Leases
            .Select(lease => lease.UnitId)
            .FirstAsync();
        var scopedTenants = await new TenantRepository(_context)
            .GetPagedListAsync(pageQuery, [], [scopedUnitId]);

        var tenantId = await _context.Tenants
            .Select(tenant => tenant.Id)
            .FirstAsync();
        var tenantLeases = await new LeaseRepository(_context)
            .GetTenantPortalPagedListAsync(tenantId, searchQuery);
        var tenantReservations = await new ReservationRepository(_context)
            .GetTenantPagedListAsync(tenantId, searchQuery);
        var reservations = await new ReservationRepository(_context)
            .GetPagedListAsync(searchQuery, null);
        var completedReservations = await new ReservationRepository(_context)
            .GetPagedListAsync(new TableQuery { Page = 1, Size = 2, Status = "tamamlandi" }, null);
        var properties = await new PropertyRepository(_context)
            .GetPagedListAsync(searchQuery, null);
        var lastPropertyPage = await new PropertyRepository(_context)
            .GetPagedListAsync(new TableQuery { Page = int.MaxValue, Size = 2 }, null);
        var manualCharges = await new ChargeRepository(_context)
            .GetManualChargePagedListAsync(searchQuery, null);
        var auditLogs = await new AuditLogRepository(_context)
            .QueryAsync(null, null, null, null, null, pageQuery);
        var chargeTypes = await new ChargeTypeRepository(_context).GetPagedListAsync(searchQuery);
        var documentTypes = await new DocumentTypeRepository(_context).GetPagedListAsync(searchQuery);
        var propertyTypes = await new PropertyTypeRepository(_context).GetPagedListAsync(searchQuery);
        var unitTypes = await new UnitTypeRepository(_context).GetPagedListAsync(searchQuery);
        var sectors = await new CategoryRepository(_context)
            .GetPagedListByTypeAsync(Models.Entities.CategoryType.Sector, searchQuery);
        var tenantCategories = await new CategoryRepository(_context)
            .GetPagedListByTypeAsync(Models.Entities.CategoryType.Tenant, searchQuery);
        var rateYears = await new RateScheduleRepository(_context).GetYearSummariesPagedAsync(pageQuery);
        var rateRules = await new ReservationRateOverrideRepository(_context).GetRateRulesPagedAsync(searchQuery);
        var roles = new RoleRepository(_context);
        var internalRoles = await roles.GetInternalRolesWithDetailsPagedAsync(searchQuery);
        var tenantRoles = await roles.GetTenantRolesWithDetailsPagedAsync(tenantId, searchQuery);

        var users = new ApplicationUserRepository(_context);
        var internalUsers = await users.GetInternalAdminUsersPageAsync(searchQuery);
        var tenantUsers = await users.GetAdminTenantUsersPageAsync(searchQuery);
        var tenantUserPage = await users.GetTenantUserPageAsync(tenantId, searchQuery);

        AssertPage(leases);
        AssertPage(searchedLeases);
        AssertPage(tenants);
        AssertPage(searchedTenants);
        AssertPage(scopedTenants);
        AssertPage(tenantLeases);
        AssertPage(tenantReservations);
        AssertPage(reservations);
        AssertPage(completedReservations);
        AssertPage(properties);
        Assert.Equal(Math.Max(1, lastPropertyPage.TotalPages), lastPropertyPage.Page);
        Assert.InRange(lastPropertyPage.Items.Count, 0, 2);
        AssertPage(manualCharges);
        AssertPage(auditLogs);
        AssertPage(chargeTypes);
        AssertPage(documentTypes);
        AssertPage(propertyTypes);
        AssertPage(unitTypes);
        AssertPage(sectors);
        AssertPage(tenantCategories);
        AssertPage(rateYears);
        AssertPage(rateRules);
        AssertPage(internalRoles);
        AssertPage(tenantRoles);
        AssertPage(internalUsers);
        AssertPage(tenantUsers);
        AssertPage(tenantUserPage);
    }

    private static void AssertPage<T>(PagedResult<T> result)
    {
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.Size);
        Assert.InRange(result.Items.Count, 0, 2);
        Assert.True(result.Total >= result.Items.Count);
    }
}
