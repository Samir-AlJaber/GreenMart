namespace GreenMart.Services
{
    public interface IEmailService
    {
        Task<bool> SendDeliveryApprovalAsync(string recipientEmail, string recipientName);
    }
}
