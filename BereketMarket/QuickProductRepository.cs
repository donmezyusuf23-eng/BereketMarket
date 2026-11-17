using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public class QuickProductRepository
{
	public async Task<List<QuickProduct>> GetQuickProductsAsync()
	{
		List<QuickProduct> list = new List<QuickProduct>();
		await using SqliteConnection connection = Database.GetConnection();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = "SELECT Id, Name, PriceKurus, Barcode, ColorHex, DisplayOrder FROM QuickProducts ORDER BY DisplayOrder ASC;";

		await using SqliteDataReader reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			list.Add(new QuickProduct
			{
				Id = reader.GetInt32(0),
				Name = reader.GetString(1),
				PriceKurus = reader.GetInt32(2),
				Barcode = reader.IsDBNull(3) ? null : reader.GetString(3),
				ColorHex = reader.GetString(4),
				DisplayOrder = reader.GetInt32(5)
			});
		}

		return list;
	}
}
