using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BereketMarket;

public class StockAdjustmentWindow : Window, IComponentConnector
{
	private readonly long productId;

	private readonly int originalStock;

	private bool isSaving;

	internal TextBlock ProductNameTextBlock;

	internal TextBlock BarcodeTextBlock;

	internal TextBlock CurrentStockTextBlock;

	internal TextBox ActualStockTextBox;

	internal TextBlock DifferenceTextBlock;

	internal TextBox ReasonTextBox;

	internal TextBlock StatusTextBlock;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public StockAdjustmentWindow(ProductListItem product)
	{
		productId = product.Id;
		originalStock = product.Stock;
		InitializeComponent();
		ProductNameTextBlock.Text = product.Name;
		BarcodeTextBlock.Text = "Barkod: " + product.Barcode;
		CurrentStockTextBlock.Text = $"Sistemdeki stok: {originalStock} adet";
		ActualStockTextBox.Text = originalStock.ToString(CultureInfo.InvariantCulture);
		Loaded += (object _, RoutedEventArgs _) =>
		{
			ActualStockTextBox.Focus();
			ActualStockTextBox.SelectAll();
		};
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (isSaving)
			{
				e.Cancel = true;
			}
		};
	}

	private bool TryReadStock(out int stock)
	{
		return int.TryParse(ActualStockTextBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out stock);
	}

	private void ActualStockTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (DifferenceTextBlock == null)
		{
			return;
		}
		if (!TryReadStock(out var stock))
		{
			DifferenceTextBlock.Text = "0 veya daha büyük bir tam sayı yaz.";
			return;
		}
		long num = (long)stock - (long)originalStock;
		if (num == 0L)
		{
			DifferenceTextBlock.Text = "Stok miktarı değişmeyecek.";
			return;
		}
		string value = num.ToString("+0;-0;0", CultureInfo.GetCultureInfo("tr-TR"));
		DifferenceTextBlock.Text = $"Düzeltme: {value} adet • Yeni stok: {stock}";
	}

	private async void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (isSaving)
		{
			return;
		}
		if (!TryReadStock(out var actualStock))
		{
			StatusTextBlock.Text = "Gerçek miktar için 0 veya daha büyük bir tam sayı yaz.";
			ActualStockTextBox.Focus();
			return;
		}
		if (actualStock == originalStock)
		{
			StatusTextBlock.Text = "Miktar mevcut stokla aynı; düzeltme gerekmiyor.";
			return;
		}
		string reason = ReasonTextBox.Text.Trim();
		if (reason.Length < 3)
		{
			StatusTextBlock.Text = "En az 3 karakterlik bir gerekçe yaz.";
			ReasonTextBox.Focus();
			return;
		}
		isSaving = true;
		SaveButton.IsEnabled = false;
		CancelButton.IsEnabled = false;
		ActualStockTextBox.IsEnabled = false;
		ReasonTextBox.IsEnabled = false;
		SaveButton.Content = "Kaydediliyor...";
		StatusTextBlock.Text = "";
		bool succeeded = false;
		try
		{
			await Task.Run(() =>
			{
				StockAdjustmentRepository.Adjust(productId, originalStock, actualStock, reason);
			});
			succeeded = true;
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = "Düzeltme kaydedilemedi: " + ex.Message;
		}
		finally
		{
			isSaving = false;
			SaveButton.IsEnabled = true;
			CancelButton.IsEnabled = true;
			ActualStockTextBox.IsEnabled = true;
			ReasonTextBox.IsEnabled = true;
			SaveButton.Content = "Düzeltmeyi kaydet";
		}
		if (succeeded)
		{
			DialogResult = true;
		}
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		if (!isSaving)
		{
			DialogResult = false;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.2.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/BereketMarket;component/stockadjustmentwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.2.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			ProductNameTextBlock = (TextBlock)target;
			break;
		case 2:
			BarcodeTextBlock = (TextBlock)target;
			break;
		case 3:
			CurrentStockTextBlock = (TextBlock)target;
			break;
		case 4:
			ActualStockTextBox = (TextBox)target;
			ActualStockTextBox.TextChanged += ActualStockTextBox_TextChanged;
			break;
		case 5:
			DifferenceTextBlock = (TextBlock)target;
			break;
		case 6:
			ReasonTextBox = (TextBox)target;
			break;
		case 7:
			StatusTextBlock = (TextBlock)target;
			break;
		case 8:
			SaveButton = (Button)target;
			SaveButton.Click += SaveButton_Click;
			break;
		case 9:
			CancelButton = (Button)target;
			CancelButton.Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
