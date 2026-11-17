using System;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class ProductEditRepository
{
	public static void Update(long productId, string oldBarcode, string oldName, string newBarcode, string newName)
	{
		newBarcode = newBarcode.Trim();
		newName = newName.Trim();
		if (string.IsNullOrWhiteSpace(newBarcode) || newBarcode.Length > 128)
		{
			throw new ArgumentException("Barkod boş olamaz ve 128 karakteri geçemez.");
		}
		if (string.IsNullOrWhiteSpace(newName) || newName.Length > 200)
		{
			throw new ArgumentException("Ürün adı boş olamaz ve 200 karakteri geçemez.");
		}
		if (newBarcode == oldBarcode && newName == oldName)
		{
			throw new ArgumentException("Ürün bilgilerinde değişiklik yapılmadı.");
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
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS ProductInfoHistory\r\n(\r\n    Id INTEGER PRIMARY KEY,\r\n    ProductId INTEGER NOT NULL,\r\n    OldBarcode TEXT NOT NULL,\r\n    NewBarcode TEXT NOT NULL,\r\n    OldName TEXT NOT NULL,\r\n    NewName TEXT NOT NULL,\r\n    ChangedAt TEXT NOT NULL,\r\n    FOREIGN KEY (ProductId) REFERENCES Products(Id)\r\n);";
			sqliteCommand.ExecuteNonQuery();
		}
		string value = DateTimeOffset.Now.ToString("O");
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "UPDATE Products\r\nSET\r\n    Barcode = $newBarcode,\r\n    Name = $newName,\r\n    UpdatedAt = $date\r\nWHERE Id = $id\r\n  AND IsActive = 1\r\n  AND Barcode = $oldBarcode\r\n  AND Name = $oldName;";
			sqliteCommand2.Parameters.AddWithValue("$newBarcode", newBarcode);
			sqliteCommand2.Parameters.AddWithValue("$newName", newName);
			sqliteCommand2.Parameters.AddWithValue("$date", value);
			sqliteCommand2.Parameters.AddWithValue("$id", productId);
			sqliteCommand2.Parameters.AddWithValue("$oldBarcode", oldBarcode);
			sqliteCommand2.Parameters.AddWithValue("$oldName", oldName);
			if (sqliteCommand2.ExecuteNonQuery() != 1)
			{
				throw new InvalidOperationException("Ürün bilgileri değişmiş veya ürün artık aktif değil. Pencereyi kapatıp ürün listesini yenile.");
			}
		}
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.Transaction = sqliteTransaction;
			sqliteCommand3.CommandText = "INSERT INTO ProductInfoHistory\r\n(\r\n    ProductId,\r\n    OldBarcode,\r\n    NewBarcode,\r\n    OldName,\r\n    NewName,\r\n    ChangedAt\r\n)\r\nVALUES\r\n(\r\n    $productId,\r\n    $oldBarcode,\r\n    $newBarcode,\r\n    $oldName,\r\n    $newName,\r\n    $date\r\n);";
			sqliteCommand3.Parameters.AddWithValue("$productId", productId);
			sqliteCommand3.Parameters.AddWithValue("$oldBarcode", oldBarcode);
			sqliteCommand3.Parameters.AddWithValue("$newBarcode", newBarcode);
			sqliteCommand3.Parameters.AddWithValue("$oldName", oldName);
			sqliteCommand3.Parameters.AddWithValue("$newName", newName);
			sqliteCommand3.Parameters.AddWithValue("$date", value);
			sqliteCommand3.ExecuteNonQuery();
		}
		sqliteTransaction.Commit();
	}
}
