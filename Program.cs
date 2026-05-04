using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
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
        options.AddPolicy(perm, policy => policy.RequireClaim("permission", perm));
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IdentitySeedService>();
builder.Services.AddScoped<UserTasinmazYetkiService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, PermissionClaimsTransformer>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminBypassHandler>();
builder.Services.AddScoped<ITasinmazService, TasinmazService>();
builder.Services.AddScoped<IBirimService, BirimService>();
builder.Services.AddScoped<IKiraciService, KiraciService>();
builder.Services.AddScoped<ISozlesmeService, SozlesmeService>();
builder.Services.AddScoped<IIstatistikService, IstatistikService>();
builder.Services.AddScoped<SeedDataService>();
builder.Services.AddScoped<ITahakkukService, TahakkukService>();
builder.Services.AddScoped<IOdemeService, OdemeService>();
builder.Services.AddScoped<IDekontService, DekontService>();
builder.Services.AddScoped<IBankaHareketiService, BankaHareketiService>();
builder.Services.AddSingleton<IBankaHareketiParser, AkbankCsvParser>();

var app = builder.Build();

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

    if (app.Environment.IsDevelopment())
    {
        var domainSeed = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await domainSeed.SeedDomainDataAsync();
    }
}

app.Run();
