using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public static class ExcelExportService
{
	public static Dictionary<string, int> Export(string destinationPath)
	{
		string fullPath = Path.GetFullPath(destinationPath);
		if (!string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Dosya uzantısı .xlsx olmalı.");
		}
		if (File.Exists(fullPath))
		{
			throw new InvalidOperationException("Bu isimde dosya zaten var. Farklı bir dosya adı seç.");
		}
		string directoryName = Path.GetDirectoryName(fullPath);
		if (string.IsNullOrWhiteSpace(directoryName) || !Directory.Exists(directoryName))
		{
			throw new DirectoryNotFoundException("Kaydedilecek klasör bulunamadı.");
		}
		SqliteConnectionStringBuilder sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder
		{
			DataSource = Database.DatabasePath,
			Mode = SqliteOpenMode.ReadOnly,
			Pooling = false
		};
		using XLWorkbook xLWorkbook = new XLWorkbook();
		xLWorkbook.Style.Font.FontName = "Calibri";
		xLWorkbook.Style.Font.FontSize = 11.0;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		DateTime now = DateTime.Now;
		using (SqliteConnection sqliteConnection = new SqliteConnection(sqliteConnectionStringBuilder.ToString()))
		{
			sqliteConnection.Open();
			using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction(deferred: true);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
			{
				sqliteCommand.Transaction = sqliteTransaction;
				sqliteCommand.CommandText = "SELECT name\r\nFROM sqlite_master\r\nWHERE type = 'table';";
				using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
				while (sqliteDataReader.Read())
				{
					hashSet.Add(sqliteDataReader.GetString(0));
				}
			}
			if (!hashSet.Contains("Products"))
			{
				throw new InvalidOperationException("Ürün tablosu bulunamadı.");
			}
			dictionary["Ürünler"] = WriteSheet(xLWorkbook, sqliteConnection, sqliteTransaction, "Ürünler", "Aktif ve pasif ürünler birlikte gösterilir.", new string[8] { "Ürün ID", "Barkod", "Ürün adı", "Güncel fiyat (TL)", "Güncel stok", "Durum", "Kayıt tarihi", "Son güncelleme tarihi" }, "SELECT\r\n    Id,\r\n    Barcode,\r\n    Name,\r\n    SalePriceKurus / 100.0,\r\n    StockQuantity,\r\n    CASE WHEN IsActive = 1\r\n        THEN 'Aktif' ELSE 'Pasif' END,\r\n    CreatedAt,\r\n    UpdatedAt\r\nFROM Products\r\nORDER BY Id;", now);
			dictionary["Satışlar"] = WriteSheet(xLWorkbook, sqliteConnection, sqliteTransaction, "Satışlar", "Net satış hesabında yalnızca Tamamlandı durumunu kullanın.", new string[4] { "Satış ID", "Satış tarihi", "Toplam tutar (TL)", "Durum" }, hashSet.Contains("Sales") ? "SELECT\r\n    Id,\r\n    SoldAt,\r\n    TotalKurus / 100.0,\r\n    CASE Status\r\n        WHEN 'Completed' THEN 'Tamamlandı'\r\n        WHEN 'Cancelled' THEN 'İptal edildi'\r\n        ELSE Status\r\n    END\r\nFROM Sales\r\nORDER BY Id;" : null, now);
			dictionary["Satış Detayları"] = WriteSheet(xLWorkbook, sqliteConnection, sqliteTransaction, "Satış Detayları", "Ad, barkod ve fiyat satış anındaki değerlerdir. Net hesaplarda iptal edilen satışları hariç tutun.", new string[8] { "Satış ID", "Ürün ID", "Barkod", "Satıştaki ürün adı", "Adet", "Birim fiyat (TL)", "Satır tutarı (TL)", "Durum" }, (hashSet.Contains("SaleItems") && hashSet.Contains("Sales")) ? "SELECT\r\n    i.SaleId,\r\n    i.ProductId,\r\n    i.Barcode,\r\n    i.ProductName,\r\n    i.Quantity,\r\n    i.UnitPriceKurus / 100.0,\r\n    i.LineTotalKurus / 100.0,\r\n    CASE s.Status\r\n        WHEN 'Completed' THEN 'Tamamlandı'\r\n        WHEN 'Cancelled' THEN 'İptal edildi'\r\n        ELSE s.Status\r\n    END\r\nFROM SaleItems i\r\nINNER JOIN Sales s ON s.Id = i.SaleId\r\nORDER BY i.SaleId, i.Id;" : null, now);
			List<string> list = new List<string>();
			if (hashSet.Contains("InitialStockEntries"))
			{
				list.Add("SELECT\r\n    ProductId,\r\n    CreatedAt,\r\n    'İlk stok',\r\n    Quantity,\r\n    Quantity,\r\n    NULL,\r\n    'Başlangıç stoğu'\r\nFROM InitialStockEntries");
			}
			if (hashSet.Contains("StockEntries"))
			{
				list.Add("SELECT\r\n    ProductId,\r\n    CreatedAt,\r\n    'Stok girişi',\r\n    Quantity,\r\n    StockAfter,\r\n    NULL,\r\n    'Yeni mal girişi'\r\nFROM StockEntries");
			}
			if (hashSet.Contains("SaleItems") && hashSet.Contains("Sales"))
			{
				list.Add("SELECT\r\n    i.ProductId,\r\n    s.SoldAt,\r\n    'Satış',\r\n    -i.Quantity,\r\n    i.StockAfter,\r\n    s.Id,\r\n    CASE WHEN s.Status = 'Cancelled'\r\n        THEN 'Bu satış sonradan iptal edildi.'\r\n        ELSE 'Satış çıkışı'\r\n    END\r\nFROM SaleItems i\r\nINNER JOIN Sales s ON s.Id = i.SaleId");
			}
			if (hashSet.Contains("SaleCancellations") && hashSet.Contains("SaleCancellationItems"))
			{
				list.Add("SELECT\r\n    i.ProductId,\r\n    c.CancelledAt,\r\n    'Satış iptali',\r\n    i.Quantity,\r\n    i.StockAfter,\r\n    c.SaleId,\r\n    c.Reason\r\nFROM SaleCancellationItems i\r\nINNER JOIN SaleCancellations c\r\n    ON c.Id = i.CancellationId");
			}
			if (hashSet.Contains("StockAdjustments"))
			{
				list.Add("SELECT\r\n    ProductId,\r\n    CreatedAt,\r\n    'Stok düzeltme',\r\n    QuantityChange,\r\n    StockAfter,\r\n    NULL,\r\n    Reason\r\nFROM StockAdjustments");
			}
			dictionary["Stok Hareketleri"] = WriteSheet(xLWorkbook, sqliteConnection, sqliteTransaction, "Stok Hareketleri", "Eski ürünlerde ilk stok kaydı eksik olabilir. Güncel stok için Ürünler sayfasını kullanın.", new string[7] { "Ürün ID", "İşlem tarihi", "İşlem türü", "Miktar değişimi", "İşlem sonrası stok", "İlgili satış ID", "Açıklama" }, (list.Count > 0) ? string.Join("\nUNION ALL\n", list) : null, now);
			dictionary["Fiyat Geçmişi"] = WriteSheet(xLWorkbook, sqliteConnection, sqliteTransaction, "Fiyat Geçmişi", "Fiyat değişiklikleri; ürünlerle Ürün ID üzerinden eşleşir.", new string[4] { "Ürün ID", "Değişiklik tarihi", "Eski fiyat (TL)", "Yeni fiyat (TL)" }, hashSet.Contains("PriceHistory") ? "SELECT\r\n    ProductId,\r\n    ChangedAt,\r\n    OldPriceKurus / 100.0,\r\n    NewPriceKurus / 100.0\r\nFROM PriceHistory\r\nORDER BY Id;" : null, now);
			dictionary["Satış İptalleri"] = WriteSheet(xLWorkbook, sqliteConnection, sqliteTransaction, "Satış İptalleri", "İptal edilen satışın ayrıntıları Satış ID ile bulunabilir.", new string[3] { "Satış ID", "İptal tarihi", "İptal gerekçesi" }, hashSet.Contains("SaleCancellations") ? "SELECT SaleId, CancelledAt, Reason\r\nFROM SaleCancellations\r\nORDER BY Id;" : null, now);
			sqliteTransaction.Commit();
		}
		string text = Path.Combine(directoryName, ".BereketExcel_" + Guid.NewGuid().ToString("N") + ".xlsx");
		bool flag = false;
		try
		{
			using (FileStream fileStream = new FileStream(text, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
			{
				flag = true;
				xLWorkbook.SaveAs(fileStream);
				fileStream.Flush(flushToDisk: true);
			}
			File.Move(text, fullPath);
		}
		finally
		{
			if (flag)
			{
				try
				{
					if (File.Exists(text))
					{
						File.Delete(text);
					}
				}
				catch
				{
				}
			}
		}
		return dictionary;
	}

	private static int WriteSheet(XLWorkbook workbook, SqliteConnection connection, SqliteTransaction transaction, string sheetName, string description, string[] headers, string? sql, DateTime exportedAt)
	{
		IXLWorksheet iXLWorksheet = workbook.Worksheets.Add(sheetName);
		iXLWorksheet.Range(1, 1, 1, headers.Length).Merge();
		iXLWorksheet.Cell(1, 1).Value = "BEREKET MARKET • " + sheetName;
		iXLWorksheet.Cell(1, 1).Style.Font.Bold = true;
		iXLWorksheet.Cell(1, 1).Style.Font.FontSize = 18.0;
		iXLWorksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#172B4D");
		iXLWorksheet.Row(1).Height = 30.0;
		iXLWorksheet.Range(2, 1, 2, headers.Length).Merge();
		iXLWorksheet.Cell(2, 1).Value = description;
		iXLWorksheet.Cell(2, 1).Style.Alignment.WrapText = true;
		iXLWorksheet.Row(2).Height = 38.0;
		iXLWorksheet.Range(3, 1, 3, headers.Length).Merge();
		iXLWorksheet.Cell(3, 1).Value = "Aktarım zamanı: " + exportedAt.ToString("dd.MM.yyyy HH:mm:ss");
		for (int i = 0; i < headers.Length; i++)
		{
			string text = headers[i];
			iXLWorksheet.Cell(5, i + 1).Value = text;
			iXLWorksheet.Column(i + 1).Width = 22.0;
			if (text.EndsWith("ID", StringComparison.Ordinal))
			{
				iXLWorksheet.Column(i + 1).Width = 15.0;
			}
			if (text.Contains("ürün adı", StringComparison.OrdinalIgnoreCase))
			{
				iXLWorksheet.Column(i + 1).Width = 32.0;
			}
			if (text.Contains("Açıklama") || text.Contains("gerekçesi"))
			{
				iXLWorksheet.Column(i + 1).Width = 55.0;
				iXLWorksheet.Column(i + 1).Style.Alignment.WrapText = true;
			}
		}
		int num = 6;
		if (sql != null)
		{
			using SqliteCommand sqliteCommand = connection.CreateCommand();
			sqliteCommand.Transaction = transaction;
			sqliteCommand.CommandText = sql;
			using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
			while (sqliteDataReader.Read())
			{
				if (num > 1048576)
				{
					throw new InvalidOperationException(sheetName + " sayfası Excel satır sınırını aşıyor.");
				}
				for (int j = 0; j < headers.Length; j++)
				{
					if (sqliteDataReader.IsDBNull(j))
					{
						continue;
					}
					IXLCell iXLCell = iXLWorksheet.Cell(num, j + 1);
					string text2 = headers[j];
					object value = sqliteDataReader.GetValue(j);
					if (text2.EndsWith("ID", StringComparison.Ordinal) || text2.Contains("Barkod"))
					{
						iXLCell.Style.NumberFormat.Format = "@";
						iXLCell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
					}
					else if (text2.Contains("tarihi", StringComparison.OrdinalIgnoreCase))
					{
						string text3 = Convert.ToString(value) ?? "";
						if (DateTimeOffset.TryParse(text3, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
						{
							iXLCell.Value = result.LocalDateTime;
							iXLCell.Style.NumberFormat.Format = "dd.mm.yyyy hh:mm:ss";
						}
						else
						{
							iXLCell.Value = text3;
						}
					}
					else if (value is string text4)
					{
						iXLCell.Value = text4;
					}
					else
					{
						iXLCell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
						iXLCell.Style.NumberFormat.Format = (text2.EndsWith("(TL)") ? "#,##0.00" : "0");
					}
				}
				num++;
			}
		}
		int num2 = num - 5 - 1;
		if (num2 > 0)
		{
			IXLTable iXLTable = iXLWorksheet.Range(5, 1, num - 1, headers.Length).CreateTable("Table" + workbook.Worksheets.Count);
			iXLTable.Theme = XLTableTheme.TableStyleMedium2;
			iXLTable.ShowAutoFilter = true;
		}
		else
		{
			iXLWorksheet.Cell(2, 1).Value = description + " Bu bölümde kayıt bulunmuyor.";
		}
		IXLRange iXLRange = iXLWorksheet.Range(5, 1, 5, headers.Length);
		iXLRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#172B4D");
		iXLRange.Style.Font.FontColor = XLColor.White;
		iXLRange.Style.Font.Bold = true;
		iXLRange.Style.Alignment.WrapText = true;
		iXLWorksheet.Row(5).Height = 32.0;
		iXLWorksheet.SheetView.FreezeRows(5);
		return num2;
	}
}
