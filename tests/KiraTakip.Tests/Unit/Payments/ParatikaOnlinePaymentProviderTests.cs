using System.Net;
using System.Text;
using KiraTakip.Infrastructure.Payments;
using KiraTakip.Models.Dtos.OnlinePayment;
using KiraTakip.Models.Settings;
using Microsoft.Extensions.Options;
using Xunit;

namespace KiraTakip.Tests;

/// <summary>
/// Gerçek HTTP çağrısı yapmadan (StubHttpMessageHandler ile), kullanıcının Paratika API v2
/// dokümanından doğrudan yapıştırdığı örnek istek/yanıtlarla (İç Faz 7 planı, 2026-09-11)
/// form alanlarının doğru kurulduğunu ve JSON yanıtın doğru ayrıştırıldığını doğrular.
/// </summary>
public class ParatikaOnlinePaymentProviderTests
{
    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }
        public Uri? LastRequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private static ParatikaOnlinePaymentProvider CreateProvider(
        string responseJson,
        out StubHttpMessageHandler handler,
        int sessionExpiryMinutes = 15)
    {
        handler = new StubHttpMessageHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.paratika.com.tr/") };
        var options = Options.Create(new ParatikaOptions
        {
            ApiBaseUrl = "https://test.paratika.com.tr/paratika/api/v2",
            HostedPaymentPageBaseUrl = "https://test.paratika.com.tr/payment",
            ReturnUrl = "https://app.kiratakip.local/Tenant/Charges/OnlinePaymentReturn",
            SessionExpiryMinutes = sessionExpiryMinutes,
            HttpTimeoutSeconds = 30
        });
        return new ParatikaOnlinePaymentProvider(httpClient, options);
    }

    private static PaymentProviderAccount CreateAccount()
        => new("Paratika", "MERCHANT-1", "api-user", "api-password", "TRY");

    // FormUrlEncodedContent boşlukları '+' ile kodlar (application/x-www-form-urlencoded) —
    // Uri.UnescapeDataString yalnız %XX dizilerini çözer, '+' işaretini boşluğa çevirmez.
    private static string DecodeFormBody(string? body)
        => Uri.UnescapeDataString((body ?? string.Empty).Replace('+', ' '));

    [Fact]
    public async Task CreateSessionAsync_ShouldPostExpectedFormFields()
    {
        // Kullanıcının yapıştırdığı gerçek örnek yanıt (PAYMENTSESSION).
        const string responseJson = """
            {
              "sessionToken" : "HCZVQH5FIR5QBHBQCT6AMUJLVHEHXCMSQ2HA5I6WCGQKQNQX",
              "responseCode" : "00",
              "responseMsg" : "Approved"
            }
            """;
        var provider = CreateProvider(responseJson, out var handler);

        var result = await provider.CreateSessionAsync(
            new CreatePaymentSessionRequest(
                "PaymentId-FuldmrwwiOpb",
                11.21m,
                "TRY",
                "Customer-jNPz2qSI",
                "Name jNPz2qSI",
                "jNPz2qSI@email.com",
                "6381053412"),
            CreateAccount(),
            CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.Equal("HCZVQH5FIR5QBHBQCT6AMUJLVHEHXCMSQ2HA5I6WCGQKQNQX", result.SessionToken);
        Assert.Equal("00", result.ResponseCode);
        Assert.NotNull(result.SessionExpiresAt);
        Assert.Null(result.ProviderTransactionId);

        var body = DecodeFormBody(handler.LastRequestBody);
        Assert.Contains("ACTION=SESSIONTOKEN", body);
        Assert.Contains("MERCHANT=MERCHANT-1", body);
        Assert.Contains("MERCHANTUSER=api-user", body);
        Assert.Contains("MERCHANTPASSWORD=api-password", body);
        Assert.Contains("SESSIONTYPE=PAYMENTSESSION", body);
        Assert.Contains("MERCHANTPAYMENTID=PaymentId-FuldmrwwiOpb", body);
        Assert.Contains("AMOUNT=11.21", body);
        Assert.Contains("CURRENCY=TRY", body);
        Assert.Contains("CUSTOMER=Customer-jNPz2qSI", body);
        Assert.Contains("CUSTOMERNAME=Name jNPz2qSI", body);
        Assert.Contains("CUSTOMEREMAIL=jNPz2qSI@email.com", body);
        Assert.Contains("CUSTOMERPHONE=6381053412", body);
        Assert.Contains("SESSIONEXPIRY=1h", body);
        Assert.Contains("RETURNURL=https://app.kiratakip.local/Tenant/Charges/OnlinePaymentReturn", body);
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldRoundUpSessionExpiryToAtLeastOneHour()
    {
        var provider = CreateProvider(
            """{"sessionToken":"X","responseCode":"00","responseMsg":"Approved"}""",
            out var handler,
            sessionExpiryMinutes: 90);

        await provider.CreateSessionAsync(
            new CreatePaymentSessionRequest("id", 10m, "TRY", "c", "n", "e", "p"),
            CreateAccount(),
            CancellationToken.None);

        var body = DecodeFormBody(handler.LastRequestBody);
        Assert.Contains("SESSIONEXPIRY=2h", body);
    }

    [Fact]
    public async Task QueryAsync_ShouldParseTransactionListEntry_WhenApproved()
    {
        // Kullanıcının yapıştırdığı gerçek örnek QUERYTRANSACTION yanıtı.
        const string responseJson = """
            {
              "responseCode" : "00",
              "responseMsg" : "Approved",
              "transactionCount" : "1",
              "totalTransactionCount" : "1",
              "transactionList" : [ {
                "pgTranTraceAudit" : "828514893296",
                "pgTranReturnCode" : "00",
                "pgOrderId" : "10000000-PaymentId-mQ15HqoF7d8f",
                "pgTranApprCode" : "294906",
                "pgTranId" : "18285OQZD14766",
                "pgTranRefId" : "828514893296",
                "amount" : 80,
                "transactionStatus" : "AP",
                "currency" : "TRY",
                "paymentSystem" : "My Finans Webpos Online Account (Test)",
                "transactionType" : "SALE"
              } ]
            }
            """;
        var provider = CreateProvider(responseJson, out var handler);

        var result = await provider.QueryAsync(
            new PaymentInquiryRequest("PaymentId-mQ15HqoF7d8f"),
            CreateAccount(),
            CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.Equal("18285OQZD14766", result.ProviderTransactionId);
        Assert.Equal("AP", result.TransactionStatus);

        var body = DecodeFormBody(handler.LastRequestBody);
        Assert.Contains("ACTION=QUERYTRANSACTION", body);
        Assert.Contains("MERCHANTPAYMENTID=PaymentId-mQ15HqoF7d8f", body);
    }

    [Fact]
    public async Task QueryAsync_ShouldReturnUnsuccessful_WhenTransactionListIsEmpty()
    {
        const string responseJson = """
            {
              "responseCode" : "00",
              "responseMsg" : "Approved",
              "transactionCount" : "0",
              "totalTransactionCount" : "0",
              "transactionList" : [ ]
            }
            """;
        var provider = CreateProvider(responseJson, out _);

        var result = await provider.QueryAsync(
            new PaymentInquiryRequest("unknown-id"),
            CreateAccount(),
            CancellationToken.None);

        Assert.False(result.IsSuccessful);
        Assert.Null(result.TransactionStatus);
    }

    [Fact]
    public void BuildHostedPaymentPageUrl_ShouldAppendSessionTokenToConfiguredBaseUrl()
    {
        var provider = CreateProvider("{}", out _);

        var url = provider.BuildHostedPaymentPageUrl("SESSION-TOKEN-1");

        Assert.Equal("https://test.paratika.com.tr/payment/SESSION-TOKEN-1", url);
    }

    [Fact]
    public async Task ValidateCallbackAsync_ShouldReturnInvalid_WhenMerchantPaymentIdMissing()
    {
        var provider = CreateProvider("{}", out _);

        var result = await provider.ValidateCallbackAsync(
            new PaymentCallbackRequest(new Dictionary<string, string>()),
            CreateAccount(),
            CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateCallbackAsync_ShouldReturnMerchantPaymentIdAsQueryKeyOnly_WhenPresent()
    {
        var provider = CreateProvider("{}", out _);

        var result = await provider.ValidateCallbackAsync(
            new PaymentCallbackRequest(new Dictionary<string, string> { ["merchantPaymentId"] = "abc-123" }),
            CreateAccount(),
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Equal("abc-123", result.MerchantPaymentId);
        // Bilinçli tasarım: imza doğrulanmaz, bu yüzden ResponseCode/TransactionStatus hep null döner —
        // asıl karar OnlinePaymentService.CompleteAsync'in QUERYTRANSACTION çağrısına dayanır.
        Assert.Null(result.ResponseCode);
        Assert.Null(result.TransactionStatus);
    }
}
