using KiraTakip.Common;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Hashids;
using KiraTakip.Infrastructure.Notifications;
using KiraTakip.Web.Context;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Models.Settings;
using KiraTakip.Models.ViewModels;
using KiraTakip.Repositories.Charges;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using KiraTakip.Models.Dtos.ChargeReminder;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class ChargeReminderServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ChargeReminderServiceTests(DatabaseFixture fixture)
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

    private sealed class FakeMailService : IMailService
    {
        public string? ToAddress { get; private set; }
        public string? ToName { get; private set; }

        public Task SendAsync(string toAddress, string toName, string subject, string htmlBody, CancellationToken ct = default)
        {
            ToAddress = toAddress;
            ToName = toName;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRazorViewToStringRenderer : IRazorViewToStringRenderer
    {
        public TenantDebtReminderEmailViewModel? CapturedModel { get; private set; }

        public Task<string> RenderAsync<TModel>(string viewName, TModel model)
        {
            CapturedModel = model as TenantDebtReminderEmailViewModel;
            return Task.FromResult("<html></html>");
        }
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string scheme, string host)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = scheme;
        httpContext.Request.Host = new HostString(host);
        return new HttpContextAccessor { HttpContext = httpContext };
    }

    private ChargeReminderService CreateService(
        FakeMailService mailService,
        FakeRazorViewToStringRenderer renderer,
        IHttpContextAccessor httpContextAccessor)
    {
        return new(new ChargeRepository(_context),
                   new UnitOfWork(_context),
                   new HttpRequestContext(httpContextAccessor),
                   mailService,
                   renderer,
                   NullLogger<ChargeReminderService>.Instance,
                   new TestOperationalPolicyProvider(),
                   new SmtpConfigurationValidator(Options.Create(new SmtpSettings { Host = "smtp.test.local", From = "no-reply@test.local" })),
                   new HashIdEncoder(new HashidsNet.Hashids()));
    }

    private async Task<(Property Property, Unit Unit, Tenant Tenant, Charge Charge)> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Hatırlatma Plaza {suffix}", City = "İstanbul", District = "Kadıköy" };
        var unitType = new UnitType
        {
            Name = $"Hatırlatma Ofis {suffix}",
            Code = $"REM_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant
        {
            Name = $"Hatırlatma Kiracı {suffix}",
            TenantNo = $"REM-{suffix}",
            Email = $"reminder-{suffix}@test.local"
        };

        _context.Properties.Add(property);
        _context.UnitTypes.Add(unitType);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Ofis {suffix}", Area = 50 };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var charge = new Charge
        {
            TenantId = tenant.Id,
            UnitId = unit.Id,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = DateTime.Today.AddDays(3),
            ExpectedAmount = 1000m,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            Status = ChargeStatus.Pending
        };
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        return (property, unit, tenant, charge);
    }

    [Fact]
    public async Task SendDebtRemindersAsync_GeneratesAuthenticatedTenantChargeDeepLink()
    {
        var seed = await SeedAsync();
        var mailService = new FakeMailService();
        var renderer = new FakeRazorViewToStringRenderer();
        var service = CreateService(
            mailService,
            renderer,
            CreateHttpContextAccessor("https", "kiratakip.example.com"));

        await service.SendDebtRemindersAsync(new ChargeReminderScopeInput(UnitIds: [seed.Unit.Id]));

        Assert.NotNull(renderer.CapturedModel);
        var debt = Assert.Single(renderer.CapturedModel!.Debts, d => d.PropertyName == seed.Property.Name);
        Assert.StartsWith("https://kiratakip.example.com/Tenant/Charges/Details/", debt.ChargeDetailsUrl);
        Assert.DoesNotContain("/Payment/Portal", debt.ChargeDetailsUrl);
    }

    [Fact]
    public async Task SendDebtRemindersAsync_StillSendsToTenantEmail()
    {
        var seed = await SeedAsync();
        var mailService = new FakeMailService();
        var renderer = new FakeRazorViewToStringRenderer();
        var service = CreateService(
            mailService,
            renderer,
            CreateHttpContextAccessor("https", "kiratakip.example.com"));

        await service.SendDebtRemindersAsync(new ChargeReminderScopeInput(UnitIds: [seed.Unit.Id]));

        Assert.Equal(seed.Tenant.Email, mailService.ToAddress);
    }

    [Fact]
    public async Task SendDebtRemindersAsync_NoHttpContext_FallsBackToLocalhost()
    {
        var seed = await SeedAsync();
        var mailService = new FakeMailService();
        var renderer = new FakeRazorViewToStringRenderer();
        var service = CreateService(
            mailService,
            renderer,
            new HttpContextAccessor());

        await service.SendDebtRemindersAsync(new ChargeReminderScopeInput(UnitIds: [seed.Unit.Id]));

        var debt = Assert.Single(renderer.CapturedModel!.Debts, d => d.PropertyName == seed.Property.Name);
        Assert.StartsWith("http://localhost:5031/Tenant/Charges/Details/", debt.ChargeDetailsUrl);
    }
}
