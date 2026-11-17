using System;

namespace BereketMarket;

public class CustomerTransaction
{
	public int Id { get; set; }
	public int CustomerId { get; set; }
	public string TransactionType { get; set; } = "Debt";
	public int AmountKurus { get; set; }
	public string? Description { get; set; }
	public int? SaleId { get; set; }
	public DateTime CreatedAt { get; set; }
	public decimal AmountLira => AmountKurus / 100m;
	public string DisplayType => TransactionType == "Debt" ? "Borç (Alışveriş)" : "Tahsilat (Ödeme)";
}
