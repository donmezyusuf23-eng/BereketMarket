using System;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class StockRepository
{
	public static int AddStock(long productId, int quantity)
	{
		if (quantity <= 0)
		{
			throw new ArgumentException("Eklenecek adet sıfırdan büyük olmalı.");
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
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS StockEntries\n(\n    Id INTEGER PRIMARY KEY,\n    ProductId INTEGER NOT NULL,\n    Quantity INTEGER NOT NULL CHECK (Quantity > 0),\n    StockAfter INTEGER NOT NULL CHECK (StockAfter >= 0),\n    CreatedAt TEXT NOT NULL,\n    FOREIGN KEY (ProductId) REFERENCES Products(Id)\n);";
			sqliteCommand.ExecuteNonQuery();
		}
		string value = DateTimeOffset.Now.ToString("O");
		int num;
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "UPDATE Products\nSET\n    StockQuantity = StockQuantity + $quantity,\n    UpdatedAt = $now\nWHERE Id = $id\n  AND IsActive = 1\n  AND StockQuantity <= $maximumBefore\nRETURNING StockQuantity;";
			sqliteCommand2.Parameters.AddWithValue("$quantity", quantity);
			sqliteCommand2.Parameters.AddWithValue("$now", value);
			sqliteCommand2.Parameters.AddWithValue("$id", productId);
			sqliteCommand2.Parameters.AddWithValue("$maximumBefore", int.MaxValue - quantity);
			object obj = sqliteCommand2.ExecuteScalar();
			if (obj == null || obj == DBNull.Value)
			{
				throw new InvalidOperationException("Ürün bulunamadı, pasif durumda veya stok sınırı aşılıyor.");
			}
			num = Convert.ToInt32(obj);
		}
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.Transaction = sqliteTransaction;
			sqliteCommand3.CommandText = "INSERT INTO StockEntries\n    (ProductId, Quantity, StockAfter, CreatedAt)\nVALUES\n    ($productId, $quantity, $stockAfter, $createdAt);";
			sqliteCommand3.Parameters.AddWithValue("$productId", productId);
			sqliteCommand3.Parameters.AddWithValue("$quantity", quantity);
			sqliteCommand3.Parameters.AddWithValue("$stockAfter", num);
			sqliteCommand3.Parameters.AddWithValue("$createdAt", value);
			sqliteCommand3.ExecuteNonQuery();
		}
		sqliteTransaction.Commit();
		return num;
	}
}
