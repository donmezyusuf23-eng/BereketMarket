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

public class PriceEditWindow : Window, IComponentConnector
{
	private readonly long productId;

	private readonly long oldPriceKurus;

	private bool isSaving;

	internal TextBlock ProductNameTextBlock;

	internal TextBlock BarcodeTextBlock;

	internal TextBlock CurrentPriceTextBlock;

	internal TextBox NewPriceTextBox;

	internal TextBlock StatusTextBlock;

	internal Button SavePriceButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public decimal NewPrice { get; private set; }

	public PriceEditWindow(ProductListItem product)
	{
		InitializeComponent();
		productId = product.Id;
		oldPriceKurus = decimal.ToInt64(product.Price * 100m);
		CultureInfo cultureInfo = CultureInfo.GetCultureInfo("tr-TR");
		ProductNameTextBlock.Text = product.Name;
		BarcodeTextBlock.Text = "Barkod: " + product.Barcode;
		CurrentPriceTextBlock.Text = "Mevcut fiyat: " + product.Price.ToString("N2", cultureInfo) + " TL";
		NewPriceTextBox.Text = product.Price.ToString("F2", cultureInfo);
		Loaded += (object _, RoutedEventArgs _) =>
		{
			NewPriceTextBox.Focus();
			NewPriceTextBox.SelectAll();
		};
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (isSaving)
			{
				e.Cancel = true;
			}
		};
	}

	private async void SavePriceButton_Click(object sender, RoutedEventArgs e)
	{
		if (isSaving)
		{
			return;
		}
		if (!decimal.TryParse(NewPriceTextBox.Text.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price) || price <= 0m || price > 1000000m)
		{
			ShowError("Fiyat 0'dan büyük, en fazla 1.000.000 TL olmalı.");
			return;
		}
		if (decimal.Round(price, 2) != price)
		{
			ShowError("Kuruş kısmı en fazla iki basamak olabilir.");
			return;
		}
		long newPriceKurus = decimal.ToInt64(price * 100m);
		if (newPriceKurus == oldPriceKurus)
		{
			ShowError("Yeni fiyat mevcut fiyatla aynı.");
			return;
		}
		isSaving = true;
		SavePriceButton.IsEnabled = false;
		CancelButton.IsEnabled = false;
		NewPriceTextBox.IsEnabled = false;
		SavePriceButton.Content = "Kaydediliyor...";
		StatusTextBlock.Text = "";
		bool succeeded = false;
		try
		{
			await Task.Run(() =>
			{
				PriceRepository.UpdatePrice(productId, oldPriceKurus, newPriceKurus);
			});
			NewPrice = price;
			succeeded = true;
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = "Fiyat güncellenemedi. Girdiğin değer korunuyor.";
			MessageBox.Show(ex.Message, "Fiyat güncelleme başarısız", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		finally
		{
			isSaving = false;
			SavePriceButton.IsEnabled = true;
			CancelButton.IsEnabled = true;
			NewPriceTextBox.IsEnabled = true;
			SavePriceButton.Content = "Fiyatı güncelle";
		}
		if (succeeded)
		{
			DialogResult = true;
		}
		else
		{
			NewPriceTextBox.Focus();
		}
	}

	private void ShowError(string message)
	{
		StatusTextBlock.Text = message;
		NewPriceTextBox.Focus();
		NewPriceTextBox.SelectAll();
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
			Uri resourceLocator = new Uri("/BereketMarket;component/priceeditwindow.xaml", UriKind.Relative);
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
			CurrentPriceTextBlock = (TextBlock)target;
			break;
		case 4:
			NewPriceTextBox = (TextBox)target;
			break;
		case 5:
			StatusTextBlock = (TextBlock)target;
			break;
		case 6:
			SavePriceButton = (Button)target;
			SavePriceButton.Click += SavePriceButton_Click;
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
