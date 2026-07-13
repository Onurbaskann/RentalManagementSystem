using KiraTakip.Authorization;
using KiraTakip.Infrastructure;
using KiraTakip.Infrastructure.Hashids;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class WebModule
    {
        public static IServiceCollection AddWebModule(this IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                options.Filters.AddService<YetkiKapsamiActionFilter>();
                options.Filters.Add<BusinessRuleExceptionFilter>();
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

            return services;
        }
    }
}
