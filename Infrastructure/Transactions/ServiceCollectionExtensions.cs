using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Infrastructure.Transactions;

public static class TransactionalServiceCollectionExtensions
{
    private static readonly ProxyGenerator ProxyGenerator = new();

    /// <summary>
    /// Daha önce register edilmiş tüm interface→implementation eşleşmelerini tarar;
    /// implementation tipi <see cref="ITransactionalService"/> implement ediyorsa
    /// orijinal kaydı çıkartıp yerine Castle DynamicProxy ile sarmalanmış bir versiyon koyar.
    ///
    /// Bu metot Program.cs içinde TÜM servis register'larından SONRA çağrılmalıdır.
    /// </summary>
    public static IServiceCollection AddTransactionalProxies(this IServiceCollection services)
    {
        services.AddScoped<TransactionInterceptor>();

        var transactional = services
            .Where(d => d.ServiceType.IsInterface
                     && d.ImplementationType != null
                     && typeof(ITransactionalService).IsAssignableFrom(d.ImplementationType))
            .ToList();

        foreach (var descriptor in transactional)
        {
            services.Remove(descriptor);

            var serviceType = descriptor.ServiceType;
            var implType = descriptor.ImplementationType!;
            var lifetime = descriptor.Lifetime;

            // Concrete tipi de aynı lifetime ile kaydet (proxy bunu hedef alacak)
            services.Add(new ServiceDescriptor(implType, implType, lifetime));

            services.Add(new ServiceDescriptor(
                serviceType,
                sp =>
                {
                    var target = sp.GetRequiredService(implType);
                    var interceptor = sp.GetRequiredService<TransactionInterceptor>();
                    return ProxyGenerator.CreateInterfaceProxyWithTarget(
                        serviceType,
                        target,
                        interceptor.ToInterceptor());
                },
                lifetime));
        }

        return services;
    }
}
