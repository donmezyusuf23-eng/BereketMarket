namespace BereketMarket;

public class SaleHistoryItem
{
	public long Id { get; set; }

	public string DateDisplay { get; set; } = "";

	public long TotalKurus { get; set; }

	public long ItemCount { get; set; }

	public string Status { get; set; } = "";

	public bool IsCompleted => Status == "Completed";

	public string StatusDisplay
	{
		get
		{
			string status = Status;
			if (!(status == "Completed"))
			{
				if (status == "Cancelled")
				{
					return "İptal edildi";
				}
				return Status;
			}
			return "Tamamlandı";
		}
	}

	public decimal Total => (decimal)TotalKurus / 100m;
}
