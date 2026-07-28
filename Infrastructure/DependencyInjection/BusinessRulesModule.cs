using System.Reflection;
using KiraTakip.Infrastructure.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.DependencyInjection
{
    public static class BusinessRulesModule
    {
        /// <summary>
        /// Assembly içinde IBusinessRules'tan türeyen domain kural interface'lerini (örn.
        /// ILeaseBusinessRules) ve her birinin tek implementasyonunu tarayıp scoped olarak
        /// kaydeder. Deployment başına farklı bir kural seti kaydetmek (örn.
        /// CustomerBLeaseBusinessRules), bu taramanın bulacağı implementasyon sınıfını
        /// değiştirmekten ibarettir — servise dokunulmaz.
        ///
        /// Şu an assembly içinde IBusinessRules'tan türeyen bir interface olmadığından
        /// bu adımda hiçbir kayıt yapılmaz; seam inert'tir (davranış değişmez).
        /// </summary>
        public static IServiceCollection AddBusinessRulesModule(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            var businessRuleInterfaces = types.Where(t =>
                t.IsInterface && t != typeof(IBusinessRules) && typeof(IBusinessRules).IsAssignableFrom(t));

            foreach (var iface in businessRuleInterfaces)
            {
                var implementation = types.SingleOrDefault(t =>
                    t is { IsClass: true, IsAbstract: false } && iface.IsAssignableFrom(t));

                if (implementation != null)
                    services.AddScoped(iface, implementation);
            }

            return services;
        }
    }
}
