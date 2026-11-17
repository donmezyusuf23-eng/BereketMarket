using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class ProductListRepository
{
	public static List<ProductListItem> GetProducts(string search, bool includeInactive = false)
	{
		List<ProductListItem> list = new List<ProductListItem>();
		CultureInfo cultureInfo = CultureInfo.GetCultureInfo("tr-TR");
		using SqliteConnection sqliteConnection = Database.GetConnection();
		using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
		sqliteCommand.CommandText = @"
SELECT
    Id,
    Barcode,
    Name,
    SalePriceKurus,
    StockQuantity,
    CreatedAt,
    IsActive,
    PurchasePriceKurus,
    CriticalStockQuantity
FROM Products
WHERE IsActive = 1 OR @includeInactive = 1
ORDER BY IsActive DESC, Id DESC;";
		sqliteCommand.Parameters.AddWithValue("@includeInactive", includeInactive ? 1 : 0);
		using SqliteDataReader reader = sqliteCommand.ExecuteReader();
		while (reader.Read())
		{
			string barcode = reader.GetString(1);
			string name = reader.GetString(2);
			if (string.IsNullOrWhiteSpace(search) || barcode.Contains(search, StringComparison.OrdinalIgnoreCase) || cultureInfo.CompareInfo.IndexOf(name, search, CompareOptions.IgnoreCase) >= 0)
			{
				string createdAtRaw = reader.GetString(5);
				string createdAtDisplay = createdAtRaw;
				if (DateTimeOffset.TryParse(createdAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
				{
					createdAtDisplay = result.ToLocalTime().ToString("dd.MM.yyyy HH:mm", cultureInfo);
				}

				list.Add(new ProductListItem
				{
					Id = reader.GetInt64(0),
					Barcode = barcode,
					Name = name,
					Price = (decimal)reader.GetInt64(3) / 100m,
					Stock = reader.GetInt32(4),
					CreatedAtDisplay = createdAtDisplay,
					IsActive = (reader.GetInt32(6) == 1),
					PurchasePrice = reader.IsDBNull(7) ? 0 : (decimal)reader.GetInt64(7) / 100m,
					CriticalStock = reader.IsDBNull(8) ? 5 : reader.GetInt32(8)
				});
			}
		}
		return list;
	}
}
