using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace BereketMarket;

public class ProductListWindow : Window
{
	private TextBox _searchBox = null!;
	private CheckBox _includeInactive = null!;
	private DataGrid _grid = null!;
	private TextBlock _summaryText = null!;
	private Button _btnEditPrice = null!;
	private Button _btnAddStock = null!;
	private Button _btnAdjustStock = null!;
	private Button _btnEditProduct = null!;
	private Button _btnHistory = null!;
	private Button _btnToggleStatus = null!;

	public ProductListWindow()
	{
		Title = "Bereket Market — Ürün ve Stok Yönetimi";
		Width = 1180;
		Height = 750;
		MinWidth = 950;
		MinHeight = 600;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		FontFamily = new FontFamily("Segoe UI");
		FontSize = 14;

		BuildUi();
		Loaded += async (s, e) =>
		{
			await LoadProductsAsync();
			_searchBox.Focus();
		};
	}

	private void BuildUi()
	{
		Grid mainGrid = new Grid();
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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

		StackPanel titleStack = new StackPanel();
		TextBlock title = new TextBlock
		{
			Text = "ÜRÜNLER & STOK LİSTESİ",
			FontSize = 24,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
		};
		TextBlock subtitle = new TextBlock
		{
			Text = "Alış/satış fiyatları, kâr marjları ve kritik stok takibi",
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 4, 0, 0)
		};
		titleStack.Children.Add(title);
		titleStack.Children.Add(subtitle);
		Grid.SetColumn(titleStack, 0);
		headerGrid.Children.Add(titleStack);

		Button btnNewProduct = CreateButton("+ Yeni Ürün Ekle", "#0D9488", "#FFFFFF", 14, true);
		btnNewProduct.Padding = new Thickness(18, 10, 18, 10);
		btnNewProduct.Click += (s, e) =>
		{
			MainWindow newProdWindow = new MainWindow { Owner = this };
			if (newProdWindow.ShowDialog() == true)
			{
				_ = LoadProductsAsync();
			}
		};
		Grid.SetColumn(btnNewProduct, 1);
		headerGrid.Children.Add(btnNewProduct);

		headerBorder.Child = headerGrid;
		Grid.SetRow(headerBorder, 0);
		mainGrid.Children.Add(headerBorder);

		Border searchBarBorder = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Padding = new Thickness(24, 12, 24, 12),
			Margin = new Thickness(24, 16, 24, 0),
			CornerRadius = new CornerRadius(12),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		Grid searchGrid = new Grid();
		searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_searchBox = new TextBox
		{
			FontSize = 14,
			Padding = new Thickness(12, 9, 12, 9),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};
		_searchBox.TextChanged += async (s, e) => await LoadProductsAsync();
		Grid.SetColumn(_searchBox, 0);
		searchGrid.Children.Add(_searchBox);

		Button btnSearch = CreateButton("🔍 Ara", "#2563EB", "#FFFFFF", 13, true);
		btnSearch.Padding = new Thickness(16, 8, 16, 8);
		btnSearch.Margin = new Thickness(10, 0, 0, 0);
		btnSearch.Click += async (s, e) => await LoadProductsAsync();
		Grid.SetColumn(btnSearch, 1);
		searchGrid.Children.Add(btnSearch);

		_includeInactive = new CheckBox
		{
			Content = "Pasif ürünleri de göster",
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(16, 0, 0, 0),
			FontSize = 13
		};
		_includeInactive.Checked += async (s, e) => await LoadProductsAsync();
		_includeInactive.Unchecked += async (s, e) => await LoadProductsAsync();
		Grid.SetColumn(_includeInactive, 2);
		searchGrid.Children.Add(_includeInactive);

		searchBarBorder.Child = searchGrid;
		Grid.SetRow(searchBarBorder, 1);
		mainGrid.Children.Add(searchBarBorder);

		Border tableCard = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(16),
			Margin = new Thickness(24, 16, 24, 16),
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
		_grid.SelectionChanged += (s, e) => UpdateButtonStates();

