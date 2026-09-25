namespace GreenMart.Models
{
    public class PaymentResultViewModel
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool CanDownloadReceipt { get; set; }
    }
}
