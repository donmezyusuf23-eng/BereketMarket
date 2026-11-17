using System;
using System.Collections.Generic;

namespace BereketMarket;

public class DashboardData
{
	public DateTime LoadedAt { get; set; }

	public decimal TodayTotal { get; set; }

	public decimal MonthTotal { get; set; }

	public long TodayTransactionCount { get; set; }

	public List<DailySalesSummary> DailySales { get; set; } = new List<DailySalesSummary>();

	public List<LowStockItem> LowStockProducts { get; set; } = new List<LowStockItem>();

	public List<TopSellingItem> TopProducts { get; set; } = new List<TopSellingItem>();
}
