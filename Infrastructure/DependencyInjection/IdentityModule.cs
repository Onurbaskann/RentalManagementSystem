using System;
using KiraTakip.Authorization;
using KiraTakip.Data;
using KiraTakip.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.DependencyInjection
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
            .AddErrorDescriber<KiraTakip.Infrastructure.TurkishIdentityErrorDescriber>();

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
            });

            services.AddAuthorization(options =>
            {
                foreach (var m in PermissionCatalog.AllModules)
                {
                    var modulePath = m.Path;
                    options.AddPolicy(modulePath, policy => policy.RequireClaim(AppClaimTypes.Permission, modulePath));
                    foreach (var action in m.Actions)
                    {
                        var actionPath = action;
                        options.AddPolicy(actionPath, policy => policy.RequireClaim(AppClaimTypes.Permission, actionPath));
                    }
                }

                options.AddPolicy("KiraciKullanici", policy =>
                    policy.RequireClaim(AppClaimTypes.UserType, ((int)UserType.Kiraci).ToString()));
            });

            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, PermissionClaimsTransformer>();
            services.AddSingleton<IAuthorizationHandler, AdminBypassHandler>();

            return services;
        }
    }
}
