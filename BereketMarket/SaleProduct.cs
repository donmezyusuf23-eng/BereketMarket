namespace BereketMarket;

public class SaleProduct
{
	public long Id { get; set; }

	public string Barcode { get; set; } = "";

	public string Name { get; set; } = "";

	public long PriceKurus { get; set; }

	public int Stock { get; set; }
}
