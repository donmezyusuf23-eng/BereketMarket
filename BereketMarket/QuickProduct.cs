namespace BereketMarket;

public class QuickProduct
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public int PriceKurus { get; set; }
	public string? Barcode { get; set; }
	public string ColorHex { get; set; } = "#008C95";
	public int DisplayOrder { get; set; }
	public decimal PriceLira => PriceKurus / 100m;
}
