using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class Database
{
	public static string DataFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BereketMarket");

	public static string DatabasePath { get; } = Path.Combine(DataFolder, "bereketmarket.db");

	public static SqliteConnection GetConnection()
	{
		SqliteConnection connection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = DatabasePath,
			Mode = SqliteOpenMode.ReadWriteCreate,
			ForeignKeys = true
		}.ToString());
		connection.Open();
		return connection;
	}

	public static void Initialize()
	{
		Directory.CreateDirectory(DataFolder);
		using SqliteConnection connection = GetConnection();
		using SqliteCommand command = connection.CreateCommand();

		command.CommandText = @"
CREATE TABLE IF NOT EXISTS Products
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Barcode TEXT NOT NULL UNIQUE CHECK (length(trim(Barcode)) > 0),
    Name TEXT NOT NULL CHECK (length(trim(Name)) > 0),
    PurchasePriceKurus INTEGER NOT NULL DEFAULT 0 CHECK (PurchasePriceKurus >= 0),
    SalePriceKurus INTEGER NOT NULL CHECK (SalePriceKurus >= 0),
    StockQuantity INTEGER NOT NULL CHECK (StockQuantity >= 0),
    CriticalStockQuantity INTEGER NOT NULL DEFAULT 5 CHECK (CriticalStockQuantity >= 0),
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1))
);

CREATE TABLE IF NOT EXISTS Sales
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SoldAt TEXT NOT NULL,
    TotalKurus INTEGER NOT NULL CHECK (TotalKurus > 0),
    PaymentType TEXT NOT NULL DEFAULT 'Cash',
    CustomerId INTEGER,
    Status TEXT NOT NULL DEFAULT 'Completed'
);

CREATE TABLE IF NOT EXISTS SaleItems
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Barcode TEXT NOT NULL,
    ProductName TEXT NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    PurchasePriceKurus INTEGER NOT NULL DEFAULT 0,
    UnitPriceKurus INTEGER NOT NULL CHECK (UnitPriceKurus > 0),
    LineTotalKurus INTEGER NOT NULL CHECK (LineTotalKurus > 0),
    StockAfter INTEGER NOT NULL CHECK (StockAfter >= 0),
    FOREIGN KEY (SaleId) REFERENCES Sales(Id),
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS Customers
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL CHECK (length(trim(FullName)) > 0),
    Phone TEXT,
    Note TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS CustomerTransactions
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL,
    TransactionType TEXT NOT NULL CHECK (TransactionType IN ('Debt', 'Payment')),
    AmountKurus INTEGER NOT NULL CHECK (AmountKurus > 0),
    Description TEXT,
    SaleId INTEGER,
    CreatedAt TEXT NOT NULL,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE TABLE IF NOT EXISTS QuickProducts
