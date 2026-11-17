using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BereketMarket;

public class CashRegisterBookWindow : Window
{
	private readonly CashRegisterRepository _repository = new CashRegisterRepository();
	private DatePicker _datePicker = null!;
	private TextBlock _txtTotalCash = null!;
	private TextBlock _txtTotalDebt = null!;
	private TextBlock _txtTotalRevenue = null!;
	private TextBlock _txtTotalProfit = null!;
	private TextBlock _txtTotalItems = null!;
	private DataGrid _grid = null!;

	public CashRegisterBookWindow()
	{
		Title = "Bereket Market — Günlük Kasa Defteri";
		Width = 1150;
		Height = 750;
		MinWidth = 950;
		MinHeight = 600;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		FontFamily = new FontFamily("Segoe UI");
		FontSize = 14;

		BuildUi();
		Loaded += async (s, e) => await LoadDataAsync();
	}

	private void BuildUi()
	{
		Grid mainGrid = new Grid();
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		Border headerBorder = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Padding = new Thickness(24, 16, 24, 16),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(0, 0, 0, 1)
		};

		Grid headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel titlePanel = new StackPanel();
		TextBlock headerTitle = new TextBlock
		{
			Text = "GÜNLÜK KASA DEFTERİ",
			FontSize = 24,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
		};
		TextBlock headerSub = new TextBlock
		{
			Text = "Gün boyunca satılan tüm ürünler ve anlık kasa durumu",
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 4, 0, 0)
		};
		titlePanel.Children.Add(headerTitle);
		titlePanel.Children.Add(headerSub);
		Grid.SetColumn(titlePanel, 0);
		headerGrid.Children.Add(titlePanel);

		StackPanel controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
		TextBlock dateLabel = new TextBlock
		{
			Text = "Tarih:",
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 0, 10, 0),
			FontWeight = FontWeights.SemiBold
		};

		_datePicker = new DatePicker
		{
			SelectedDate = DateTime.Today,
			Width = 140,
			Height = 36,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		_datePicker.SelectedDateChanged += async (s, e) => await LoadDataAsync();

		Button btnRefresh = new Button
		{
			Content = "Yenile",
			Width = 80,
			Height = 36,
			Margin = new Thickness(10, 0, 0, 0),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D9488")),
			Foreground = Brushes.White,
			FontWeight = FontWeights.Bold,
			BorderThickness = new Thickness(0)
		};
		btnRefresh.Click += async (s, e) => await LoadDataAsync();

		controlsPanel.Children.Add(dateLabel);
		controlsPanel.Children.Add(_datePicker);
		controlsPanel.Children.Add(btnRefresh);
		Grid.SetColumn(controlsPanel, 1);
		headerGrid.Children.Add(controlsPanel);

		headerBorder.Child = headerGrid;
		Grid.SetRow(headerBorder, 0);
		mainGrid.Children.Add(headerBorder);

		Grid cardsGrid = new Grid { Margin = new Thickness(24, 20, 24, 0) };
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
		cardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		_txtTotalCash = new TextBlock();
		Border cardCash = CreateSummaryCard("KASADAKİ NAKİT", _txtTotalCash, "#065F46", "#10B981", "#D1FAE5");
		Grid.SetColumn(cardCash, 0);
		cardsGrid.Children.Add(cardCash);

		_txtTotalDebt = new TextBlock();
		Border cardDebt = CreateSummaryCard("VERESİYE ÇIKIŞI", _txtTotalDebt, "#831843", "#F43F5E", "#FFE4E6");
		Grid.SetColumn(cardDebt, 2);
		cardsGrid.Children.Add(cardDebt);

		_txtTotalRevenue = new TextBlock();
		Border cardTotal = CreateSummaryCard("TOPLAM CİRO", _txtTotalRevenue, "#1E3A8A", "#3B82F6", "#DBEAFE");
		Grid.SetColumn(cardTotal, 4);
		cardsGrid.Children.Add(cardTotal);

		_txtTotalProfit = new TextBlock();
		Border cardProfit = CreateSummaryCard("NET TAHMİNİ KÂR", _txtTotalProfit, "#581C87", "#A855F7", "#F3E8FF");
		Grid.SetColumn(cardProfit, 6);
		cardsGrid.Children.Add(cardProfit);

		Grid.SetRow(cardsGrid, 1);
		mainGrid.Children.Add(cardsGrid);

		Border tableContainer = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(16),
			Margin = new Thickness(24, 18, 24, 24),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		_grid = new DataGrid
		{
			AutoGenerateColumns = false,
			IsReadOnly = true,
			CanUserAddRows = false,
			HeadersVisibility = DataGridHeadersVisibility.Column,
			GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
			HorizontalGridLinesBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0),
			RowHeight = 42,
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
			RowBackground = Brushes.Transparent,
			AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#152238"))
		};

		_grid.Columns.Add(new DataGridTextColumn { Header = "Saat", Binding = new Binding("TimeString"), Width = new DataGridLength(100) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Satılan Ürün", Binding = new Binding("ProductName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Adet", Binding = new Binding("Quantity"), Width = new DataGridLength(80) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Birim Fiyat", Binding = new Binding("UnitPriceLira") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(110) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Toplam", Binding = new Binding("TotalPriceLira") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(120) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Kâr", Binding = new Binding("ProfitLira") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(100) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Ödeme Türü", Binding = new Binding("PaymentType"), Width = new DataGridLength(110) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Müşteri (Veresiye)", Binding = new Binding("CustomerName"), Width = new DataGridLength(160) });

		tableContainer.Child = _grid;
		Grid.SetRow(tableContainer, 2);
		mainGrid.Children.Add(tableContainer);

		Content = mainGrid;
	}

	private Border CreateSummaryCard(string title, TextBlock valueBlock, string borderHex, string textHex, string bgHex)
	{
		Border card = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 14, 16, 14),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex)),
			BorderThickness = new Thickness(1, 4, 1, 1)
		};

		StackPanel p = new StackPanel();
		TextBlock lbl = new TextBlock
		{
			Text = title,
			FontSize = 11,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
		};

		valueBlock.Text = "0,00 TL";
		valueBlock.FontSize = 22;
		valueBlock.FontWeight = FontWeights.ExtraBold;
		valueBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textHex));
		valueBlock.Margin = new Thickness(0, 4, 0, 0);

		p.Children.Add(lbl);
		p.Children.Add(valueBlock);
		card.Child = p;
		return card;
	}

	private async System.Threading.Tasks.Task LoadDataAsync()
	{
		DateTime date = _datePicker.SelectedDate ?? DateTime.Today;
		var (summary, items) = await _repository.GetDailyRegisterAsync(date);

		_txtTotalCash.Text = $"{summary.TotalCashSalesLira:N2} TL";
		_txtTotalDebt.Text = $"{summary.TotalDebtSalesLira:N2} TL";
		_txtTotalRevenue.Text = $"{summary.TotalSalesLira:N2} TL";
		_txtTotalProfit.Text = $"{summary.TotalProfitLira:N2} TL";

		_grid.ItemsSource = items;
	}
}
