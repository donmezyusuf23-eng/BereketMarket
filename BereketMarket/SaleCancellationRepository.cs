using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class SaleCancellationRepository
{
	public static void Cancel(long saleId, string reason)
	{
		reason = reason.Trim();
		if (reason.Length < 3 || reason.Length > 500)
		{
			throw new ArgumentException("İptal gerekçesi 3–500 karakter arasında olmalı.");
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
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS SaleCancellations\n(\n    Id INTEGER PRIMARY KEY,\n    SaleId INTEGER NOT NULL UNIQUE,\n    Reason TEXT NOT NULL,\n    CancelledAt TEXT NOT NULL,\n    FOREIGN KEY (SaleId) REFERENCES Sales(Id)\n);\n\nCREATE TABLE IF NOT EXISTS SaleCancellationItems\n(\n    CancellationId INTEGER NOT NULL,\n    ProductId INTEGER NOT NULL,\n    Quantity INTEGER NOT NULL CHECK (Quantity > 0),\n    StockAfter INTEGER NOT NULL CHECK (StockAfter >= 0),\n    PRIMARY KEY (CancellationId, ProductId),\n    FOREIGN KEY (CancellationId)\n        REFERENCES SaleCancellations(Id),\n    FOREIGN KEY (ProductId) REFERENCES Products(Id)\n);";
			sqliteCommand.ExecuteNonQuery();
		}
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "UPDATE Sales\nSET Status = 'Cancelled'\nWHERE Id = $id AND Status = 'Completed';";
			sqliteCommand2.Parameters.AddWithValue("$id", saleId);
			if (sqliteCommand2.ExecuteNonQuery() != 1)
			{
				throw new InvalidOperationException("Satış bulunamadı veya zaten iptal edilmiş.");
			}
		}
		List<(long, int)> list = new List<(long, int)>();
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.Transaction = sqliteTransaction;
			sqliteCommand3.CommandText = "SELECT ProductId, Quantity\nFROM SaleItems\nWHERE SaleId = $id;";
			sqliteCommand3.Parameters.AddWithValue("$id", saleId);
			using SqliteDataReader sqliteDataReader = sqliteCommand3.ExecuteReader();
			while (sqliteDataReader.Read())
			{
				int @int = sqliteDataReader.GetInt32(1);
				if (@int <= 0)
				{
					throw new InvalidOperationException("Satışta geçersiz ürün adedi bulundu.");
				}
				list.Add((sqliteDataReader.GetInt64(0), @int));
			}
		}
		if (list.Count == 0)
		{
			throw new InvalidOperationException("Satışın ürün kayıtları bulunamadı.");
		}
		string value = DateTimeOffset.Now.ToString("O");
		long num;
		using (SqliteCommand sqliteCommand4 = sqliteConnection.CreateCommand())
		{
			sqliteCommand4.Transaction = sqliteTransaction;
			sqliteCommand4.CommandText = "INSERT INTO SaleCancellations\n    (SaleId, Reason, CancelledAt)\nVALUES ($saleId, $reason, $date)\nRETURNING Id;";
			sqliteCommand4.Parameters.AddWithValue("$saleId", saleId);
			sqliteCommand4.Parameters.AddWithValue("$reason", reason);
			sqliteCommand4.Parameters.AddWithValue("$date", value);
			num = Convert.ToInt64(sqliteCommand4.ExecuteScalar());
		}
		foreach (var item in list)
		{
			int num2;
			using (SqliteCommand sqliteCommand5 = sqliteConnection.CreateCommand())
			{
				sqliteCommand5.Transaction = sqliteTransaction;
				sqliteCommand5.CommandText = "UPDATE Products\nSET\n    StockQuantity = StockQuantity + $quantity,\n    UpdatedAt = $date\nWHERE Id = $productId\n  AND StockQuantity <= $maximumBefore\nRETURNING StockQuantity;";
				sqliteCommand5.Parameters.AddWithValue("$quantity", item.Item2);
				sqliteCommand5.Parameters.AddWithValue("$date", value);
				sqliteCommand5.Parameters.AddWithValue("$productId", item.Item1);
				sqliteCommand5.Parameters.AddWithValue("$maximumBefore", int.MaxValue - item.Item2);
				object obj = sqliteCommand5.ExecuteScalar();
				if (obj == null || obj == DBNull.Value)
				{
					throw new InvalidOperationException("Bir ürünün stoğu geri yüklenemedi. İptal işlemi uygulanmadı.");
				}
				num2 = Convert.ToInt32(obj);
			}
			using SqliteCommand sqliteCommand6 = sqliteConnection.CreateCommand();
			sqliteCommand6.Transaction = sqliteTransaction;
			sqliteCommand6.CommandText = "INSERT INTO SaleCancellationItems\n    (CancellationId, ProductId, Quantity, StockAfter)\nVALUES\n    ($cancellationId, $productId, $quantity, $stockAfter);";
			sqliteCommand6.Parameters.AddWithValue("$cancellationId", num);
			sqliteCommand6.Parameters.AddWithValue("$productId", item.Item1);
			sqliteCommand6.Parameters.AddWithValue("$quantity", item.Item2);
			sqliteCommand6.Parameters.AddWithValue("$stockAfter", num2);
			sqliteCommand6.ExecuteNonQuery();
		}
		sqliteTransaction.Commit();
	}
}