		_grid.Columns.Add(new DataGridTextColumn { Header = "Barkod", Binding = new Binding("Barcode"), Width = new DataGridLength(120) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Ürün Adı", Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Alış Fiyatı", Binding = new Binding("PurchasePrice") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(110) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Satış Fiyatı", Binding = new Binding("Price") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(110) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Tahmini Kâr", Binding = new Binding("Profit") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(110) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Mevcut Stok", Binding = new Binding("Stock"), Width = new DataGridLength(100) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Stok Durumu", Binding = new Binding("StockStatusDisplay"), Width = new DataGridLength(120) });
		_grid.Columns.Add(new DataGridTextColumn { Header = "Durum", Binding = new Binding("StatusDisplay"), Width = new DataGridLength(90) });

		tableCard.Child = _grid;
		Grid.SetRow(tableCard, 2);
		mainGrid.Children.Add(tableCard);

		Border bottomBar = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Padding = new Thickness(24, 14, 24, 14),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(0, 1, 0, 0)
		};

		Grid bottomGrid = new Grid();
		bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_summaryText = new TextBlock
		{
			Text = "Toplam 0 ürün listeleniyor.",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_summaryText, 0);
		bottomGrid.Children.Add(_summaryText);

		StackPanel actionButtons = new StackPanel { Orientation = Orientation.Horizontal };

		_btnAddStock = CreateButton("📥 Stok Ekle", "#059669", "#FFFFFF", 13, true);
		_btnAddStock.Padding = new Thickness(14, 8, 14, 8);
		_btnAddStock.IsEnabled = false;
		_btnAddStock.Click += (s, e) =>
		{
			if (_grid.SelectedItem is ProductListItem p)
			{
				StockEntryWindow w = new StockEntryWindow(p) { Owner = this };
				if (w.ShowDialog() == true) _ = LoadProductsAsync();
			}
		};
		actionButtons.Children.Add(_btnAddStock);

		_btnAdjustStock = CreateButton("⚖️ Stok Düzelt", "#D97706", "#FFFFFF", 13, true);
		_btnAdjustStock.Padding = new Thickness(14, 8, 14, 8);
		_btnAdjustStock.Margin = new Thickness(8, 0, 0, 0);
		_btnAdjustStock.IsEnabled = false;
		_btnAdjustStock.Click += (s, e) =>
		{
			if (_grid.SelectedItem is ProductListItem p)
			{
				StockAdjustmentWindow w = new StockAdjustmentWindow(p) { Owner = this };
				if (w.ShowDialog() == true) _ = LoadProductsAsync();
			}
		};
		actionButtons.Children.Add(_btnAdjustStock);

		_btnEditPrice = CreateButton("🏷️ Fiyat Değiştir", "#2563EB", "#FFFFFF", 13, true);
		_btnEditPrice.Padding = new Thickness(14, 8, 14, 8);
		_btnEditPrice.Margin = new Thickness(8, 0, 0, 0);
		_btnEditPrice.IsEnabled = false;
		_btnEditPrice.Click += (s, e) =>
		{
			if (_grid.SelectedItem is ProductListItem p)
			{
				PriceEditWindow w = new PriceEditWindow(p) { Owner = this };
				if (w.ShowDialog() == true) _ = LoadProductsAsync();
			}
		};
		actionButtons.Children.Add(_btnEditPrice);

		_btnEditProduct = CreateButton("✏️ Bilgi Düzenle", "#475569", "#FFFFFF", 13, true);
		_btnEditProduct.Padding = new Thickness(14, 8, 14, 8);
		_btnEditProduct.Margin = new Thickness(8, 0, 0, 0);
		_btnEditProduct.IsEnabled = false;
		_btnEditProduct.Click += (s, e) =>
		{
			if (_grid.SelectedItem is ProductListItem p)
			{
				ProductEditWindow w = new ProductEditWindow(p) { Owner = this };
				if (w.ShowDialog() == true) _ = LoadProductsAsync();
			}
		};
		actionButtons.Children.Add(_btnEditProduct);

		_btnHistory = CreateButton("📜 Geçmiş", "#334155", "#FFFFFF", 13, false);
		_btnHistory.Padding = new Thickness(14, 8, 14, 8);
		_btnHistory.Margin = new Thickness(8, 0, 0, 0);
		_btnHistory.IsEnabled = false;
		_btnHistory.Click += (s, e) =>
		{
			if (_grid.SelectedItem is ProductListItem p)
			{
				ProductHistoryWindow w = new ProductHistoryWindow(p) { Owner = this };
				w.ShowDialog();
			}
		};
		actionButtons.Children.Add(_btnHistory);

		_btnToggleStatus = CreateButton("🔄 Aktif/Pasif", "#64748B", "#FFFFFF", 13, false);
		_btnToggleStatus.Padding = new Thickness(14, 8, 14, 8);
		_btnToggleStatus.Margin = new Thickness(8, 0, 0, 0);
		_btnToggleStatus.IsEnabled = false;
		_btnToggleStatus.Click += (s, e) =>
		{
			if (_grid.SelectedItem is ProductListItem p)
			{
				bool newStatus = !p.IsActive;
				string msg = newStatus ? $"{p.Name} aktif hale getirilsin mi?" : $"{p.Name} pasife alınsın mı?";
				if (MessageBox.Show(msg, "Durum Değiştir", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					ProductStatusRepository.ChangeStatus(p.Id, p.IsActive, newStatus);
					_ = LoadProductsAsync();
				}
			}
		};
		actionButtons.Children.Add(_btnToggleStatus);

		Grid.SetColumn(actionButtons, 1);
		bottomGrid.Children.Add(actionButtons);

		bottomBar.Child = bottomGrid;
		Grid.SetRow(bottomBar, 3);
		mainGrid.Children.Add(bottomBar);

		Content = mainGrid;
	}

	private void UpdateButtonStates()
	{
		bool hasSelected = _grid.SelectedItem != null;
		_btnAddStock.IsEnabled = hasSelected;
		_btnAdjustStock.IsEnabled = hasSelected;
		_btnEditPrice.IsEnabled = hasSelected;
		_btnEditProduct.IsEnabled = hasSelected;
		_btnHistory.IsEnabled = hasSelected;
		_btnToggleStatus.IsEnabled = hasSelected;
	}

	private async Task LoadProductsAsync()
	{
		string search = _searchBox.Text.Trim();
		bool includeInactive = _includeInactive.IsChecked == true;
		List<ProductListItem> products = await Task.Run(() => ProductListRepository.GetProducts(search, includeInactive));

		_grid.ItemsSource = products;

		int totalCount = products.Count;
		int lowStockCount = products.Count(p => p.IsLowStock);
		decimal totalStockValue = products.Sum(p => p.Price * p.Stock);

		_summaryText.Text = $"Toplam {totalCount} ürün listelendi • Kritik Stok: {lowStockCount} ürün • Toplam Stok Değeri: {totalStockValue:N2} TL";
		_summaryText.Foreground = lowStockCount > 0
			? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"))
			: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

		UpdateButtonStates();
	}

	private Button CreateButton(string text, string bgHex, string fgHex, double fontSize, bool bold)
	{
		Button btn = new Button
		{
			Content = text,
			FontSize = fontSize,
			FontWeight = bold ? FontWeights.Bold : FontWeights.SemiBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
			Cursor = Cursors.Hand,
			BorderThickness = new Thickness(0)
		};

		ControlTemplate t = new ControlTemplate(typeof(Button));
		FrameworkElementFactory b = new FrameworkElementFactory(typeof(Border));
		b.Name = "Bd";
		b.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
		b.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
		b.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

		FrameworkElementFactory p = new FrameworkElementFactory(typeof(ContentPresenter));
		p.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		p.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		b.AppendChild(p);
		t.VisualTree = b;

		Trigger mOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		mOver.Setters.Add(new Setter(UIElement.OpacityProperty, 0.88));
		t.Triggers.Add(mOver);

		btn.Template = t;
		return btn;
	}
}
