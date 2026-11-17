using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public class CustomerRepository
{
	public async Task<List<Customer>> GetAllCustomersAsync(string? search = null)
	{
		List<Customer> customers = new List<Customer>();
		await using SqliteConnection connection = Database.GetConnection();

		string sql = @"
SELECT c.Id, c.FullName, c.Phone, c.Note, c.CreatedAt,
       COALESCE(SUM(CASE WHEN t.TransactionType = 'Debt' THEN t.AmountKurus ELSE -t.AmountKurus END), 0) AS BalanceKurus
FROM Customers c
LEFT JOIN CustomerTransactions t ON c.Id = t.CustomerId
WHERE (@Search IS NULL OR c.FullName LIKE @SearchPattern OR c.Phone LIKE @SearchPattern)
GROUP BY c.Id, c.FullName, c.Phone, c.Note, c.CreatedAt
ORDER BY c.FullName ASC;";

		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = sql;
		command.Parameters.AddWithValue("@Search", string.IsNullOrWhiteSpace(search) ? DBNull.Value : search);
		command.Parameters.AddWithValue("@SearchPattern", string.IsNullOrWhiteSpace(search) ? DBNull.Value : $"%{search}%");

		await using SqliteDataReader reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			customers.Add(new Customer
			{
				Id = reader.GetInt32(0),
				FullName = reader.GetString(1),
				Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
				Note = reader.IsDBNull(3) ? null : reader.GetString(3),
				CreatedAt = DateTime.Parse(reader.GetString(4)),
				BalanceLira = reader.GetInt64(5) / 100m
			});
		}

		return customers;
	}

	public async Task<int> AddCustomerAsync(string fullName, string? phone, string? note)
	{
		await using SqliteConnection connection = Database.GetConnection();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = @"
INSERT INTO Customers (FullName, Phone, Note, CreatedAt)
VALUES (@FullName, @Phone, @Note, @CreatedAt);
SELECT last_insert_rowid();";
		command.Parameters.AddWithValue("@FullName", fullName.Trim());
		command.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone.Trim());
		command.Parameters.AddWithValue("@Note", string.IsNullOrWhiteSpace(note) ? DBNull.Value : note.Trim());
		command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));

		object? result = await command.ExecuteScalarAsync();
		return Convert.ToInt32(result);
	}

	public async Task AddPaymentAsync(int customerId, int amountKurus, string? description)
	{
		await using SqliteConnection connection = Database.GetConnection();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = @"
INSERT INTO CustomerTransactions (CustomerId, TransactionType, AmountKurus, Description, CreatedAt)
VALUES (@CustomerId, 'Payment', @AmountKurus, @Description, @CreatedAt);";
		command.Parameters.AddWithValue("@CustomerId", customerId);
		command.Parameters.AddWithValue("@AmountKurus", amountKurus);
		command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? "Nakit Borç Ödemesi" : description.Trim());
		command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));
		await command.ExecuteNonQueryAsync();
	}

	public async Task AddManualDebtAsync(int customerId, int amountKurus, string description)
	{
		await using SqliteConnection connection = Database.GetConnection();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = @"
INSERT INTO CustomerTransactions (CustomerId, TransactionType, AmountKurus, Description, CreatedAt)
VALUES (@CustomerId, 'Debt', @AmountKurus, @Description, @CreatedAt);";
		command.Parameters.AddWithValue("@CustomerId", customerId);
		command.Parameters.AddWithValue("@AmountKurus", amountKurus);
		command.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(description) ? "Eski Borç Kaydı" : description.Trim());
		command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("O"));
		await command.ExecuteNonQueryAsync();
	}

	public async Task<List<CustomerTransaction>> GetTransactionsAsync(int customerId)
	{
		List<CustomerTransaction> transactions = new List<CustomerTransaction>();
		await using SqliteConnection connection = Database.GetConnection();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = @"
SELECT Id, CustomerId, TransactionType, AmountKurus, Description, SaleId, CreatedAt
FROM CustomerTransactions
WHERE CustomerId = @CustomerId
ORDER BY datetime(CreatedAt) DESC;";
		command.Parameters.AddWithValue("@CustomerId", customerId);

		await using SqliteDataReader reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			transactions.Add(new CustomerTransaction
			{
				Id = reader.GetInt32(0),
				CustomerId = reader.GetInt32(1),
				TransactionType = reader.GetString(2),
				AmountKurus = reader.GetInt32(3),
				Description = reader.IsDBNull(4) ? null : reader.GetString(4),
				SaleId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
				CreatedAt = DateTime.Parse(reader.GetString(6))
			});
		}

		return transactions;
	}

	public async Task<List<string>> GetSaleItemLinesAsync(int saleId)
	{
		List<string> lines = new List<string>();
		await using SqliteConnection connection = Database.GetConnection();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = @"
SELECT ProductName, Quantity, LineTotalKurus
FROM SaleItems
WHERE SaleId = @SaleId;";
		command.Parameters.AddWithValue("@SaleId", saleId);

		await using SqliteDataReader reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			string pName = reader.GetString(0);
			int qty = reader.GetInt32(1);
			decimal total = reader.GetInt32(2) / 100m;
			lines.Add($"{qty}x {pName} ({total:N2} TL)");
		}

		return lines;
	}
}
