using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class CheckoutRepository
{
	public static CheckoutResult Complete(IReadOnlyList<CheckoutLine> lines, string paymentType = "Cash", int? customerId = null)
	{
		if (lines.Count == 0)
		{
			throw new InvalidOperationException("Sepet boş.");
		}
		if (lines.Select((CheckoutLine x) => x.ProductId).Distinct().Count() != lines.Count)
		{
			throw new InvalidOperationException("Sepette yinelenen ürün satırı var.");
		}

		long totalKurus = 0L;
		foreach (CheckoutLine line in lines)
		{
			if (line.Quantity <= 0 || line.UnitPriceKurus <= 0)
			{
				throw new InvalidOperationException("Sepette geçersiz adet veya fiyat var.");
			}
			totalKurus = checked(totalKurus + line.UnitPriceKurus * line.Quantity);
		}

		using SqliteConnection connection = Database.GetConnection();
		using SqliteTransaction transaction = connection.BeginTransaction();

		string now = DateTimeOffset.Now.ToString("O");
		long saleId;
		using (SqliteCommand command = connection.CreateCommand())
		{
			command.Transaction = transaction;
			command.CommandText = @"
INSERT INTO Sales (SoldAt, TotalKurus, PaymentType, CustomerId, Status)
VALUES (@SoldAt, @TotalKurus, @PaymentType, @CustomerId, 'Completed')
RETURNING Id;";
			command.Parameters.AddWithValue("@SoldAt", now);
			command.Parameters.AddWithValue("@TotalKurus", totalKurus);
			command.Parameters.AddWithValue("@PaymentType", paymentType);
			command.Parameters.AddWithValue("@CustomerId", customerId.HasValue ? customerId.Value : DBNull.Value);
			saleId = Convert.ToInt64(command.ExecuteScalar());
		}

		if (paymentType == "Debt" && customerId.HasValue)
		{
			using SqliteCommand debtCmd = connection.CreateCommand();
			debtCmd.Transaction = transaction;
			debtCmd.CommandText = @"
INSERT INTO CustomerTransactions (CustomerId, TransactionType, AmountKurus, Description, SaleId, CreatedAt)
VALUES (@CustomerId, 'Debt', @AmountKurus, @Description, @SaleId, @CreatedAt);";
			debtCmd.Parameters.AddWithValue("@CustomerId", customerId.Value);
			debtCmd.Parameters.AddWithValue("@AmountKurus", totalKurus);
			debtCmd.Parameters.AddWithValue("@Description", $"Kasa Satışı (Fiş #{saleId})");
			debtCmd.Parameters.AddWithValue("@SaleId", saleId);
			debtCmd.Parameters.AddWithValue("@CreatedAt", now);
			debtCmd.ExecuteNonQuery();
		}

		foreach (CheckoutLine line in lines)
		{
			string barcode;
			string name;
			long currentPrice;
			int currentStock;
			int purchasePrice;
			bool isActive;

			using (SqliteCommand selectCmd = connection.CreateCommand())
			{
				selectCmd.Transaction = transaction;
				selectCmd.CommandText = "SELECT Barcode, Name, SalePriceKurus, StockQuantity, IsActive, PurchasePriceKurus FROM Products WHERE Id = @Id;";
				selectCmd.Parameters.AddWithValue("@Id", line.ProductId);
				using SqliteDataReader reader = selectCmd.ExecuteReader();
				if (!reader.Read())
				{
					throw new InvalidOperationException("Sepetteki bir ürün artık bulunamıyor.");
				}
				barcode = reader.GetString(0);
				name = reader.GetString(1);
				currentPrice = reader.GetInt64(2);
				currentStock = reader.GetInt32(3);
				isActive = reader.GetInt32(4) == 1;
				purchasePrice = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
			}

			if (!isActive)
			{
				throw new InvalidOperationException(name + " artık aktif değil.");
			}
			if (currentPrice != line.UnitPriceKurus)
			{
				throw new InvalidOperationException(name + " ürününün fiyatı değişmiş. Ürünü sepetten kaldırıp yeniden okut.");
			}
			if (currentStock < line.Quantity)
			{
				throw new InvalidOperationException($"{name} için stok yetersiz.\nMevcut: {currentStock}, sepette: {line.Quantity}");
			}

			int stockAfter = currentStock - line.Quantity;

			using (SqliteCommand updateCmd = connection.CreateCommand())
			{
				updateCmd.Transaction = transaction;
				updateCmd.CommandText = @"
UPDATE Products
SET StockQuantity = StockQuantity - @Quantity,
    UpdatedAt = @Now
WHERE Id = @Id AND IsActive = 1 AND StockQuantity >= @Quantity AND SalePriceKurus = @Price;";
				updateCmd.Parameters.AddWithValue("@Quantity", line.Quantity);
				updateCmd.Parameters.AddWithValue("@Now", now);
				updateCmd.Parameters.AddWithValue("@Id", line.ProductId);
				updateCmd.Parameters.AddWithValue("@Price", line.UnitPriceKurus);
				if (updateCmd.ExecuteNonQuery() != 1)
				{
					throw new InvalidOperationException(name + " için stok güncellenemedi.");
				}
			}

			using (SqliteCommand insertItemCmd = connection.CreateCommand())
			{
				insertItemCmd.Transaction = transaction;
				insertItemCmd.CommandText = @"
INSERT INTO SaleItems (SaleId, ProductId, Barcode, ProductName, Quantity, PurchasePriceKurus, UnitPriceKurus, LineTotalKurus, StockAfter)
VALUES (@SaleId, @ProductId, @Barcode, @Name, @Quantity, @PurchasePrice, @Price, @Total, @StockAfter);";
				insertItemCmd.Parameters.AddWithValue("@SaleId", saleId);
				insertItemCmd.Parameters.AddWithValue("@ProductId", line.ProductId);
				insertItemCmd.Parameters.AddWithValue("@Barcode", barcode);
				insertItemCmd.Parameters.AddWithValue("@Name", name);
				insertItemCmd.Parameters.AddWithValue("@Quantity", line.Quantity);
				insertItemCmd.Parameters.AddWithValue("@PurchasePrice", purchasePrice);
				insertItemCmd.Parameters.AddWithValue("@Price", line.UnitPriceKurus);
				insertItemCmd.Parameters.AddWithValue("@Total", checked(line.UnitPriceKurus * line.Quantity));
				insertItemCmd.Parameters.AddWithValue("@StockAfter", stockAfter);
				insertItemCmd.ExecuteNonQuery();
			}
		}

		transaction.Commit();
		return new CheckoutResult(saleId, totalKurus);
	}
}
