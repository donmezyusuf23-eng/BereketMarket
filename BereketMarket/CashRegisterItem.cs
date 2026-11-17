using System;

namespace BereketMarket;

public class CashRegisterItem
{
	public string TimeString { get; set; } = string.Empty;
	public string ProductName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public decimal UnitPriceLira { get; set; }
	public decimal TotalPriceLira { get; set; }
	public decimal ProfitLira { get; set; }
	public string PaymentType { get; set; } = "Nakit";
	public string? CustomerName { get; set; }
}

public class CashRegisterSummary
{
	public decimal TotalCashSalesLira { get; set; }
	public decimal TotalDebtSalesLira { get; set; }
	public decimal TotalSalesLira => TotalCashSalesLira + TotalDebtSalesLira;
	public decimal TotalProfitLira { get; set; }
	public int TotalItemsCount { get; set; }
}
