using GreenMart.Models;

namespace GreenMart.Services
{
    public interface ISslCommerzService
    {
        bool IsConfigured { get; }

        Task<SslCommerzSessionResult> CreateSessionAsync(
            Order order,
            Payment payment,
            User customer,
            string callbackBaseUrl,
            CancellationToken cancellationToken = default);

        Task<SslCommerzValidationResult> ValidatePaymentAsync(
            string validationId,
            CancellationToken cancellationToken = default);
    }

    public record SslCommerzSessionResult(
        bool Success,
        string? GatewayUrl,
        string? SessionKey,
        string? Error);

    public record SslCommerzValidationResult(
        bool IsValid,
        string? TransactionId,
        decimal Amount,
        string? Currency,
        string? GatewayTransactionId,
        string? BankTransactionId,
        string? CardType,
        string? Status,
        int RiskLevel,
        string? Error);
}
