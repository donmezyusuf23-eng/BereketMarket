using System;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class ProductStatusRepository
{
	public static void ChangeStatus(long productId, bool expectedActive, bool newActive)
	{
		if (expectedActive == newActive)
		{
			throw new ArgumentException("Ürünün durumu zaten seçilen durumda.");
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
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS ProductStatusHistory\n(\n    Id INTEGER PRIMARY KEY,\n    ProductId INTEGER NOT NULL,\n    OldIsActive INTEGER NOT NULL\n        CHECK (OldIsActive IN (0, 1)),\n    NewIsActive INTEGER NOT NULL\n        CHECK (NewIsActive IN (0, 1)),\n    ChangedAt TEXT NOT NULL,\n    FOREIGN KEY (ProductId) REFERENCES Products(Id)\n);";
			sqliteCommand.ExecuteNonQuery();
		}
		string value = DateTimeOffset.Now.ToString("O");
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "UPDATE Products\nSET\n    IsActive = $newActive,\n    UpdatedAt = $date\nWHERE Id = $id\n  AND IsActive = $expectedActive;";
			sqliteCommand2.Parameters.AddWithValue("$newActive", newActive ? 1 : 0);
			sqliteCommand2.Parameters.AddWithValue("$date", value);
			sqliteCommand2.Parameters.AddWithValue("$id", productId);
			sqliteCommand2.Parameters.AddWithValue("$expectedActive", expectedActive ? 1 : 0);
			if (sqliteCommand2.ExecuteNonQuery() != 1)
			{
				throw new InvalidOperationException("Ürünün durumu değişmiş veya ürün bulunamıyor. Listeyi yenileyip tekrar dene.");
			}
		}
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.Transaction = sqliteTransaction;
			sqliteCommand3.CommandText = "INSERT INTO ProductStatusHistory\n    (ProductId, OldIsActive, NewIsActive, ChangedAt)\nVALUES\n    ($id, $oldActive, $newActive, $date);";
			sqliteCommand3.Parameters.AddWithValue("$id", productId);
			sqliteCommand3.Parameters.AddWithValue("$oldActive", expectedActive ? 1 : 0);
			sqliteCommand3.Parameters.AddWithValue("$newActive", newActive ? 1 : 0);
			sqliteCommand3.Parameters.AddWithValue("$date", value);
			sqliteCommand3.ExecuteNonQuery();
		}
		sqliteTransaction.Commit();
	}
}
