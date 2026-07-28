using System.Reflection;
using KiraTakip.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class ValidationModule
    {
        /// <summary>
        /// Assembly içinde IValidator&lt;T&gt; implementasyonu olan tüm sınıfları tarar ve
        /// scoped olarak kaydeder. Yeni bir validator eklemek, ilgili sınıfı yazmaktan
        /// ibarettir — ayrıca DI kaydı gerekmez.
        /// </summary>
        public static IServiceCollection AddValidationModule(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var validatorRegistrations =
                from type in assembly.GetTypes()
                where type is { IsAbstract: false, IsInterface: false }
                from iface in type.GetInterfaces()
                where iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IValidator<>)
                select (Service: iface, Implementation: type);

            foreach (var (service, implementation) in validatorRegistrations)
                services.AddScoped(service, implementation);

            return services;
        }
    }
}
