using KiraTakip.Auditing;
using KiraTakip.Common;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using KiraTakip.Web.Authorization;
using KiraTakip.Web.Context;
using KiraTakip.Web.Filters;
using KiraTakip.Web.HostedServices;
using KiraTakip.Web.Hosting;
using KiraTakip.Web.Identity;
using KiraTakip.Web.ModelBinding;
using KiraTakip.Web.Rendering;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace KiraTakip.Web.DependencyInjection
{
    public static class WebModule
    {
        public static IServiceCollection AddWebModule(
            this IServiceCollection services,
            IConfiguration? configuration = null)
        {
            var reverseProxySection = configuration?.GetSection(ReverseProxySettings.SectionName);
            var reverseProxySettings = reverseProxySection?.Get<ReverseProxySettings>()
                ?? new ReverseProxySettings();

            services.AddOptions<ReverseProxySettings>()
                .Bind(reverseProxySection ?? new ConfigurationBuilder().Build().GetSection(ReverseProxySettings.SectionName))
                .Validate(settings => settings.ForwardLimit is >= 1 and <= 10,
                    "ReverseProxy:ForwardLimit 1 ile 10 arasında olmalıdır.")
                .Validate(settings => settings.KnownProxies.All(value => IPAddress.TryParse(value, out _)),
                    "ReverseProxy:KnownProxies yalnızca geçerli IP adresleri içermelidir.")
                .ValidateOnStart();

            if (reverseProxySettings.Enabled)
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.ForwardLimit = reverseProxySettings.ForwardLimit;
                    foreach (var proxy in reverseProxySettings.KnownProxies)
                    {
                        if (IPAddress.TryParse(proxy, out var address))
                            options.KnownProxies.Add(address);
                    }
                });
            }

            services.AddControllersWithViews(options =>
            {
                options.Filters.AddService<YetkiKapsamiActionFilter>();
                options.Filters.Add<ValidationActionFilter>();
                options.Filters.Add<BusinessRuleExceptionFilter>();
                options.Filters.Add<SuccessfulPostRedirectFilter>();
                options.ModelBinderProviders.Insert(0, new HashidsModelBinderProvider());
                options.ModelBinderProviders.Insert(1, new InvariantDecimalModelBinderProvider());
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

            services.AddHttpContextAccessor();
            services.AddScoped<IRequestContext, HttpRequestContext>();
            services.AddScoped<IAuditContext, HttpRequestContext>();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();
            services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
            services.AddScoped<YetkiKapsamiActionFilter>();
            services.AddHostedService<ReservationCompletionBackgroundService>();

            return services;
        }
    }
}
