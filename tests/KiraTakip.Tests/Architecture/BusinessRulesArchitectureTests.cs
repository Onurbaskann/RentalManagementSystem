using KiraTakip.Application.DependencyInjection;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Payments;
using KiraTakip.Services.Reservations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KiraTakip.Tests.Architecture;

public class BusinessRulesArchitectureTests
{
    [Fact]
    public void AddBusinessRulesModule_ShouldRegisterAllBusinessRuleInterfacesWithExactImplementations()
    {
        var services = new ServiceCollection();
        services.AddBusinessRulesModule();

        var onlinePayment = services.SingleOrDefault(d => d.ServiceType == typeof(IOnlinePaymentBusinessRules));
        Assert.NotNull(onlinePayment);
        Assert.Equal(typeof(OnlinePaymentBusinessRules), onlinePayment.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, onlinePayment.Lifetime);

        var payment = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentBusinessRules));
        Assert.NotNull(payment);
        Assert.Equal(typeof(PaymentBusinessRules), payment.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, payment.Lifetime);

        var paymentRouting = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentStoreRoutingBusinessRules));
        Assert.NotNull(paymentRouting);
        Assert.Equal(typeof(PaymentStoreRoutingBusinessRules), paymentRouting.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, paymentRouting.Lifetime);

        var store = services.SingleOrDefault(d => d.ServiceType == typeof(IStoreBusinessRules));
        Assert.NotNull(store);
        Assert.Equal(typeof(StoreBusinessRules), store.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, store.Lifetime);

        var reservation = services.SingleOrDefault(d => d.ServiceType == typeof(IReservationBusinessRules));
        Assert.NotNull(reservation);
        Assert.Equal(typeof(ReservationBusinessRules), reservation.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, reservation.Lifetime);
    }
}
