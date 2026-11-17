using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public class CashRegisterRepository
{
	public async Task<(CashRegisterSummary Summary, List<CashRegisterItem> Items)> GetDailyRegisterAsync(DateTime date)
	{
		CashRegisterSummary summary = new CashRegisterSummary();
		List<CashRegisterItem> items = new List<CashRegisterItem>();

		string dateStr = date.ToString("yyyy-MM-dd");
		string start = $"{dateStr}T00:00:00";
		string end = $"{dateStr}T23:59:59";

		await using SqliteConnection connection = Database.GetConnection();

		string sumSql = @"
SELECT 
    COALESCE(SUM(CASE WHEN s.PaymentType = 'Cash' THEN s.TotalKurus ELSE 0 END), 0) AS TotalCash,
    COALESCE(SUM(CASE WHEN s.PaymentType = 'Debt' THEN s.TotalKurus ELSE 0 END), 0) AS TotalDebt
FROM Sales s
WHERE s.SoldAt >= @Start AND s.SoldAt <= @End AND s.Status = 'Completed';";

		await using SqliteCommand sumCmd = connection.CreateCommand();
		sumCmd.CommandText = sumSql;
		sumCmd.Parameters.AddWithValue("@Start", start);
		sumCmd.Parameters.AddWithValue("@End", end);

		await using (SqliteDataReader r = await sumCmd.ExecuteReaderAsync())
		{
			if (await r.ReadAsync())
			{
				summary.TotalCashSalesLira = r.GetInt64(0) / 100m;
				summary.TotalDebtSalesLira = r.GetInt64(1) / 100m;
			}
		}

		string itemsSql = @"
SELECT 
    s.SoldAt,
    si.ProductName,
    si.Quantity,
    si.UnitPriceKurus,
    si.LineTotalKurus,
    si.PurchasePriceKurus,
    s.PaymentType,
    c.FullName
FROM SaleItems si
JOIN Sales s ON si.SaleId = s.Id
LEFT JOIN Customers c ON s.CustomerId = c.Id
WHERE s.SoldAt >= @Start AND s.SoldAt <= @End AND s.Status = 'Completed'
ORDER BY datetime(s.SoldAt) DESC;";

		await using SqliteCommand itemsCmd = connection.CreateCommand();
		itemsCmd.CommandText = itemsSql;
		itemsCmd.Parameters.AddWithValue("@Start", start);
		itemsCmd.Parameters.AddWithValue("@End", end);

		decimal totalProfit = 0;
		int totalItems = 0;

		await using (SqliteDataReader reader = await itemsCmd.ExecuteReaderAsync())
		{
			while (await reader.ReadAsync())
			{
				DateTime soldAt = DateTime.Parse(reader.GetString(0));
				string pName = reader.GetString(1);
				int qty = reader.GetInt32(2);
				int unitPriceKurus = reader.GetInt32(3);
				int lineTotalKurus = reader.GetInt32(4);
				int purchasePriceKurus = reader.GetInt32(5);
				string paymentType = reader.GetString(6);
				string? customerName = reader.IsDBNull(7) ? null : reader.GetString(7);

				decimal profit = (lineTotalKurus - (purchasePriceKurus * qty)) / 100m;
				totalProfit += profit;
				totalItems += qty;

				items.Add(new CashRegisterItem
				{
					TimeString = soldAt.ToLocalTime().ToString("HH:mm:ss"),
					ProductName = pName,
					Quantity = qty,
					UnitPriceLira = unitPriceKurus / 100m,
					TotalPriceLira = lineTotalKurus / 100m,
					ProfitLira = profit,
					PaymentType = paymentType == "Debt" ? "Veresiye" : "Nakit",
					CustomerName = customerName
				});
			}
		}

		summary.TotalProfitLira = totalProfit;
		summary.TotalItemsCount = totalItems;

		return (summary, items);
	}
}
