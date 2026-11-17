using System;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class ProductRepository
{
	public static void Add(string barcode, string name, long priceKurus, int stock, long purchasePriceKurus = 0, int criticalStock = 5)
	{
		using SqliteConnection sqliteConnection = Database.GetConnection();
		using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction();
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.Transaction = sqliteTransaction;
			sqliteCommand.CommandText = @"
CREATE TABLE IF NOT EXISTS InitialStockEntries
(
    ProductId INTEGER PRIMARY KEY,
    Quantity INTEGER NOT NULL CHECK (Quantity >= 0),
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);";
			sqliteCommand.ExecuteNonQuery();
		}
		string now = DateTimeOffset.Now.ToString("O");
		long productId;
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = @"
INSERT INTO Products (Barcode, Name, PurchasePriceKurus, SalePriceKurus, StockQuantity, CriticalStockQuantity, CreatedAt, UpdatedAt, IsActive)
VALUES (@barcode, @name, @purchasePrice, @price, @stock, @criticalStock, @createdAt, @updatedAt, 1)
RETURNING Id;";
			sqliteCommand2.Parameters.AddWithValue("@barcode", barcode);
			sqliteCommand2.Parameters.AddWithValue("@name", name);
			sqliteCommand2.Parameters.AddWithValue("@purchasePrice", purchasePriceKurus);
			sqliteCommand2.Parameters.AddWithValue("@price", priceKurus);
			sqliteCommand2.Parameters.AddWithValue("@stock", stock);
			sqliteCommand2.Parameters.AddWithValue("@criticalStock", criticalStock);
			sqliteCommand2.Parameters.AddWithValue("@createdAt", now);
			sqliteCommand2.Parameters.AddWithValue("@updatedAt", now);
			productId = Convert.ToInt64(sqliteCommand2.ExecuteScalar());
		}
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.Transaction = sqliteTransaction;
			sqliteCommand3.CommandText = "INSERT INTO InitialStockEntries (ProductId, Quantity, CreatedAt) VALUES (@productId, @quantity, @createdAt);";
			sqliteCommand3.Parameters.AddWithValue("@productId", productId);
			sqliteCommand3.Parameters.AddWithValue("@quantity", stock);
			sqliteCommand3.Parameters.AddWithValue("@createdAt", now);
			sqliteCommand3.ExecuteNonQuery();
		}
		sqliteTransaction.Commit();
	}
}
