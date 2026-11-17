namespace BereketMarket;

public class LowStockItem
{
	public string Barcode { get; set; } = "";

	public string Name { get; set; } = "";

	public int Stock { get; set; }

	public string StockStatus
	{
		get
		{
			if (Stock != 0)
			{
				return "Azaldı";
			}
			return "Tükendi";
		}
	}
}
