using KiraTakip.Web.Authorization;
using KiraTakip.Web.Context;
using KiraTakip.Web.Filters;
using KiraTakip.Web.HostedServices;
using KiraTakip.Web.Identity;
using KiraTakip.Web.ModelBinding;
using KiraTakip.Web.Rendering;
using KiraTakip.Services.Interfaces.Documents;
using KiraTakip.Services.Interfaces.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Web.DependencyInjection
{
    public static class WebModule
    {
        public static IServiceCollection AddWebModule(this IServiceCollection services)
        {
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
            services.AddScoped<KiraTakip.Common.IRequestContext, HttpRequestContext>();
            services.AddScoped<ICurrentUserContext, CurrentUserContext>();
            services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
            services.AddScoped<YetkiKapsamiActionFilter>();
            services.AddHostedService<ReservationCompletionBackgroundService>();

            return services;
        }
    }
}
