namespace BereketMarket;

public class SaleHistoryDetail
{
	public string Barcode { get; set; } = "";

	public string Name { get; set; } = "";

	public int Quantity { get; set; }

	public long PriceKurus { get; set; }

	public long TotalKurus { get; set; }

	public decimal UnitPrice => (decimal)PriceKurus / 100m;

	public decimal Total => (decimal)TotalKurus / 100m;
}
