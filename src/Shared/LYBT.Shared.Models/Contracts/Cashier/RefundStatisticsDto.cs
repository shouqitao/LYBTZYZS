namespace LYBT.Shared.Models.Contracts.Cashier
{
    public class RefundStatisticsDto
    {
        public int RefundCount { get; set; }
        public decimal RefundAmount { get; set; }
        public Dictionary<string, int> RefundReasons { get; set; } = new();
    }
}