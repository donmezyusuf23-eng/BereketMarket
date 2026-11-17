using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class SalesHistoryRepository
{
	private static SqliteConnection OpenConnection()
	{
		SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = Database.DatabasePath,
			Mode = SqliteOpenMode.ReadOnly
		}.ToString());
		try
		{
			sqliteConnection.Open();
			return sqliteConnection;
		}
		catch
		{
			sqliteConnection.Dispose();
			throw;
		}
	}

	public static List<SaleHistoryItem> GetSales(DateTime startDate, DateTime endDate)
	{
		List<SaleHistoryItem> list = new List<SaleHistoryItem>();
		using SqliteConnection sqliteConnection = OpenConnection();
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.CommandText = "SELECT COUNT(*)\nFROM sqlite_master\nWHERE type = 'table' AND name = 'Sales';";
			if (Convert.ToInt64(sqliteCommand.ExecuteScalar()) == 0L)
			{
				return list;
			}
		}
		DateTime dateTime = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Local);
		DateTime dateTime2 = DateTime.SpecifyKind(endDate.Date.AddDays(1.0), DateTimeKind.Local);
		string value = new DateTimeOffset(dateTime).ToString("O");
		string value2 = new DateTimeOffset(dateTime2).ToString("O");
		using SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand();
		sqliteCommand2.CommandText = "SELECT\n    s.Id,\n    s.SoldAt,\n    s.TotalKurus,\n    COALESCE(SUM(i.Quantity), 0),\n    s.Status\nFROM Sales s\nLEFT JOIN SaleItems i ON i.SaleId = s.Id\nWHERE julianday(s.SoldAt) >= julianday($from)\n  AND julianday(s.SoldAt) < julianday($until)\nGROUP BY s.Id, s.SoldAt, s.TotalKurus, s.Status\nORDER BY julianday(s.SoldAt) DESC, s.Id DESC;";
		sqliteCommand2.Parameters.AddWithValue("$from", value);
		sqliteCommand2.Parameters.AddWithValue("$until", value2);
		using SqliteDataReader sqliteDataReader = sqliteCommand2.ExecuteReader();
		while (sqliteDataReader.Read())
		{
			string text = sqliteDataReader.GetString(1);
			if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
			{
				text = result.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("tr-TR"));
			}
			list.Add(new SaleHistoryItem
			{
				Id = sqliteDataReader.GetInt64(0),
				DateDisplay = text,
				TotalKurus = sqliteDataReader.GetInt64(2),
				ItemCount = sqliteDataReader.GetInt64(3),
				Status = sqliteDataReader.GetString(4)
			});
		}
		return list;
	}

	public static List<SaleHistoryDetail> GetDetails(long saleId)
	{
		List<SaleHistoryDetail> list = new List<SaleHistoryDetail>();
		using SqliteConnection sqliteConnection = OpenConnection();
		using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
		sqliteCommand.CommandText = "SELECT\n    Barcode,\n    ProductName,\n    Quantity,\n    UnitPriceKurus,\n    LineTotalKurus\nFROM SaleItems\nWHERE SaleId = $saleId\nORDER BY Id;";
		sqliteCommand.Parameters.AddWithValue("$saleId", saleId);
		using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
		while (sqliteDataReader.Read())
		{
			list.Add(new SaleHistoryDetail
			{
				Barcode = sqliteDataReader.GetString(0),
				Name = sqliteDataReader.GetString(1),
				Quantity = sqliteDataReader.GetInt32(2),
				PriceKurus = sqliteDataReader.GetInt64(3),
				TotalKurus = sqliteDataReader.GetInt64(4)
			});
		}
		return list;
	}
}
