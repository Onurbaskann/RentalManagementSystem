using KiraTakip.Authorization;
using KiraTakip.Web.Authorization;
using KiraTakip.Data;
using KiraTakip.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace KiraTakip.Web.DependencyInjection
{
    public static class IdentityModule
    {
        public static IServiceCollection AddIdentityModule(this IServiceCollection services)
        {
            services.AddAuthentication(IdentityConstants.ApplicationScheme)
                .AddIdentityCookies();

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<KiraTakip.Infrastructure.Identity.TurkishIdentityErrorDescriber>();

            services.Configure<SecurityStampValidatorOptions>(o =>
            {
                o.ValidationInterval = TimeSpan.FromMinutes(3);
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnValidatePrincipal = async context =>
                {
                    await SecurityStampValidator.ValidatePrincipalAsync(context);

                    if (context.Principal == null)
                        return;

                    var hasLegacyPermissionClaims = false;
                    foreach (var identity in context.Principal.Identities)
                    {
                        var legacyClaims = identity.Claims.Where(c => c.Type == AppClaimTypes.Permission).ToList();
                        if (legacyClaims.Count > 0)
                        {
                            hasLegacyPermissionClaims = true;
                            foreach (var claim in legacyClaims)
                            {
                                identity.RemoveClaim(claim);
                            }
                        }
                    }

                    if (hasLegacyPermissionClaims)
                    {
                        context.ShouldRenew = true;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                foreach (var m in PermissionCatalog.AllModules)
                {
                    var modulePath = m.Path;
                    options.AddPolicy(modulePath, policy => policy.AddRequirements(new PermissionRequirement(modulePath)));
                    foreach (var action in m.Actions)
                    {
                        var actionPath = action;
                        options.AddPolicy(actionPath, policy => policy.AddRequirements(new PermissionRequirement(actionPath)));
                    }
                }

                options.AddPolicy("TenantUser", policy =>
                    policy.RequireClaim(AppClaimTypes.UserType, ((int)UserType.Tenant).ToString()));
            });

            services.AddScoped<ICurrentUserPermissionContext, CurrentUserPermissionContext>();
            services.AddScoped<ICurrentUserPermissionService, CurrentUserPermissionService>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, PermissionClaimsTransformer>();
            services.AddSingleton<IAuthorizationHandler, AdminBypassHandler>();
            services.AddScoped<KiraTakip.Services.Interfaces.Identity.IApplicationUserManager, KiraTakip.Infrastructure.Identity.ApplicationUserManagerAdapter>();

            return services;
        }
    }
}
