using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BereketMarket;

public class SalesHistoryWindow : Window, IComponentConnector
{
	private bool isLoading;

	private int detailRequestVersion;

	internal WrapPanel FilterPanel;

	internal DatePicker StartDatePicker;

	internal DatePicker EndDatePicker;

	internal TextBlock TotalSalesTextBlock;

	internal TextBlock TransactionCountTextBlock;

	internal TextBlock ItemCountTextBlock;

	internal DataGrid SalesGrid;

	internal TextBlock DetailTitleTextBlock;

	internal DataGrid DetailsGrid;

	internal TextBlock StatusTextBlock;

	private bool _contentLoaded;

	public SalesHistoryWindow()
	{
		InitializeComponent();
		StartDatePicker.SelectedDate = DateTime.Today;
		EndDatePicker.SelectedDate = DateTime.Today;
		Loaded += async (object _, RoutedEventArgs _) =>
		{
			await LoadSalesAsync();
		};
	}

	private async Task LoadSalesAsync()
	{
		if (isLoading)
		{
			return;
		}
		if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
		{
			StatusTextBlock.Text = "Başlangıç ve bitiş tarihlerini seç.";
			return;
		}
		DateTime start = StartDatePicker.SelectedDate.Value.Date;
		DateTime end = EndDatePicker.SelectedDate.Value.Date;
		if (start > end)
		{
			StatusTextBlock.Text = "Başlangıç tarihi bitiş tarihinden sonra olamaz.";
			return;
		}
		if (end == DateTime.MaxValue.Date)
		{
			StatusTextBlock.Text = "Daha küçük bir bitiş tarihi seç.";
			return;
		}
		isLoading = true;
		FilterPanel.IsEnabled = false;
		SalesGrid.IsEnabled = false;
		detailRequestVersion++;
		SalesGrid.ItemsSource = null;
		DetailsGrid.ItemsSource = null;
		TotalSalesTextBlock.Text = "0,00 TL";
		TransactionCountTextBlock.Text = "0";
		ItemCountTextBlock.Text = "0";
		DetailTitleTextBlock.Text = "Satış içeriğini görmek için yukarıdan bir satış seç.";
		StatusTextBlock.Text = "Satışlar yükleniyor...";
		try
		{
			List<SaleHistoryItem> list = await Task.Run(() => SalesHistoryRepository.GetSales(start, end));
			SalesGrid.ItemsSource = list;
			List<SaleHistoryItem> list2 = list.Where((SaleHistoryItem item) => item.IsCompleted).ToList();
			int value = Enumerable.Count(list, (SaleHistoryItem item) => item.Status == "Cancelled");
			decimal num = list2.Sum((SaleHistoryItem item) => item.Total);
			long num2 = list2.Sum((SaleHistoryItem item) => item.ItemCount);
			TotalSalesTextBlock.Text = num.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) + " TL";
			TransactionCountTextBlock.Text = list2.Count.ToString();
			ItemCountTextBlock.Text = num2.ToString();
			StatusTextBlock.Text = ((list.Count == 0) ? "Seçilen tarihlerde satış bulunamadı." : $"{start:dd.MM.yyyy} – {end:dd.MM.yyyy}: {list2.Count} geçerli, {value} iptal edilmiş satış.");
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = "Satış geçmişi yüklenemedi.";
			MessageBox.Show(ex.Message, "Satış geçmişi", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		finally
		{
			isLoading = false;
			FilterPanel.IsEnabled = true;
			SalesGrid.IsEnabled = true;
		}
	}

	private async void SalesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		int requestVersion = ++detailRequestVersion;
		DetailsGrid.ItemsSource = null;
		object selectedItem = SalesGrid.SelectedItem;
		SaleHistoryItem selected = selectedItem as SaleHistoryItem;
		if (selected == null)
		{
			DetailTitleTextBlock.Text = "Satış içeriğini görmek için yukarıdan bir satış seç.";
			return;
		}
		DetailTitleTextBlock.Text = $"Satış #{selected.Id} — ürünler yükleniyor...";
		try
		{
			List<SaleHistoryDetail> itemsSource = await Task.Run(() => SalesHistoryRepository.GetDetails(selected.Id));
			if (requestVersion == detailRequestVersion)
			{
				DetailsGrid.ItemsSource = itemsSource;
				DetailTitleTextBlock.Text = $"Satış #{selected.Id} — {selected.DateDisplay}";
			}
		}
		catch (Exception ex)
		{
			if (requestVersion == detailRequestVersion)
			{
				DetailTitleTextBlock.Text = "Satış detayları yüklenemedi.";
				MessageBox.Show(ex.Message, "Satış detayı", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
	}

	private async void FilterButton_Click(object sender, RoutedEventArgs e)
	{
		await LoadSalesAsync();
	}

	private async void TodayButton_Click(object sender, RoutedEventArgs e)
	{
		StartDatePicker.SelectedDate = DateTime.Today;
		EndDatePicker.SelectedDate = DateTime.Today;
		await LoadSalesAsync();
	}

	private async void ThisMonthButton_Click(object sender, RoutedEventArgs e)
	{
		DateTime today = DateTime.Today;
		StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
		EndDatePicker.SelectedDate = today;
		await LoadSalesAsync();
	}

	private async void CancelSaleButton_Click(object sender, RoutedEventArgs e)
	{
		if (!isLoading)
		{
			object selectedItem = SalesGrid.SelectedItem;
			if (!(selectedItem is SaleHistoryItem selected))
			{
				MessageBox.Show("Önce listeden iptal edilecek satışı seç.", "Satış seçimi", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
			else if (!selected.IsCompleted)
			{
				MessageBox.Show("Bu satış zaten iptal edilmiş veya iptal edilebilir durumda değil.", "Satış iptali", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
			else if (new SaleCancellationWindow(selected)
			{
				Owner = this
			}.ShowDialog() == true)
			{
				await LoadSalesAsync();
				MessageBox.Show($"Satış #{selected.Id} iptal edildi.\n" + "Ürünler stoğa geri eklendi.", "İptal tamamlandı", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
		}
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
			Uri resourceLocator = new Uri("/BereketMarket;component/saleshistorywindow.xaml", UriKind.Relative);
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
			FilterPanel = (WrapPanel)target;
			break;
		case 2:
			StartDatePicker = (DatePicker)target;
			break;
		case 3:
			EndDatePicker = (DatePicker)target;
			break;
		case 4:
			((Button)target).Click += FilterButton_Click;
			break;
		case 5:
			((Button)target).Click += TodayButton_Click;
			break;
		case 6:
			((Button)target).Click += ThisMonthButton_Click;
			break;
		case 7:
			TotalSalesTextBlock = (TextBlock)target;
			break;
		case 8:
			TransactionCountTextBlock = (TextBlock)target;
			break;
		case 9:
			ItemCountTextBlock = (TextBlock)target;
			break;
		case 10:
			SalesGrid = (DataGrid)target;
			SalesGrid.SelectionChanged += SalesGrid_SelectionChanged;
			break;
		case 11:
			DetailTitleTextBlock = (TextBlock)target;
			break;
		case 12:
			DetailsGrid = (DataGrid)target;
			break;
		case 13:
			StatusTextBlock = (TextBlock)target;
			break;
		case 14:
			((Button)target).Click += CancelSaleButton_Click;
			break;
		case 15:
			((Button)target).Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
