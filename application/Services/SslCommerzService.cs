using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GreenMart.Models;
using Microsoft.Extensions.Options;

namespace GreenMart.Services
{
    public class SslCommerzService : ISslCommerzService
    {
        private readonly HttpClient _httpClient;
        private readonly SslCommerzSettings _settings;

        public SslCommerzService(
            HttpClient httpClient,
            IOptions<SslCommerzSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task<SslCommerzSessionResult> CreateSessionAsync(
            Order order,
            Payment payment,
            User customer,
            string callbackBaseUrl,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                return new(false, null, null,
                    "Online payment is not configured. Add your SSLCOMMERZ sandbox credentials to appsettings.Payment.json.");
            }

            var endpoint = _settings.Sandbox
                ? "https://sandbox.sslcommerz.com/gwprocess/v4/api.php"
                : "https://securepay.sslcommerz.com/gwprocess/v4/api.php";

            var callbackRoot = callbackBaseUrl.TrimEnd('/');
            var productNames = string.Join(", ",
                (order.OrderItems ?? Array.Empty<OrderItem>())
                .Select(x => x.Product.ProductName)
                .Take(5));

            var values = new Dictionary<string, string>
            {
                ["store_id"] = _settings.StoreId,
                ["store_passwd"] = _settings.StorePassword,
                ["total_amount"] = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                ["currency"] = payment.Currency,
                ["tran_id"] = payment.TransactionId,
                ["success_url"] = $"{callbackRoot}/Payment/Success",
                ["fail_url"] = $"{callbackRoot}/Payment/Fail",
                ["cancel_url"] = $"{callbackRoot}/Payment/Cancel",
                ["ipn_url"] = $"{callbackRoot}/Payment/Ipn",
                ["cus_name"] = customer.FullName,
                ["cus_email"] = customer.Email,
                ["cus_add1"] = order.ShippingAddress ?? "Not provided",
                ["cus_city"] = "Dhaka",
                ["cus_postcode"] = "1200",
                ["cus_country"] = "Bangladesh",
                ["cus_phone"] = customer.PhoneNumber,
                ["shipping_method"] = "YES",
                ["ship_name"] = customer.FullName,
                ["ship_add1"] = order.ShippingAddress ?? "Not provided",
                ["ship_city"] = "Dhaka",
                ["ship_postcode"] = "1200",
                ["ship_country"] = "Bangladesh",
                ["product_name"] = string.IsNullOrWhiteSpace(productNames) ? $"GreenMart order #{order.OrderId}" : productNames,
                ["product_category"] = "Marketplace",
                ["product_profile"] = "general",
                ["num_of_item"] = (order.OrderItems?.Sum(x => x.Quantity) ?? 1).ToString(CultureInfo.InvariantCulture),
                ["value_a"] = order.OrderId.ToString(CultureInfo.InvariantCulture)
            };

            var gatewayFilter = GatewayFilter(payment.SelectedChannel);
            if (gatewayFilter != null)
            {
                values["multi_card_name"] = gatewayFilter;
            }

            try
            {
                using var response = await _httpClient.PostAsync(
                    endpoint,
                    new FormUrlEncodedContent(values),
                    cancellationToken);

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<SessionResponse>(cancellationToken: cancellationToken);

                if (result?.Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) == true &&
                    Uri.TryCreate(result.GatewayPageUrl, UriKind.Absolute, out _))
                {
                    return new(true, result.GatewayPageUrl, result.SessionKey, null);
                }

                return new(false, null, result?.SessionKey,
                    result?.FailedReason ?? "The payment gateway could not start a payment session.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return new(false, null, null, "The payment gateway is currently unreachable. Please try again.");
            }
        }

        public async Task<SslCommerzValidationResult> ValidatePaymentAsync(
            string validationId,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(validationId))
            {
                return Invalid("Payment validation information is missing.");
            }

            var endpoint = _settings.Sandbox
                ? "https://sandbox.sslcommerz.com/validator/api/validationserverAPI.php"
                : "https://securepay.sslcommerz.com/validator/api/validationserverAPI.php";

            var url = $"{endpoint}?val_id={Uri.EscapeDataString(validationId)}" +
                      $"&store_id={Uri.EscapeDataString(_settings.StoreId)}" +
                      $"&store_passwd={Uri.EscapeDataString(_settings.StorePassword)}&v=1&format=json";

            try
            {
                var result = await _httpClient.GetFromJsonAsync<ValidationResponse>(url, cancellationToken);
                var validStatus = result?.Status.Equals("VALID", StringComparison.OrdinalIgnoreCase) == true ||
                                  result?.Status.Equals("VALIDATED", StringComparison.OrdinalIgnoreCase) == true;

                if (!validStatus || result == null)
                {
                    return Invalid("SSLCOMMERZ did not validate this transaction.", result?.Status);
                }

                _ = decimal.TryParse(result.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount);
                return new(true, result.TransactionId, amount, result.Currency,
                    result.TransactionId, result.BankTransactionId,
                    result.CardType, result.Status, result.RiskLevel, null);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return Invalid("The payment gateway validation service is currently unreachable.");
            }
        }

        private static string? GatewayFilter(string? selectedChannel) => selectedChannel?.ToLowerInvariant() switch
        {
            "bkash" => "bkash",
            "nagad" => "mobilebank",
            "rocket" => "dbblmobilebanking",
            "visa" => "visacard",
            "mastercard" => "mastercard",
            _ => null
        };

        private static SslCommerzValidationResult Invalid(string error, string? status = null) =>
            new(false, null, 0, null, null, null, null, status, 0, error);

        private sealed class SessionResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("failedreason")]
            public string? FailedReason { get; set; }

            [JsonPropertyName("GatewayPageURL")]
            public string? GatewayPageUrl { get; set; }

            [JsonPropertyName("sessionkey")]
            public string? SessionKey { get; set; }
        }

        private sealed class ValidationResponse
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;

            [JsonPropertyName("tran_id")]
            public string? TransactionId { get; set; }

            [JsonPropertyName("amount")]
            public string? Amount { get; set; }

            [JsonPropertyName("currency")]
            public string? Currency { get; set; }

            [JsonPropertyName("val_id")]
            public string? ValidationId { get; set; }

            [JsonPropertyName("card_type")]
            public string? CardType { get; set; }

            [JsonPropertyName("bank_tran_id")]
            public string? BankTransactionId { get; set; }

            [JsonPropertyName("risk_level")]
            public int RiskLevel { get; set; }

        }
    }
}
