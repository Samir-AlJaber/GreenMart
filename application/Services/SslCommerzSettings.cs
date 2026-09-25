namespace GreenMart.Services
{
    public class SslCommerzSettings
    {
        public bool Sandbox { get; set; } = true;
        public string StoreId { get; set; } = string.Empty;
        public string StorePassword { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(StoreId) &&
            !string.IsNullOrWhiteSpace(StorePassword);
    }
}
