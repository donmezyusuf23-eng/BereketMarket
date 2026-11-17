using System.Collections.Generic;

namespace BereketMarket;

public class ProductHistoryResult
{
	public List<ProductHistoryItem> Items { get; set; } = new List<ProductHistoryItem>();

	public bool HasInitialStockRecord { get; set; }
}
