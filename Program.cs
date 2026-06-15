using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services;
using KiraTakip.Services.Banka;
using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization(options =>
{
    foreach (var perm in PermissionCatalog.All)
        options.AddPolicy(perm, policy => policy.RequireClaim(AppClaimTypes.Permission, perm));
});

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new KiraTakip.Infrastructure.InvariantDecimalModelBinderProvider());
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => $"'{x}' değeri '{y}' alanı için geçersizdir.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor((x) => $"'{x}' alanı için bir değer belirtilmelidir.");
    options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "Bir değer girilmelidir.");
    options.ModelBindingMessageProvider.SetMissingRequestBodyRequiredValueAccessor(() => "İstek gövdesi boş olamaz.");
    options.ModelBindingMessageProvider.SetNonPropertyAttemptedValueIsInvalidAccessor((x) => $"'{x}' değeri geçersizdir.");
    options.ModelBindingMessageProvider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Geçersiz değer.");
    options.ModelBindingMessageProvider.SetNonPropertyValueMustBeANumberAccessor(() => "Alan sayı olmalıdır.");
    options.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor((x) => $"'{x}' alanı için değer geçersizdir.");
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor((x) => $"'{x}' değeri geçersizdir.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor((x) => $"'{x}' alanı sayı olmalıdır.");
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor((x) => "Bu alan boş bırakılamaz.");
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddScoped<IdentitySeedService>();
builder.Services.AddScoped<IUserTasinmazYetkiService, UserTasinmazYetkiService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, PermissionClaimsTransformer>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminBypassHandler>();
builder.Services.AddScoped<ITasinmazService, TasinmazService>();
builder.Services.AddScoped<IBirimService, BirimService>();
builder.Services.AddScoped<IKiraciService, KiraciService>();
builder.Services.AddScoped<ISozlesmeService, SozlesmeService>();
builder.Services.AddScoped<IIstatistikService, IstatistikService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IKiraciRepository, KiraciRepository>();
builder.Services.AddScoped<ITasinmazRepository, TasinmazRepository>();
builder.Services.AddScoped<IBirimRepository, BirimRepository>();
builder.Services.AddScoped<ISozlesmeRepository, SozlesmeRepository>();
builder.Services.AddScoped<ITahakkukRepository, TahakkukRepository>();
builder.Services.AddScoped<IOdemeRepository, OdemeRepository>();
builder.Services.AddScoped<IBankaHareketiRepository, BankaHareketiRepository>();
builder.Services.AddScoped<IDekontRepository, DekontRepository>();
builder.Services.AddScoped<ITasinmazTarifeRepository, TasinmazTarifeRepository>();
builder.Services.AddScoped<IBorcTipiRepository, BorcTipiRepository>();
builder.Services.AddScoped<ISozlesmeTarifeRepository, SozlesmeTarifeRepository>();
builder.Services.AddScoped<IBirimTarifeRepository, BirimTarifeRepository>();
builder.Services.AddScoped<IGenelTarifeRepository, GenelTarifeRepository>();
builder.Services.AddScoped<IRezervasyonTarifeRepository, RezervasyonTarifeRepository>();
builder.Services.AddScoped<IBirimTuruRepository, BirimTuruRepository>();
builder.Services.AddScoped<IKategoriRepository, KategoriRepository>();
builder.Services.AddScoped<IRezervasyonRepository, RezervasyonRepository>();
builder.Services.AddScoped<IUserTasinmazYetkiRepository, UserTasinmazYetkiRepository>();
builder.Services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
builder.Services.AddScoped<ITahakkukService, TahakkukService>();
builder.Services.AddScoped<IOdemeService, OdemeService>();
builder.Services.AddScoped<IDekontService, DekontService>();
builder.Services.AddScoped<IBankaHareketiService, BankaHareketiService>();
builder.Services.AddSingleton<IBankaHareketiParser, AkbankCsvParser>();
builder.Services.AddScoped<IRateResolverService, RateResolverService>();
builder.Services.AddScoped<ITahakkukUretimService, TahakkukUretimService>();
builder.Services.AddScoped<IManuelBorcService, ManuelBorcService>();
builder.Services.AddScoped<IRezervasyonService, RezervasyonService>();
builder.Services.AddScoped<ITasinmazFiyatService, TasinmazFiyatService>();
builder.Services.AddScoped<ITarifeHiyerarsiService, TarifeHiyerarsiService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<PaymentLinkSettings>(builder.Configuration.GetSection("PaymentLink"));
builder.Services.AddScoped<IMailService, SmtpMailService>();
builder.Services.AddScoped<IPaymentLinkService, PaymentLinkService>();
builder.Services.AddScoped<IBorcHatirlatmaService, BorcHatirlatmaService>();
builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

var cultureInfo = new System.Globalization.CultureInfo("tr-TR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var seedService = scope.ServiceProvider.GetRequiredService<IdentitySeedService>();
    await seedService.SeedAsync();

    var domainSeed = scope.ServiceProvider.GetRequiredService<SeedDataService>();

    if (app.Environment.IsDevelopment())
    {
        // [ANTIGRAVITY-TRIGGER]: Veri tabanını sıfırlayıp yeni seed verileriyle temiz bir başlangıç yapmak için aşağıdaki satırı aktif edin.
        // await domainSeed.ClearDomainDataAsync();
    }
    // Sistem tanımları — her ortamda idempotent çalışır
    await domainSeed.SeedEnumDegerleriAsync();
    await domainSeed.SeedBorcTipleriAsync();
    await domainSeed.SeedTasinmazTipleriAsync();
    await domainSeed.SeedBirimTurleriAsync();
    await domainSeed.SeedKiraciKategorileriAsync();
    await domainSeed.SeedSektorlerAsync();
    await domainSeed.SeedTarifelerAsync(); // Tarife.Yil oluşur
    await domainSeed.EnsureVarsayilanRezervasyonTarifeAsync();

    if (app.Environment.IsDevelopment())
    {
        await domainSeed.SeedTasinmazFiyatlarAsync();
        await domainSeed.SeedDomainDataAsync();
        await domainSeed.SeedTahakkuklarAsync();
    }
}

app.Run();
