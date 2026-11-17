using System;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class StockAdjustmentRepository
{
	public static void Adjust(long productId, int expectedStock, int actualStock, string reason)
	{
		reason = reason.Trim();
		if (actualStock < 0)
		{
			throw new ArgumentException("Stok negatif olamaz.");
		}
		if (reason.Length < 3 || reason.Length > 500)
		{
			throw new ArgumentException("Gerekçe 3–500 karakter arasında olmalı.");
		}
		if (actualStock == expectedStock)
		{
			throw new ArgumentException("Yeni miktar mevcut stokla aynı.");
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
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS StockAdjustments\n(\n    Id INTEGER PRIMARY KEY,\n    ProductId INTEGER NOT NULL,\n    StockBefore INTEGER NOT NULL\n        CHECK (StockBefore >= 0),\n    StockAfter INTEGER NOT NULL\n        CHECK (StockAfter >= 0),\n    QuantityChange INTEGER NOT NULL,\n    Reason TEXT NOT NULL,\n    CreatedAt TEXT NOT NULL,\n    CHECK (QuantityChange = StockAfter - StockBefore),\n    CHECK (QuantityChange <> 0),\n    FOREIGN KEY (ProductId) REFERENCES Products(Id)\n);";
			sqliteCommand.ExecuteNonQuery();
		}
		string value = DateTimeOffset.Now.ToString("O");
		long num = (long)actualStock - (long)expectedStock;
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "UPDATE Products\nSET\n    StockQuantity = $actual,\n    UpdatedAt = $date\nWHERE Id = $id\n  AND IsActive = 1\n  AND StockQuantity = $expected;";
			sqliteCommand2.Parameters.AddWithValue("$actual", actualStock);
			sqliteCommand2.Parameters.AddWithValue("$date", value);
			sqliteCommand2.Parameters.AddWithValue("$id", productId);
			sqliteCommand2.Parameters.AddWithValue("$expected", expectedStock);
			if (sqliteCommand2.ExecuteNonQuery() != 1)
			{
				throw new InvalidOperationException("Stok miktarı değişmiş veya ürün artık aktif değil. Pencereyi kapatıp ürün listesini yenile ve tekrar dene.");
			}
		}
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.Transaction = sqliteTransaction;
			sqliteCommand3.CommandText = "INSERT INTO StockAdjustments\n(\n    ProductId,\n    StockBefore,\n    StockAfter,\n    QuantityChange,\n    Reason,\n    CreatedAt\n)\nVALUES\n(\n    $productId,\n    $before,\n    $after,\n    $difference,\n    $reason,\n    $date\n);";
			sqliteCommand3.Parameters.AddWithValue("$productId", productId);
			sqliteCommand3.Parameters.AddWithValue("$before", expectedStock);
			sqliteCommand3.Parameters.AddWithValue("$after", actualStock);
			sqliteCommand3.Parameters.AddWithValue("$difference", num);
			sqliteCommand3.Parameters.AddWithValue("$reason", reason);
			sqliteCommand3.Parameters.AddWithValue("$date", value);
			sqliteCommand3.ExecuteNonQuery();
		}
		sqliteTransaction.Commit();
	}
}
