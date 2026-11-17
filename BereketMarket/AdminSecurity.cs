using System;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class AdminSecurity
{
	private const int PasswordIterations = 600000;

	private static volatile bool authenticated;

	public static bool IsAuthenticated => authenticated;

	public static void Logout()
	{
		authenticated = false;
	}

	public static void RequireAdmin()
	{
		if (!authenticated)
		{
			throw new InvalidOperationException("Bu işlem için yönetici girişi gerekli.");
		}
	}

	private static SqliteConnection OpenConnection()
	{
		SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = Database.DatabasePath,
			Mode = SqliteOpenMode.ReadWrite,
			ForeignKeys = true
		}.ToString());
		try
		{
			sqliteConnection.Open();
			using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
			sqliteCommand.CommandText = "CREATE TABLE IF NOT EXISTS AdminCredentials\n(\n    Id INTEGER PRIMARY KEY CHECK (Id = 1),\n    Salt BLOB NOT NULL,\n    PasswordHash BLOB NOT NULL,\n    Iterations INTEGER NOT NULL,\n    FailedAttempts INTEGER NOT NULL DEFAULT 0,\n    LockedUntil INTEGER NOT NULL DEFAULT 0\n);";
			sqliteCommand.ExecuteNonQuery();
			return sqliteConnection;
		}
		catch
		{
			sqliteConnection.Dispose();
			throw;
		}
	}

	public static bool HasPassword()
	{
		using SqliteConnection sqliteConnection = OpenConnection();
		using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
		sqliteCommand.CommandText = "SELECT COUNT(*) FROM AdminCredentials WHERE Id = 1;";
		return Convert.ToInt64(sqliteCommand.ExecuteScalar()) == 1;
	}

	private static void ValidateNewPassword(string password)
	{
		if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
		{
			throw new ArgumentException("Şifre 12–128 karakter arasında olmalı. Hatırlayabileceğin uzun bir parola kullan.");
		}
	}

	private static byte[] Derive(string password, byte[] salt, int iterations)
	{
		return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
	}

	public static void CreatePassword(string password)
	{
		ValidateNewPassword(password);
		byte[] bytes = RandomNumberGenerator.GetBytes(32);
		byte[] array = Derive(password, bytes, 600000);
		try
		{
			using SqliteConnection sqliteConnection = OpenConnection();
			using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction();
			using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
			{
				sqliteCommand.Transaction = sqliteTransaction;
				sqliteCommand.CommandText = "SELECT COUNT(*) FROM AdminCredentials WHERE Id = 1;";
				if (Convert.ToInt64(sqliteCommand.ExecuteScalar()) != 0L)
				{
					throw new InvalidOperationException("Yönetici şifresi zaten oluşturulmuş. Pencereyi kapatıp yeniden giriş yap.");
				}
			}
			using SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand();
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "INSERT INTO AdminCredentials\n    (Id, Salt, PasswordHash, Iterations)\nVALUES\n    (1, $salt, $hash, $iterations);";
			sqliteCommand2.Parameters.AddWithValue("$salt", bytes);
			sqliteCommand2.Parameters.AddWithValue("$hash", array);
			sqliteCommand2.Parameters.AddWithValue("$iterations", 600000);
			sqliteCommand2.ExecuteNonQuery();
			sqliteTransaction.Commit();
			authenticated = true;
		}
		finally
		{
			CryptographicOperations.ZeroMemory(array);
		}
	}

	public static void Login(string password)
	{
		authenticated = false;
		VerifyAndUpdate(password, null);
		authenticated = true;
	}

	public static void ChangePassword(string currentPassword, string newPassword)
	{
		RequireAdmin();
		ValidateNewPassword(newPassword);
		if (currentPassword == newPassword)
		{
			throw new ArgumentException("Yeni şifre mevcut şifreyle aynı olamaz.");
		}
		VerifyAndUpdate(currentPassword, newPassword);
	}

	private static void VerifyAndUpdate(string password, string? newPassword)
	{
		if (password.Length == 0 || password.Length > 128)
		{
			throw new ArgumentException("Geçerli bir şifre gir.");
		}
		using SqliteConnection sqliteConnection = OpenConnection();
		using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction();
		byte[] array;
		byte[] array2;
		int @int;
		int int2;
		long int3;
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.Transaction = sqliteTransaction;
			sqliteCommand.CommandText = "SELECT\n    Salt,\n    PasswordHash,\n    Iterations,\n    FailedAttempts,\n    LockedUntil\nFROM AdminCredentials\nWHERE Id = 1;";
			using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
			if (!sqliteDataReader.Read())
			{
				throw new InvalidOperationException("Yönetici şifresi henüz oluşturulmamış.");
			}
			array = (byte[])sqliteDataReader.GetValue(0);
			array2 = (byte[])sqliteDataReader.GetValue(1);
			@int = sqliteDataReader.GetInt32(2);
			int2 = sqliteDataReader.GetInt32(3);
			int3 = sqliteDataReader.GetInt64(4);
		}
		long num = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		if (int3 > num)
		{
			throw new InvalidOperationException($"Çok fazla hatalı deneme. {int3 - num} saniye sonra tekrar dene.");
		}
		if (array.Length != 32 || array2.Length != 32 || @int != 600000)
		{
			throw new InvalidOperationException("Şifre doğrulama kaydı geçersiz.");
		}
		byte[] array3 = Derive(password, array, @int);
		bool flag;
		try
		{
			flag = CryptographicOperations.FixedTimeEquals(array3, array2);
		}
		finally
		{
			CryptographicOperations.ZeroMemory(array3);
		}
		if (!flag)
		{
			int num2 = int2 + 1;
			bool flag2 = num2 >= 5;
			using SqliteCommand sqliteCommand2 = sqliteConnection.CreateCommand();
			sqliteCommand2.Transaction = sqliteTransaction;
			sqliteCommand2.CommandText = "UPDATE AdminCredentials\nSET\n    FailedAttempts = $failures,\n    LockedUntil = $lockedUntil\nWHERE Id = 1;";
			sqliteCommand2.Parameters.AddWithValue("$failures", (!flag2) ? num2 : 0);
			sqliteCommand2.Parameters.AddWithValue("$lockedUntil", flag2 ? (num + 30) : 0);
			sqliteCommand2.ExecuteNonQuery();
			sqliteTransaction.Commit();
			throw new InvalidOperationException(flag2 ? "5 hatalı deneme yapıldı. 30 saniye bekle." : "Şifre yanlış.");
		}
		using SqliteCommand sqliteCommand3 = sqliteConnection.CreateCommand();
		sqliteCommand3.Transaction = sqliteTransaction;
		byte[] array4 = null;
		try
		{
			if (newPassword == null)
			{
				sqliteCommand3.CommandText = "UPDATE AdminCredentials\nSET FailedAttempts = 0, LockedUntil = 0\nWHERE Id = 1;";
			}
			else
			{
				byte[] bytes = RandomNumberGenerator.GetBytes(32);
				array4 = Derive(newPassword, bytes, 600000);
				sqliteCommand3.CommandText = "UPDATE AdminCredentials\nSET\n    Salt = $salt,\n    PasswordHash = $hash,\n    Iterations = $iterations,\n    FailedAttempts = 0,\n    LockedUntil = 0\nWHERE Id = 1;";
				sqliteCommand3.Parameters.AddWithValue("$salt", bytes);
				sqliteCommand3.Parameters.AddWithValue("$hash", array4);
				sqliteCommand3.Parameters.AddWithValue("$iterations", 600000);
			}
			sqliteCommand3.ExecuteNonQuery();
			sqliteTransaction.Commit();
		}
		finally
		{
			if (array4 != null)
			{
				CryptographicOperations.ZeroMemory(array4);
			}
		}
	}
}
