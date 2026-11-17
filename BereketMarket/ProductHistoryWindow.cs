using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BereketMarket;

public class ProductHistoryWindow : Window, IComponentConnector
{
	private readonly long productId;

	private bool isLoading;

	internal TextBlock ProductNameTextBlock;

	internal TextBlock BarcodeTextBlock;

	internal TextBlock InfoTextBlock;

	internal DataGrid HistoryGrid;

	internal TextBlock StatusTextBlock;

	internal Button RefreshButton;

	private bool _contentLoaded;

	public ProductHistoryWindow(ProductListItem product)
	{
		InitializeComponent();
		productId = product.Id;
		ProductNameTextBlock.Text = product.Name;
		BarcodeTextBlock.Text = "Barkod: " + product.Barcode;
		Loaded += async (object _, RoutedEventArgs _) =>
		{
			await LoadHistoryAsync();
		};
	}

	private async Task LoadHistoryAsync()
	{
		if (isLoading)
		{
			return;
		}
		isLoading = true;
		RefreshButton.IsEnabled = false;
		HistoryGrid.ItemsSource = null;
		InfoTextBlock.Text = "Hareketler yükleniyor...";
		StatusTextBlock.Text = "";
		try
		{
			ProductHistoryResult productHistoryResult = await Task.Run(() => ProductHistoryRepository.GetHistory(productId));
			HistoryGrid.ItemsSource = productHistoryResult.Items;
			InfoTextBlock.Text = (productHistoryResult.HasInitialStockRecord ? "Hareketler yeniden eskiye sıralıdır. “Sonraki stok”, ilgili işlem tamamlandığı andaki stok miktarıdır. Fiyat değişiminde stok değişmez." : "Bu ürünün başlangıç stok miktarı ayrı bir kayıt olarak tutulmamış. Bilinen hareketler gösteriliyor; eksik başlangıç miktarı tahmin edilmiyor.");
			StatusTextBlock.Text = ((productHistoryResult.Items.Count == 0) ? "Bu ürün için henüz görüntülenebilir hareket yok." : $"{productHistoryResult.Items.Count} hareket listelendi.");
		}
		catch (Exception ex)
		{
			InfoTextBlock.Text = "Ürün hareketleri yüklenemedi.";
			StatusTextBlock.Text = "Yenile düğmesiyle tekrar deneyebilirsin.";
			MessageBox.Show(ex.Message, "Ürün geçmişi", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		finally
		{
			isLoading = false;
			RefreshButton.IsEnabled = true;
		}
	}

	private async void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		await LoadHistoryAsync();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.2.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/BereketMarket;component/producthistorywindow.xaml", UriKind.Relative);
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
			InfoTextBlock = (TextBlock)target;
			break;
		case 4:
			HistoryGrid = (DataGrid)target;
			break;
		case 5:
			StatusTextBlock = (TextBlock)target;
			break;
		case 6:
			RefreshButton = (Button)target;
			RefreshButton.Click += RefreshButton_Click;
			break;
		case 7:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
