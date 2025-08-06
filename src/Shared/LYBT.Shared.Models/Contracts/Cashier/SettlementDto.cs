namespace LYBT.Shared.Models.Contracts.Cashier
{
    public class SettlementDto
    {
        public Guid Id { get; set; }
        public Guid CashierId { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
        public string Status { get; set; }
    }
}