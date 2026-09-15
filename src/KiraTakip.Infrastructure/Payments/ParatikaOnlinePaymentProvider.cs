using System.Globalization;
using System.Text.Json;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos.OnlinePayment;
using KiraTakip.Models.Settings;
using KiraTakip.Services.Interfaces.Payments;
using Microsoft.Extensions.Options;

namespace KiraTakip.Infrastructure.Payments;

/// <summary>
/// Paratika Hosted Payment Page (SESSIONTOKEN/QUERYTRANSACTION) entegrasyonu. Alan adları ve
/// örnek istek/yanıtlar kullanıcı tarafından doğrulanmış ham doküman metninden alındı
/// (2026-09-11, İç Faz 7 planı) — WebFetch özetlemesi değil.
///
/// ValidateCallbackAsync BİLİNÇLİ OLARAK imza doğrulaması yapmaz: bulunan sdSha512/SD_SHA512
/// imza şeması dokümanın Direct POST/MOTO (server-to-server kart geçişi) bölümüne ait — bizim
/// kullandığımız Hosted Payment Page akışına uygulanabilirliği doğrulanamadı. Bu yüzden
/// RETURNURL/NOTIFICATIONURL verisine hiçbir noktada güvenilmez; yalnızca merchantPaymentId bir
/// SORGU ANAHTARI olarak okunur, asıl karar OnlinePaymentService.CompleteAsync'in yaptığı
/// authenticated QUERYTRANSACTION çağrısının sonucuna dayanır.
/// </summary>
public class ParatikaOnlinePaymentProvider(HttpClient httpClient, IOptions<ParatikaOptions> options)
    : IOnlinePaymentProvider
{
    private readonly ParatikaOptions _options = options.Value;

    public string ProviderCode => PaymentProviderCodes.Paratika;

    public async Task<CreatePaymentSessionResult> CreateSessionAsync(
        CreatePaymentSessionRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["ACTION"] = "SESSIONTOKEN",
            ["MERCHANT"] = account.MerchantId,
            ["MERCHANTUSER"] = account.MerchantUser,
            ["MERCHANTPASSWORD"] = account.MerchantPassword,
            ["SESSIONTYPE"] = "PAYMENTSESSION",
            ["RETURNURL"] = _options.ReturnUrl,
            ["MERCHANTPAYMENTID"] = request.MerchantPaymentId,
            ["AMOUNT"] = request.Amount.ToString(CultureInfo.InvariantCulture),
            ["CURRENCY"] = request.Currency,
            ["CUSTOMER"] = request.CustomerCode,
            ["CUSTOMERNAME"] = request.CustomerName,
            ["CUSTOMEREMAIL"] = request.CustomerEmail,
            ["CUSTOMERPHONE"] = request.CustomerPhone,
            ["SESSIONEXPIRY"] = FormatSessionExpiry(_options.SessionExpiryMinutes)
        };

        using var response = await PostAsync(fields, cancellationToken);
        var json = await ParseJsonAsync(response, cancellationToken);

        var responseCode = GetString(json, "responseCode");
        var isSuccessful = responseCode == "00";

        return new CreatePaymentSessionResult(
            IsSuccessful: isSuccessful,
            // SESSIONTOKEN yanıtı bir işlem numarası (pgTranId) döndürmüyor — yalnız
            // {sessionToken, responseCode, responseMsg}; işlem no'su ilk kez QUERYTRANSACTION'da gelir.
            ProviderTransactionId: null,
            SessionToken: GetString(json, "sessionToken"),
            SessionExpiresAt: isSuccessful ? DateTime.UtcNow.AddMinutes(_options.SessionExpiryMinutes) : null,
            ResponseCode: responseCode,
            TransactionStatus: null,
            ErrorCode: GetString(json, "errorCode"),
            SafeMessage: GetString(json, "responseMsg") ?? GetString(json, "errorMsg"));
    }

    public async Task<PaymentInquiryResult> QueryAsync(
        PaymentInquiryRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["ACTION"] = "QUERYTRANSACTION",
            ["MERCHANT"] = account.MerchantId,
            ["MERCHANTUSER"] = account.MerchantUser,
            ["MERCHANTPASSWORD"] = account.MerchantPassword,
            ["MERCHANTPAYMENTID"] = request.MerchantPaymentId
        };

        using var response = await PostAsync(fields, cancellationToken);
        var json = await ParseJsonAsync(response, cancellationToken);

        var responseCode = GetString(json, "responseCode");
        var entry = json.TryGetProperty("transactionList", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().FirstOrDefault()
            : default;

        if (responseCode != "00" || entry.ValueKind != JsonValueKind.Object)
        {
            return new PaymentInquiryResult(
                IsSuccessful: false,
                ProviderTransactionId: null,
                ResponseCode: responseCode,
                TransactionStatus: null,
                ErrorCode: GetString(json, "errorCode"),
                SafeMessage: GetString(json, "responseMsg"));
        }

        return new PaymentInquiryResult(
            IsSuccessful: true,
            ProviderTransactionId: GetString(entry, "pgTranId"),
            ResponseCode: responseCode,
            TransactionStatus: GetString(entry, "transactionStatus"),
            ErrorCode: null,
            SafeMessage: GetString(entry, "paymentSystem"));
    }

    public Task<PaymentCallbackResult> ValidateCallbackAsync(
        PaymentCallbackRequest request,
        PaymentProviderAccount account,
        CancellationToken cancellationToken)
    {
        request.Fields.TryGetValue("merchantPaymentId", out var merchantPaymentId);

        return Task.FromResult(new PaymentCallbackResult(
            IsValid: !string.IsNullOrWhiteSpace(merchantPaymentId),
            MerchantPaymentId: merchantPaymentId,
            ProviderTransactionId: null,
            ResponseCode: null,
            TransactionStatus: null,
            ErrorCode: null,
            SafeMessage: null));
    }

    // Format WebFetch özetinden geliyor (https://vpos.paratika.com.tr/payment/[SECURE_SESSION_TOKEN])
    // — kullanıcı tarafından ham metinle doğrulanmadı. Yanlışsa yalnızca 404/hatalı yönlendirme ile
    // sonuçlanır (güvenlik riski yok); DURMA KAPISI C'deki manuel duman testinde ilk kez doğrulanacak.
    public string BuildHostedPaymentPageUrl(string sessionToken)
        => $"{_options.HostedPaymentPageBaseUrl.TrimEnd('/')}/{sessionToken}";

    private async Task<HttpResponseMessage> PostAsync(
        Dictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(fields);
        var response = await httpClient.PostAsync(_options.ApiBaseUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static async Task<JsonElement> ParseJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    // SESSIONEXPIRY dokümanda yalnız saat birimli örnekle (varsayılan '168h') doğrulandı; dakika
    // biriminin desteklendiği teyit edilmediği için en az 1 saate yuvarlanır (İç Faz 7 planı notu).
    private static string FormatSessionExpiry(int minutes)
    {
        var hours = Math.Max(1, (int)Math.Ceiling(minutes / 60.0));
        return $"{hours}h";
    }
}
