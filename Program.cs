using KiraTakip;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Infrastructure.Transactions;

var builder = WebApplication.CreateBuilder(args);

// Register Dependency Modules
builder.Services.AddInfrastructureModule(builder.Configuration);
builder.Services.AddIdentityModule();
builder.Services.AddRepositoryModule();
builder.Services.AddServiceModule();
builder.Services.AddValidationModule();
builder.Services.AddBusinessRulesModule();
builder.Services.AddWebModule();

// ITransactionalService implement eden tüm servisleri otomatik transaction proxy ile sar.
// Bu çağrı TÜM AddScoped/AddTransient/AddSingleton register'larından SONRA olmalıdır.
builder.Services.AddTransactionalProxies();

var app = builder.Build();

HashidsExtensions.Configure(app.Services);
app.UseTurkishCulture();

// Production: teknik detaylar gizlenir, kullanıcı dostu hata sayfası gösterilir.
// Development: hata ayıklama için detaylı developer exception page açılır.
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

await app.SeedAsync();
await app.InitializeSystemSettingsAsync();

app.Run();
