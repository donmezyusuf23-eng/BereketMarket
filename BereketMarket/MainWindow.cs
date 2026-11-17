using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BereketMarket;

public class MainWindow : Window
{
	private TextBox _barcodeBox = null!;
	private TextBox _nameBox = null!;
	private TextBox _purchasePriceBox = null!;
	private TextBox _salePriceBox = null!;
	private TextBox _stockBox = null!;
	private TextBox _criticalStockBox = null!;
	private Button _btnSave = null!;
	private TextBlock _statusText = null!;

	public MainWindow()
	{
		Title = "Bereket Market — Yeni Ürün Tanımla";
		Width = 580;
		Height = 650;
		MinWidth = 500;
		MinHeight = 550;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		FontFamily = new FontFamily("Segoe UI");
		FontSize = 14;

		BuildUi();
		Loaded += (s, e) => _barcodeBox.Focus();
	}

	private void BuildUi()
	{
		Grid mainGrid = new Grid { Margin = new Thickness(28) };
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		StackPanel titleStack = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
		TextBlock t1 = new TextBlock
		{
			Text = "YENİ ÜRÜN TANIMLA",
			FontSize = 22,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
		};
		TextBlock t2 = new TextBlock
		{
			Text = "Alış ve satış fiyatlarıyla yeni ürün kaydı",
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 4, 0, 0)
		};
		titleStack.Children.Add(t1);
		titleStack.Children.Add(t2);
		Grid.SetRow(titleStack, 0);
		mainGrid.Children.Add(titleStack);

		ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		StackPanel formStack = new StackPanel();

		_barcodeBox = CreateInputRow(formStack, "Ürün Barkodu (Elle yazın veya okutun):");
		_nameBox = CreateInputRow(formStack, "Ürün Adı:");

		Grid pricesGrid = new Grid();
		pricesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		pricesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
		pricesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		StackPanel p1 = new StackPanel();
		_purchasePriceBox = CreateInputBox();
		p1.Children.Add(CreateLabel("Alış Fiyatı (TL):"));
		p1.Children.Add(_purchasePriceBox);
		Grid.SetColumn(p1, 0);
		pricesGrid.Children.Add(p1);

		StackPanel p2 = new StackPanel();
		_salePriceBox = CreateInputBox();
		p2.Children.Add(CreateLabel("Satış Fiyatı (TL):"));
		p2.Children.Add(_salePriceBox);
		Grid.SetColumn(p2, 2);
		pricesGrid.Children.Add(p2);

		formStack.Children.Add(pricesGrid);

		Grid stockGrid = new Grid { Margin = new Thickness(0, 16, 0, 0) };
		stockGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		stockGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
		stockGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		StackPanel s1 = new StackPanel();
		_stockBox = CreateInputBox();
		_stockBox.Text = "0";
		s1.Children.Add(CreateLabel("Başlangıç Stok Miktarı:"));
		s1.Children.Add(_stockBox);
		Grid.SetColumn(s1, 0);
		stockGrid.Children.Add(s1);

		StackPanel s2 = new StackPanel();
		_criticalStockBox = CreateInputBox();
		_criticalStockBox.Text = "5";
		s2.Children.Add(CreateLabel("Kritik Stok Uyarısı (Adet):"));
		s2.Children.Add(_criticalStockBox);
		Grid.SetColumn(s2, 2);
		stockGrid.Children.Add(s2);

		formStack.Children.Add(stockGrid);

		scroll.Content = formStack;
		Grid.SetRow(scroll, 1);
		mainGrid.Children.Add(scroll);

		_statusText = new TextBlock
		{
			Text = "",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 12, 0, 12)
		};
		Grid.SetRow(_statusText, 2);
		mainGrid.Children.Add(_statusText);

		Grid btnGrid = new Grid();
		btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
		btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		Button btnCancel = CreateButton("Vazgeç", "#334155", "#FFFFFF", 14, false);
		btnCancel.Height = 44;
		btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
		Grid.SetColumn(btnCancel, 0);
		btnGrid.Children.Add(btnCancel);

		_btnSave = CreateButton("💾 Ürünü Kaydet", "#0D9488", "#FFFFFF", 14, true);
		_btnSave.Height = 44;
		_btnSave.Click += async (s, e) => await SaveProductAsync();
		Grid.SetColumn(_btnSave, 2);
		btnGrid.Children.Add(_btnSave);

		Grid.SetRow(btnGrid, 3);
		mainGrid.Children.Add(btnGrid);

		Content = mainGrid;
	}

	private async Task SaveProductAsync()
	{
		string barcode = _barcodeBox.Text.Trim();
		string name = _nameBox.Text.Trim();
		string pPriceTxt = _purchasePriceBox.Text.Replace(',', '.').Trim();
		string sPriceTxt = _salePriceBox.Text.Replace(',', '.').Trim();
		string stockTxt = _stockBox.Text.Trim();
		string critTxt = _criticalStockBox.Text.Trim();

		if (string.IsNullOrWhiteSpace(barcode))
		{
			ShowError("Lütfen bir barkod girin veya okutun.");
			_barcodeBox.Focus();
			return;
		}

		if (string.IsNullOrWhiteSpace(name))
		{
			ShowError("Lütfen ürün adını girin.");
			_nameBox.Focus();
			return;
		}

		if (!decimal.TryParse(sPriceTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal salePrice) || salePrice < 0)
		{
			ShowError("Lütfen geçerli bir satış fiyatı girin.");
			_salePriceBox.Focus();
			return;
		}

		decimal purchasePrice = 0;
		if (!string.IsNullOrWhiteSpace(pPriceTxt))
		{
			decimal.TryParse(pPriceTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out purchasePrice);
		}

		if (!int.TryParse(stockTxt, out int stock) || stock < 0)
		{
			ShowError("Lütfen geçerli bir stok miktarı girin.");
			_stockBox.Focus();
			return;
		}

		int criticalStock = 5;
		if (!string.IsNullOrWhiteSpace(critTxt))
		{
			int.TryParse(critTxt, out criticalStock);
		}

		_btnSave.IsEnabled = false;
		try
		{
			long sPriceKurus = checked((long)Math.Round(salePrice * 100m));
			long pPriceKurus = checked((long)Math.Round(purchasePrice * 100m));

			await Task.Run(() => ProductRepository.Add(barcode, name, sPriceKurus, stock, pPriceKurus, criticalStock));

			DialogResult = true;
			Close();
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
		finally
		{
			_btnSave.IsEnabled = true;
		}
	}

	private void ShowError(string msg)
	{
		_statusText.Text = msg;
		_statusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
	}

	private TextBlock CreateLabel(string text)
	{
		return new TextBlock
		{
			Text = text,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 0, 0, 6)
		};
	}

	private TextBox CreateInputBox()
	{
		return new TextBox
		{
			FontSize = 15,
			Padding = new Thickness(12, 10, 12, 10),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};
	}

	private TextBox CreateInputRow(StackPanel parent, string label)
	{
		parent.Children.Add(CreateLabel(label));
		TextBox box = CreateInputBox();
		box.Margin = new Thickness(0, 0, 0, 16);
		parent.Children.Add(box);
		return box;
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
		b.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
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
