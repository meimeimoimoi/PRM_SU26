using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using SmartDine.Application.Constants;
using SmartDine.Domain.Interfaces;

namespace SmartDine.Infrastructure.ExternalServices;

/// <summary>
/// Tích hợp cổng thanh toán PayOS (payos.vn) qua REST API.
///
/// Credentials từ appsettings["PayOS"]: ClientId, ApiKey, ChecksumKey.
/// Endpoint: https://api-merchant.payos.vn/v2/payment-requests
///
/// Ai dùng: được inject vào PaymentService qua IPaymentGateway interface.
/// Test: thay bằng MockPaymentGateway (không cần network thật).
///
/// Tài liệu PayOS: https://payos.vn/docs/api
/// </summary>
public class PayOsGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;

    private const string BaseUrl = "https://api-merchant.payos.vn/v2/payment-requests";

    public PayOsGateway(HttpClient http, IConfiguration config)
    {
        _http = http;
        _clientId = config["PayOS:ClientId"]
            ?? throw new InvalidOperationException(ValidationMessages.PAYOS_CONFIG_CLIENTID_MISSING);
        _apiKey = config["PayOS:ApiKey"]
            ?? throw new InvalidOperationException(ValidationMessages.PAYOS_CONFIG_APIKEY_MISSING);
        _checksumKey = config["PayOS:ChecksumKey"]
            ?? throw new InvalidOperationException(ValidationMessages.PAYOS_CONFIG_CHECKSUMKEY_MISSING);

        _http.DefaultRequestHeaders.Add("x-client-id", _clientId);
        _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
    }

    /// <summary>
    /// Gọi PayOS API tạo link thanh toán cho 1 đơn hàng.
    /// Trả về CheckoutUrl (deeplink) + QrCode (raw EMV string).
    ///
    /// PayOS yêu cầu amount là số nguyên (VND), không có phần thập phân.
    /// orderCode phải unique trong hệ thống merchant.
    /// </summary>
    public async Task<GatewayCreatePaymentResult> CreatePaymentLinkAsync(
        long orderCode, int amount, string description, string returnUrl, string cancelUrl)
    {
        var body = new
        {
            orderCode,
            amount,
            description,
            cancelUrl,
            returnUrl,
            items = new[]
            {
                new { name = "Hóa đơn", quantity = 1, price = amount }
            }
        };

        try
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, body);
            var json = await response.Content.ReadFromJsonAsync<PayOsApiResponse>();

            if (json?.Code != "00" || json.Data == null)
                return new GatewayCreatePaymentResult(false, null, null, null, 0,
                    json?.Desc ?? "PayOS trả về lỗi không xác định.");

            return new GatewayCreatePaymentResult(
                Success: true,
                CheckoutUrl: json.Data.CheckoutUrl,
                QrCode: json.Data.QrCode,
                PaymentLinkId: json.Data.PaymentLinkId,
                OrderCode: json.Data.OrderCode);
        }
        catch (Exception ex)
        {
            return new GatewayCreatePaymentResult(false, null, null, null, 0,
                $"Lỗi kết nối cổng thanh toán: {ex.Message}");
        }
    }

    /// <summary>
    /// Xác minh webhook từ PayOS bằng HMAC-SHA256.
    ///
    /// Thuật toán:
    ///   1. Lấy tất cả key-value trong data object, sắp xếp key theo alphabet.
    ///   2. Nối thành chuỗi "key1=value1&amp;key2=value2&amp;...".
    ///   3. HMAC-SHA256 chuỗi đó với ChecksumKey → so sánh với signature trong body.
    ///
    /// Trả về null nếu signature sai — caller phải reject request ngay.
    /// </summary>
    public GatewayWebhookData? VerifyAndParseWebhook(string webhookBody, string? signature)
    {
        try
        {
            var doc = JsonDocument.Parse(webhookBody);
            var root = doc.RootElement;

            // Lấy data object để verify
            if (!root.TryGetProperty("data", out var dataEl))
                return null;

            // Build sorted query string từ data fields
            var sortedFields = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var prop in dataEl.EnumerateObject())
            {
                var val = prop.Value.ValueKind switch
                {
                    JsonValueKind.Null    => "",
                    JsonValueKind.String  => prop.Value.GetString() ?? "",
                    JsonValueKind.Number  => prop.Value.GetRawText(),
                    JsonValueKind.True    => "true",
                    JsonValueKind.False   => "false",
                    _                     => prop.Value.GetRawText()
                };
                sortedFields[prop.Name] = val;
            }

            var dataStr = string.Join("&", sortedFields.Select(kv => $"{kv.Key}={kv.Value}"));
            var expectedSig = ComputeHmacSha256(dataStr, _checksumKey);

            // Lấy signature từ body (nếu không truyền qua header)
            var bodySig = signature;
            if (string.IsNullOrEmpty(bodySig) && root.TryGetProperty("signature", out var sigEl))
                bodySig = sigEl.GetString();

            if (!string.Equals(expectedSig, bodySig, StringComparison.OrdinalIgnoreCase))
                return null;

            // Parse kết quả
            var isSuccess = root.TryGetProperty("code", out var codeEl) && codeEl.GetString() == "00";
            var orderCode = dataEl.TryGetProperty("orderCode", out var ocEl) ? ocEl.GetInt64() : 0;
            var amount    = dataEl.TryGetProperty("amount", out var amEl) ? amEl.GetInt32() : 0;
            var desc      = dataEl.TryGetProperty("description", out var dEl) ? dEl.GetString() ?? "" : "";

            return new GatewayWebhookData(orderCode, amount, desc, isSuccess);
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ─── Internal DTOs (chỉ dùng để deserialize response PayOS) ───

    private class PayOsApiResponse
    {
        [JsonPropertyName("code")]   public string? Code { get; set; }
        [JsonPropertyName("desc")]   public string? Desc { get; set; }
        [JsonPropertyName("data")]   public PayOsResponseData? Data { get; set; }
    }

    private class PayOsResponseData
    {
        [JsonPropertyName("checkoutUrl")]    public string? CheckoutUrl    { get; set; }
        [JsonPropertyName("qrCode")]         public string? QrCode         { get; set; }
        [JsonPropertyName("paymentLinkId")]  public string? PaymentLinkId  { get; set; }
        [JsonPropertyName("orderCode")]      public long    OrderCode       { get; set; }
        [JsonPropertyName("status")]         public string? Status          { get; set; }
    }
}