(
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL CHECK (length(trim(Name)) > 0),
    PriceKurus INTEGER NOT NULL CHECK (PriceKurus >= 0),
    Barcode TEXT,
    ColorHex TEXT NOT NULL DEFAULT '#008C95',
    DisplayOrder INTEGER NOT NULL DEFAULT 0
);
";
		command.ExecuteNonQuery();

		MigrateColumns(connection);
		SeedDefaultQuickProducts(connection);
	}

	private static void MigrateColumns(SqliteConnection connection)
	{
		AddColumnIfNotExists(connection, "Products", "PurchasePriceKurus", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfNotExists(connection, "Products", "CriticalStockQuantity", "INTEGER NOT NULL DEFAULT 5");
		AddColumnIfNotExists(connection, "Sales", "PaymentType", "TEXT NOT NULL DEFAULT 'Cash'");
		AddColumnIfNotExists(connection, "Sales", "CustomerId", "INTEGER");
		AddColumnIfNotExists(connection, "SaleItems", "PurchasePriceKurus", "INTEGER NOT NULL DEFAULT 0");
	}

	private static void AddColumnIfNotExists(SqliteConnection connection, string table, string column, string definition)
	{
		using SqliteCommand checkCommand = connection.CreateCommand();
		checkCommand.CommandText = $"PRAGMA table_info({table});";
		using SqliteDataReader reader = checkCommand.ExecuteReader();
		while (reader.Read())
		{
			string name = reader.GetString(1);
			if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
		}

		using SqliteCommand alterCommand = connection.CreateCommand();
		alterCommand.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
		alterCommand.ExecuteNonQuery();
	}

	private static void SeedDefaultQuickProducts(SqliteConnection connection)
	{
		using SqliteCommand checkCommand = connection.CreateCommand();
		checkCommand.CommandText = "SELECT COUNT(*) FROM QuickProducts;";
		long count = (long)checkCommand.ExecuteScalar()!;
		if (count > 0)
		{
			return;
		}

		string now = DateTime.UtcNow.ToString("O");
		using SqliteCommand insertCommand = connection.CreateCommand();
		insertCommand.CommandText = @"
INSERT INTO QuickProducts (Name, PriceKurus, Barcode, ColorHex, DisplayOrder) VALUES
('Ekmek', 1250, '8690001', '#E67E22', 1),
('Poşet', 25, '8690002', '#7F8C8D', 2),
('Su (0.5L)', 750, '8690003', '#2980B9', 3),
('Su (1.5L)', 1500, '8690004', '#3498DB', 4),
('Yumurta (Adet)', 450, '8690005', '#F39C12', 5),
('Çay Bardağı', 1000, '8690006', '#C0392B', 6),
('Köy Sütü (Litre)', 3000, '8690007', '#16A085', 7),
('Açık Şeker (Kg)', 3500, '8690008', '#8E44AD', 8);
";
		insertCommand.ExecuteNonQuery();

		EnsureQuickProductInProducts(connection, "8690001", "Ekmek", 1250, 1000, now);
		EnsureQuickProductInProducts(connection, "8690002", "Poşet", 25, 10, now);
		EnsureQuickProductInProducts(connection, "8690003", "Su (0.5L)", 750, 500, now);
		EnsureQuickProductInProducts(connection, "8690004", "Su (1.5L)", 1500, 1000, now);
		EnsureQuickProductInProducts(connection, "8690005", "Yumurta (Adet)", 450, 350, now);
		EnsureQuickProductInProducts(connection, "8690006", "Çay Bardağı", 1000, 700, now);
		EnsureQuickProductInProducts(connection, "8690007", "Köy Sütü (Litre)", 3000, 2400, now);
		EnsureQuickProductInProducts(connection, "8690008", "Açık Şeker (Kg)", 3500, 2800, now);
	}

	private static void EnsureQuickProductInProducts(SqliteConnection connection, string barcode, string name, int salePriceKurus, int purchasePriceKurus, string now)
	{
		using SqliteCommand check = connection.CreateCommand();
		check.CommandText = "SELECT COUNT(*) FROM Products WHERE Barcode = @Barcode;";
		check.Parameters.AddWithValue("@Barcode", barcode);
		long count = (long)check.ExecuteScalar()!;
		if (count == 0)
		{
			using SqliteCommand insert = connection.CreateCommand();
			insert.CommandText = @"
INSERT INTO Products (Barcode, Name, PurchasePriceKurus, SalePriceKurus, StockQuantity, CriticalStockQuantity, CreatedAt, UpdatedAt, IsActive)
VALUES (@Barcode, @Name, @PurchasePrice, @SalePrice, 50, 5, @Now, @Now, 1);";
			insert.Parameters.AddWithValue("@Barcode", barcode);
			insert.Parameters.AddWithValue("@Name", name);
			insert.Parameters.AddWithValue("@PurchasePrice", purchasePriceKurus);
			insert.Parameters.AddWithValue("@SalePrice", salePriceKurus);
			insert.Parameters.AddWithValue("@Now", now);
			insert.ExecuteNonQuery();
		}
	}
}
