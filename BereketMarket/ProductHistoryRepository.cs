using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class ProductHistoryRepository
{
	public static ProductHistoryResult GetHistory(long productId)
	{
		ProductHistoryResult productHistoryResult = new ProductHistoryResult();
		CultureInfo cultureInfo = CultureInfo.GetCultureInfo("tr-TR");
		using SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = Database.DatabasePath,
			Mode = SqliteOpenMode.ReadOnly
		}.ToString());
		sqliteConnection.Open();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
			using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
			while (sqliteDataReader.Read())
			{
				hashSet.Add(sqliteDataReader.GetString(0));
			}
		}
		List<string> list = new List<string>();
		if (hashSet.Contains("InitialStockEntries"))
		{
			list.Add("SELECT\r\n    CreatedAt AS EventDate,\r\n    'İlk stok' AS Operation,\r\n    Quantity AS QuantityChange,\r\n    Quantity AS StockAfter,\r\n    NULL AS OldPrice,\r\n    NULL AS NewPrice,\r\n    'Ürün ilk kez kaydedildi.' AS Note,\r\n    1 AS IsInitial\r\nFROM InitialStockEntries\r\nWHERE ProductId = $productId");
		}
		if (hashSet.Contains("StockEntries"))
		{
			list.Add("SELECT\r\n    CreatedAt,\r\n    'Stok girişi',\r\n    Quantity,\r\n    StockAfter,\r\n    NULL,\r\n    NULL,\r\n    'Yeni mal girişi',\r\n    0\r\nFROM StockEntries\r\nWHERE ProductId = $productId");
		}
		if (hashSet.Contains("Sales") && hashSet.Contains("SaleItems"))
		{
			list.Add("SELECT\r\n    s.SoldAt,\r\n    'Satış',\r\n    -i.Quantity,\r\n    i.StockAfter,\r\n    NULL,\r\n    NULL,\r\n    'Satış #' || s.Id ||\r\n    CASE\r\n        WHEN s.Status = 'Cancelled'\r\n        THEN ' — sonradan iptal edildi'\r\n        ELSE ''\r\n    END,\r\n    0\r\nFROM SaleItems i\r\nINNER JOIN Sales s ON s.Id = i.SaleId\r\nWHERE i.ProductId = $productId");
		}
		if (hashSet.Contains("SaleCancellations") && hashSet.Contains("SaleCancellationItems"))
		{
			list.Add("SELECT\r\n    c.CancelledAt,\r\n    'Satış iptali',\r\n    i.Quantity,\r\n    i.StockAfter,\r\n    NULL,\r\n    NULL,\r\n    'Satış #' || c.SaleId || ' — ' || c.Reason,\r\n    0\r\nFROM SaleCancellationItems i\r\nINNER JOIN SaleCancellations c\r\n    ON c.Id = i.CancellationId\r\nWHERE i.ProductId = $productId");
		}
		if (hashSet.Contains("PriceHistory"))
		{
			list.Add("SELECT\r\n    ChangedAt,\r\n    'Fiyat değişikliği',\r\n    NULL,\r\n    NULL,\r\n    OldPriceKurus,\r\n    NewPriceKurus,\r\n    'Stok adedi değişmedi.',\r\n    0\r\nFROM PriceHistory\r\nWHERE ProductId = $productId");
		}
		if (hashSet.Contains("ProductInfoHistory"))
		{
			list.Add("SELECT\r\n    ChangedAt,\r\n    'Bilgi düzenleme',\r\n    NULL,\r\n    NULL,\r\n    NULL,\r\n    NULL,\r\n    CASE\r\n        WHEN OldName <> NewName\r\n        THEN 'Ad: ' || OldName || ' → ' || NewName\r\n        ELSE ''\r\n    END\r\n    ||\r\n    CASE\r\n        WHEN OldName <> NewName AND OldBarcode <> NewBarcode\r\n        THEN ' | '\r\n        ELSE ''\r\n    END\r\n    ||\r\n    CASE\r\n        WHEN OldBarcode <> NewBarcode\r\n        THEN 'Barkod: ' || OldBarcode || ' → ' || NewBarcode\r\n        ELSE ''\r\n    END,\r\n    0\r\nFROM ProductInfoHistory\r\nWHERE ProductId = $productId");
		}
		if (hashSet.Contains("StockAdjustments"))
		{
			list.Add("SELECT\r\n    CreatedAt,\r\n    'Stok düzeltme',\r\n    QuantityChange,\r\n    StockAfter,\r\n    NULL,\r\n    NULL,\r\n    Reason || ' | Önceki stok: ' || StockBefore || ' adet',\r\n    0\r\nFROM StockAdjustments\r\nWHERE ProductId = $productId");
		}
		if (hashSet.Contains("ProductStatusHistory"))
		{
			list.Add("SELECT\r\n    ChangedAt,\r\n    CASE\r\n        WHEN NewIsActive = 1 THEN 'Aktifleştirme'\r\n        ELSE 'Pasife alma'\r\n    END,\r\n    NULL,\r\n    NULL,\r\n    NULL,\r\n    NULL,\r\n    CASE\r\n        WHEN NewIsActive = 1\r\n        THEN 'Ürün yeniden satışa açıldı.'\r\n        ELSE 'Ürün satışa kapatıldı. Stok korunuyor.'\r\n    END,\r\n    0\r\nFROM ProductStatusHistory\r\nWHERE ProductId = $productId");
		}
		if (list.Count == 0)
		{
			return productHistoryResult;
		}
		using SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand();
		sqliteCommand2.CommandText = string.Join("\nUNION ALL\n", list);
		sqliteCommand2.Parameters.AddWithValue("$productId", productId);
		using SqliteDataReader sqliteDataReader2 = sqliteCommand2.ExecuteReader();
		while (sqliteDataReader2.Read())
		{
			string dateDisplay;
			DateTimeOffset.TryParse(dateDisplay = sqliteDataReader2.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
			if (result != default(DateTimeOffset))
			{
				dateDisplay = result.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", cultureInfo);
			}
			string changeDisplay;
			if (sqliteDataReader2.IsDBNull(4))
			{
				changeDisplay = (sqliteDataReader2.IsDBNull(2) ? "Stok değişmedi" : (sqliteDataReader2.GetInt64(2).ToString("+0;-0;0", cultureInfo) + " adet"));
			}
			else
			{
				decimal num = (decimal)sqliteDataReader2.GetInt64(4) / 100m;
				decimal num2 = (decimal)sqliteDataReader2.GetInt64(5) / 100m;
				changeDisplay = num.ToString("N2", cultureInfo) + " → " + num2.ToString("N2", cultureInfo) + " TL";
			}
			string stockAfterDisplay = (sqliteDataReader2.IsDBNull(3) ? "—" : (sqliteDataReader2.GetInt64(3).ToString(cultureInfo) + " adet"));
			if (sqliteDataReader2.GetInt64(7) == 1)
			{
				productHistoryResult.HasInitialStockRecord = true;
			}
			productHistoryResult.Items.Add(new ProductHistoryItem
			{
				OccurredAt = result,
				DateDisplay = dateDisplay,
				Operation = sqliteDataReader2.GetString(1),
				ChangeDisplay = changeDisplay,
				StockAfterDisplay = stockAfterDisplay,
				Note = sqliteDataReader2.GetString(6)
			});
		}
		productHistoryResult.Items = productHistoryResult.Items.OrderByDescending((ProductHistoryItem item) => item.OccurredAt).ToList();
		return productHistoryResult;
	}
}
