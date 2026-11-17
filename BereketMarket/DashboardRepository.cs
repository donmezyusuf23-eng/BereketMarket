using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class DashboardRepository
{
	public const int LowStockThreshold = 5;

	public static DashboardData Load()
	{
		DateTime now = DateTime.Now;
		DateTime date = now.Date;
		DateTime dateTime = new DateTime(date.Year, date.Month, 1);
		DateTime localDate = date.AddDays(1.0);
		DashboardData dashboardData = new DashboardData
		{
			LoadedAt = now
		};
		using SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = Database.DatabasePath,
			Mode = SqliteOpenMode.ReadOnly,
			Pooling = false
		}.ToString());
		sqliteConnection.Open();
		using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction(deferred: true);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.Transaction = sqliteTransaction;
			sqliteCommand.CommandText = "SELECT name\r\nFROM sqlite_master\r\nWHERE type = 'table';";
			using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
			while (sqliteDataReader.Read())
			{
				hashSet.Add(sqliteDataReader.GetString(0));
			}
		}
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "SELECT Barcode, Name, StockQuantity\r\nFROM Products\r\nWHERE IsActive = 1\r\n  AND StockQuantity <= $threshold\r\nORDER BY StockQuantity, Name;";
			sqliteCommand2.Parameters.AddWithValue("$threshold", 5);
			using SqliteDataReader sqliteDataReader2 = sqliteCommand2.ExecuteReader();
			while (sqliteDataReader2.Read())
			{
				dashboardData.LowStockProducts.Add(new LowStockItem
				{
					Barcode = sqliteDataReader2.GetString(0),
					Name = sqliteDataReader2.GetString(1),
					Stock = sqliteDataReader2.GetInt32(2)
				});
			}
		}
		Dictionary<string, (long, decimal)> dictionary = new Dictionary<string, (long, decimal)>();
		if (hashSet.Contains("Sales"))
		{
			using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
			{
				sqliteCommand3.Transaction = sqliteTransaction;
				sqliteCommand3.CommandText = "SELECT\r\n    strftime('%Y-%m-%d', SoldAt, 'localtime'),\r\n    COUNT(*),\r\n    SUM(TotalKurus)\r\nFROM Sales\r\nWHERE Status = 'Completed'\r\n  AND julianday(SoldAt) >= julianday($start)\r\n  AND julianday(SoldAt) < julianday($end)\r\nGROUP BY strftime('%Y-%m-%d', SoldAt, 'localtime')\r\nORDER BY 1;";
				sqliteCommand3.Parameters.AddWithValue("$start", ToTimestamp(dateTime));
				sqliteCommand3.Parameters.AddWithValue("$end", ToTimestamp(localDate));
				using SqliteDataReader sqliteDataReader3 = sqliteCommand3.ExecuteReader();
				while (sqliteDataReader3.Read())
				{
					string key = sqliteDataReader3.GetString(0);
					long @int = sqliteDataReader3.GetInt64(1);
					decimal item = (decimal)sqliteDataReader3.GetInt64(2) / 100m;
					dictionary[key] = (@int, item);
				}
			}
			if (hashSet.Contains("SaleItems"))
			{
				using SqliteCommand sqliteCommand4 = sqliteConnection.CreateCommand();
				sqliteCommand4.Transaction = sqliteTransaction;
				sqliteCommand4.CommandText = "SELECT\r\n    COALESCE(p.Name, MAX(i.ProductName)),\r\n    SUM(i.Quantity),\r\n    SUM(i.LineTotalKurus)\r\nFROM SaleItems i\r\nINNER JOIN Sales s ON s.Id = i.SaleId\r\nLEFT JOIN Products p ON p.Id = i.ProductId\r\nWHERE s.Status = 'Completed'\r\n  AND julianday(s.SoldAt) >= julianday($start)\r\n  AND julianday(s.SoldAt) < julianday($end)\r\nGROUP BY i.ProductId, p.Name\r\nORDER BY\r\n    SUM(i.Quantity) DESC,\r\n    SUM(i.LineTotalKurus) DESC,\r\n    i.ProductId\r\nLIMIT 10;";
				sqliteCommand4.Parameters.AddWithValue("$start", ToTimestamp(dateTime));
				sqliteCommand4.Parameters.AddWithValue("$end", ToTimestamp(localDate));
				using SqliteDataReader sqliteDataReader4 = sqliteCommand4.ExecuteReader();
				while (sqliteDataReader4.Read())
				{
					dashboardData.TopProducts.Add(new TopSellingItem
					{
						Name = sqliteDataReader4.GetString(0),
						Quantity = sqliteDataReader4.GetInt64(1),
						Total = (decimal)sqliteDataReader4.GetInt64(2) / 100m
					});
				}
			}
		}
		DateTime dateTime2 = dateTime;
		while (dateTime2 <= date)
		{
			string key2 = dateTime2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			dictionary.TryGetValue(key2, out var value);
			List<DailySalesSummary> dailySales = dashboardData.DailySales;
			DailySalesSummary dailySalesSummary = new DailySalesSummary
			{
				DateDisplay = dateTime2.ToString("dd.MM.yyyy")
			};
			(dailySalesSummary.TransactionCount, dailySalesSummary.Total) = value;
			dailySales.Add(dailySalesSummary);
			dashboardData.MonthTotal += value.Item2;
			if (dateTime2 == date)
			{
				dashboardData.TodayTotal = value.Item2;
				(dashboardData.TodayTransactionCount, _) = value;
			}
			dateTime2 = dateTime2.AddDays(1.0);
		}
		dashboardData.DailySales.Reverse();
		sqliteTransaction.Commit();
		return dashboardData;
	}

	private static string ToTimestamp(DateTime localDate)
	{
		return new DateTimeOffset(DateTime.SpecifyKind(localDate, DateTimeKind.Local)).ToString("O");
	}
}
