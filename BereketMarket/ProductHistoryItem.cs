using System;

namespace BereketMarket;

public class ProductHistoryItem
{
	public DateTimeOffset OccurredAt { get; set; }

	public string DateDisplay { get; set; } = "";

	public string Operation { get; set; } = "";

	public string ChangeDisplay { get; set; } = "";

	public string StockAfterDisplay { get; set; } = "";

	public string Note { get; set; } = "";
}
