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

public class StockEntryWindow : Window, IComponentConnector
{
	private readonly long productId;

	private bool isSaving;

	internal TextBlock ProductNameTextBlock;

	internal TextBlock BarcodeTextBlock;

	internal TextBlock CurrentStockTextBlock;

	internal TextBox QuantityTextBox;

	internal TextBlock StatusTextBlock;

	internal Button SaveStockButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public int AddedQuantity { get; private set; }

	public int NewStock { get; private set; }

	public StockEntryWindow(ProductListItem product)
	{
		InitializeComponent();
		productId = product.Id;
		ProductNameTextBlock.Text = product.Name;
		BarcodeTextBlock.Text = "Barkod: " + product.Barcode;
		CurrentStockTextBlock.Text = $"Listedeki mevcut stok: {product.Stock} adet";
		Loaded += (object _, RoutedEventArgs _) =>
		{
			QuantityTextBox.Focus();
		};
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (isSaving)
			{
				e.Cancel = true;
			}
		};
	}

	private async void SaveStockButton_Click(object sender, RoutedEventArgs e)
	{
		if (isSaving)
		{
			return;
		}
		if (!int.TryParse(QuantityTextBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
		{
			StatusTextBlock.Text = "Sıfırdan büyük bir tam sayı yaz.";
			QuantityTextBox.Focus();
			QuantityTextBox.SelectAll();
			return;
		}
		isSaving = true;
		SaveStockButton.IsEnabled = false;
		CancelButton.IsEnabled = false;
		QuantityTextBox.IsEnabled = false;
		SaveStockButton.Content = "Kaydediliyor...";
		StatusTextBlock.Text = "";
		bool succeeded = false;
		try
		{
			NewStock = await Task.Run(() => StockRepository.AddStock(productId, quantity));
			AddedQuantity = quantity;
			succeeded = true;
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = "Stok eklenemedi. Girdiğin miktar korunuyor.";
			MessageBox.Show(ex.Message, "Stok işlemi başarısız", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		finally
		{
			isSaving = false;
			SaveStockButton.IsEnabled = true;
			CancelButton.IsEnabled = true;
			QuantityTextBox.IsEnabled = true;
			SaveStockButton.Content = "Stoğa ekle";
		}
		if (succeeded)
		{
			DialogResult = true;
		}
		else
		{
			QuantityTextBox.Focus();
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
			Uri resourceLocator = new Uri("/BereketMarket;component/stockentrywindow.xaml", UriKind.Relative);
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
			QuantityTextBox = (TextBox)target;
			break;
		case 5:
			StatusTextBlock = (TextBlock)target;
			break;
		case 6:
			SaveStockButton = (Button)target;
			SaveStockButton.Click += SaveStockButton_Click;
			break;
		case 7:
			CancelButton = (Button)target;
			CancelButton.Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
