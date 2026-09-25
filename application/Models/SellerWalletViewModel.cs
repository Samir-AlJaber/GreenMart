namespace GreenMart.Models
{
    public class SellerWalletViewModel
    {
        public SellerPayoutAccount? Account { get; set; }
        public List<SellerEarning> Earnings { get; set; } = new();
        public decimal PendingBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PaidBalance { get; set; }
    }

    public class AdminPayoutDashboardViewModel
    {
        public List<SellerPayoutAccount> PendingAccounts { get; set; } = new();
        public List<SellerEarning> CodEarningsAwaitingRemittance { get; set; } = new();
        public List<SellerPayout> Payouts { get; set; } = new();
    }
}
