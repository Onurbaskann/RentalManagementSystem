using HashidsNet;
using KiraTakip.Common;
using KiraTakip.Infrastructure.DependencyInjection;
using KiraTakip.Infrastructure.Hashids;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KiraTakip.Tests;

public class HashIdEncoderTests
{
    [Fact]
    public void Encode_ReturnsExpectedHashMatchingHashids()
    {
        var salt = "test-salt-123";
        var minLength = 8;
        var hashids = new Hashids(salt, minLength);
        var encoder = new HashIdEncoder(hashids);

        var id = 42;
        var expectedHash = hashids.Encode(id);
        var actualHash = encoder.Encode(id);

        Assert.Equal(expectedHash, actualHash);
        Assert.True(actualHash.Length >= minLength);
    }

    [Fact]
    public void InfrastructureModule_RegistersIHashIdEncoderAsSingleton()
    {
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;",
            ["Hashids:Salt"] = "di-test-salt",
            ["Hashids:MinHashLength"] = "10"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

        services.AddInfrastructureModule(configuration);
        var provider = services.BuildServiceProvider();

        var encoder = provider.GetService<IHashIdEncoder>();
        Assert.NotNull(encoder);
        Assert.IsType<HashIdEncoder>(encoder);

        var secondEncoder = provider.GetService<IHashIdEncoder>();
        Assert.Same(encoder, secondEncoder);

        var hashids = provider.GetRequiredService<IHashids>();
        Assert.Equal(hashids.Encode(99), encoder.Encode(99));
    }
}
