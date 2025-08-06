namespace LYBT.Shared.Models.Contracts.Cashier
{
    public class WorkloadStatisticsDto
    {
        public Dictionary<Guid, int> CashierTransactions { get; set; } = new();
        public Dictionary<Guid, decimal> CashierAmounts { get; set; } = new();
        public int TotalTransactions { get; set; }
        public decimal TotalAmount { get; set; }
    }
}