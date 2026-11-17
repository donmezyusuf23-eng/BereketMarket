namespace BereketMarket;

public class CartItem
{
	public long ProductId { get; set; }

	public string Barcode { get; set; } = "";

	public string Name { get; set; } = "";

	public long UnitPriceKurus { get; set; }

	public int Quantity { get; set; }

	public decimal UnitPrice => (decimal)UnitPriceKurus / 100m;

	public decimal LineTotal => UnitPrice * (decimal)Quantity;
}
