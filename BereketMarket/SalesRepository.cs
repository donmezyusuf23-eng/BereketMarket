using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class SalesRepository
{
	public static SaleProduct? FindByBarcode(string barcode)
	{
		using SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = Database.DatabasePath,
			Mode = SqliteOpenMode.ReadOnly
		}.ToString());
		sqliteConnection.Open();
		using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
		sqliteCommand.CommandText = "SELECT\n    Id,\n    Barcode,\n    Name,\n    SalePriceKurus,\n    StockQuantity\nFROM Products\nWHERE Barcode = $barcode\n  AND IsActive = 1;";
		sqliteCommand.Parameters.AddWithValue("$barcode", barcode);
		using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
		if (!sqliteDataReader.Read())
		{
			return null;
		}
		return new SaleProduct
		{
			Id = sqliteDataReader.GetInt64(0),
			Barcode = sqliteDataReader.GetString(1),
			Name = sqliteDataReader.GetString(2),
			PriceKurus = sqliteDataReader.GetInt64(3),
			Stock = sqliteDataReader.GetInt32(4)
		};
	}
}
