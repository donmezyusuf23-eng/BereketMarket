using System;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class PriceRepository
{
	public static void UpdatePrice(long productId, long expectedOldPriceKurus, long newPriceKurus)
	{
		if (newPriceKurus <= 0 || newPriceKurus > 100000000)
		{
			throw new ArgumentException("Yeni fiyat geçerli değil.");
		}
		if (newPriceKurus == expectedOldPriceKurus)
		{
			throw new ArgumentException("Yeni fiyat mevcut fiyatla aynı.");
		}
		using SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = Database.DatabasePath,
			Mode = SqliteOpenMode.ReadWrite,
			ForeignKeys = true
		}.ToString());
		sqliteConnection.Open();
		using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction();
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.Transaction = sqliteTransaction;
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS PriceHistory\n(\n    Id INTEGER PRIMARY KEY,\n    ProductId INTEGER NOT NULL,\n    OldPriceKurus INTEGER NOT NULL,\n    NewPriceKurus INTEGER NOT NULL\n        CHECK (NewPriceKurus > 0),\n    ChangedAt TEXT NOT NULL,\n    FOREIGN KEY (ProductId) REFERENCES Products(Id)\n);";
			sqliteCommand.ExecuteNonQuery();
		}
		string value = DateTimeOffset.Now.ToString("O");
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "UPDATE Products\nSET\n    SalePriceKurus = $newPrice,\n    UpdatedAt = $now\nWHERE Id = $id\n  AND IsActive = 1\n  AND SalePriceKurus = $oldPrice;";
			sqliteCommand2.Parameters.AddWithValue("$newPrice", newPriceKurus);
			sqliteCommand2.Parameters.AddWithValue("$oldPrice", expectedOldPriceKurus);
			sqliteCommand2.Parameters.AddWithValue("$now", value);
			sqliteCommand2.Parameters.AddWithValue("$id", productId);
			if (sqliteCommand2.ExecuteNonQuery() != 1)
			{
				throw new InvalidOperationException("Ürün bilgisi değişmiş veya ürün artık aktif değil. Pencereyi kapatıp ürün listesini yenile.");
			}
		}
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.Transaction = sqliteTransaction;
			sqliteCommand3.CommandText = "INSERT INTO PriceHistory\n    (ProductId, OldPriceKurus, NewPriceKurus, ChangedAt)\nVALUES\n    ($productId, $oldPrice, $newPrice, $changedAt);";
			sqliteCommand3.Parameters.AddWithValue("$productId", productId);
			sqliteCommand3.Parameters.AddWithValue("$oldPrice", expectedOldPriceKurus);
			sqliteCommand3.Parameters.AddWithValue("$newPrice", newPriceKurus);
			sqliteCommand3.Parameters.AddWithValue("$changedAt", value);
			sqliteCommand3.ExecuteNonQuery();
		}
		sqliteTransaction.Commit();
	}
}
