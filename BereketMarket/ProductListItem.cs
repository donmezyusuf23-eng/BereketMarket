namespace BereketMarket;

public class ProductListItem
{
	public long Id { get; set; }

	public string Barcode { get; set; } = "";

	public string Name { get; set; } = "";

	public decimal PurchasePrice { get; set; }

	public decimal Price { get; set; }

	public decimal Profit => Price - PurchasePrice;

	public int Stock { get; set; }

	public int CriticalStock { get; set; } = 5;

	public bool IsLowStock => Stock <= CriticalStock;

	public string CreatedAtDisplay { get; set; } = "";

	public bool IsActive { get; set; }

	public string StatusDisplay => !IsActive ? "Pasif" : "Aktif";

	public string StockStatusDisplay
	{
		get
		{
			if (Stock == 0) return "TÜKENDİ";
			if (IsLowStock) return $"KRİTİK ({Stock})";
			return "Yeterli";
		}
	}
}
