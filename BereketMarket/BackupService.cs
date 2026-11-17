using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class BackupService
{
	private static SqliteConnection OpenConnection(string path, SqliteOpenMode mode)
	{
		SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = mode,
			Pooling = false
		}.ToString());
		try
		{
			sqliteConnection.Open();
			return sqliteConnection;
		}
		catch
		{
			sqliteConnection.Dispose();
			throw;
		}
	}

	public static BackupInfo CreateBackup(string destinationPath)
	{
		string fullPath = Path.GetFullPath(destinationPath);
		string fullPath2 = Path.GetFullPath(Database.DatabasePath);
		if (string.Equals(fullPath, fullPath2, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Yedek, çalışan veritabanının üzerine kaydedilemez.");
		}
		if (!File.Exists(fullPath2))
		{
			throw new FileNotFoundException("Uygulamanın veritabanı bulunamadı.");
		}
		if (File.Exists(fullPath))
		{
			throw new InvalidOperationException("Bu isimde bir dosya zaten var. Eski yedeği korumak için farklı bir dosya adı seç.");
		}
		string directoryName = Path.GetDirectoryName(fullPath);
		if (string.IsNullOrWhiteSpace(directoryName) || !Directory.Exists(directoryName))
		{
			throw new DirectoryNotFoundException("Kaydedilecek klasör bulunamadı.");
		}
		string text = Path.Combine(directoryName, ".BereketMarket_" + Guid.NewGuid().ToString("N") + ".tmp");
		bool flag = false;
		try
		{
			using (new FileStream(text, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
			}
			flag = true;
			using (SqliteConnection sqliteConnection = OpenConnection(fullPath2, SqliteOpenMode.ReadOnly))
			{
				using SqliteConnection destination = OpenConnection(text, SqliteOpenMode.ReadWrite);
				sqliteConnection.BackupDatabase(destination);
			}
			BackupInfo result = InspectBackup(text);
			File.Move(text, fullPath);
			return result;
		}
		finally
		{
			if (flag)
			{
				TryDelete(text);
				TryDelete(text + "-journal");
				TryDelete(text + "-wal");
				TryDelete(text + "-shm");
			}
		}
	}

	public static BackupInfo InspectBackup(string path)
	{
		string fullPath = Path.GetFullPath(path);
		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException("Seçilen yedek dosyası bulunamadı.");
		}
		using SqliteConnection sqliteConnection = OpenConnection(fullPath, SqliteOpenMode.ReadOnly);
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.CommandText = "PRAGMA integrity_check;";
			using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
			if (!sqliteDataReader.Read() || !string.Equals(sqliteDataReader.GetString(0), "ok", StringComparison.OrdinalIgnoreCase) || sqliteDataReader.Read())
			{
				throw new InvalidOperationException("Veritabanı bütünlük kontrolünden geçemedi.");
			}
		}
		using (SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand())
		{
			sqliteCommand2.CommandText = "PRAGMA foreign_key_check;";
			using SqliteDataReader sqliteDataReader2 = sqliteCommand2.ExecuteReader();
			if (sqliteDataReader2.Read())
			{
				throw new InvalidOperationException("Veritabanında birbiriyle uyuşmayan kayıtlar var.");
			}
		}
		using (SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand())
		{
			sqliteCommand3.CommandText = "SELECT\n    Id,\n    Barcode,\n    Name,\n    SalePriceKurus,\n    StockQuantity,\n    CreatedAt,\n    UpdatedAt,\n    IsActive\nFROM Products\nLIMIT 0;";
			using (sqliteCommand3.ExecuteReader())
			{
			}
		}
		long productCount;
		using (SqliteCommand sqliteCommand4 = sqliteConnection.CreateCommand())
		{
			sqliteCommand4.CommandText = "SELECT COUNT(*) FROM Products;";
			productCount = Convert.ToInt64(sqliteCommand4.ExecuteScalar());
		}
		long saleCount = 0L;
		bool flag;
		using (SqliteCommand sqliteCommand5 = sqliteConnection.CreateCommand())
		{
			sqliteCommand5.CommandText = "SELECT COUNT(*)\nFROM sqlite_master\nWHERE type = 'table' AND name = 'Sales';";
			flag = Convert.ToInt64(sqliteCommand5.ExecuteScalar()) == 1;
		}
		if (flag)
		{
			using SqliteCommand sqliteCommand6 = sqliteConnection.CreateCommand();
			sqliteCommand6.CommandText = "SELECT COUNT(*) FROM Sales;";
			saleCount = Convert.ToInt64(sqliteCommand6.ExecuteScalar());
		}
		return new BackupInfo(productCount, saleCount);
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}
}
