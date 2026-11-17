using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace BereketMarket;

public class SalesWindow : Window
{
	private readonly ObservableCollection<CartItem> _cart = new ObservableCollection<CartItem>();
	private readonly SemaphoreSlim _scanLock = new SemaphoreSlim(1, 1);
	private readonly QuickProductRepository _quickRepo = new QuickProductRepository();
	private int _pendingScans;
	private bool _isCompleting;

	private TextBox _barcodeInput = null!;
	private DataGrid _cartGrid = null!;
	private TextBlock _totalTextBlock = null!;
	private TextBlock _cartSummaryTextBlock = null!;
	private TextBlock _statusTextBlock = null!;
	private Button _btnCashSale = null!;
	private Button _btnDebtSale = null!;
	private WrapPanel _quickProductsPanel = null!;

	public SalesWindow()
	{
		Title = "Bereket Market — Hızlı Kasa & Satış";
		Width = 1200;
		Height = 780;
		MinWidth = 1020;
		MinHeight = 650;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		FontFamily = new FontFamily("Segoe UI");
		FontSize = 14;

		BuildUi();

		_cartGrid.ItemsSource = _cart;

		Loaded += async (s, e) =>
		{
			await LoadQuickProductsAsync();
			_barcodeInput.Focus();
		};

		Closing += (s, e) =>
		{
			if (_isCompleting || _pendingScans > 0)
			{
				e.Cancel = true;
				ShowStatus("İşlem sürerken pencere kapatılamaz.", isError: true);
			}
			else if (_cart.Count > 0)
			{
				MessageBoxResult res = MessageBox.Show("Sepette ürünler var. Çıkarsan sepet temizlenecek.\n\nÇıkmak istiyor musun?", "Alışverişten Çık", MessageBoxButton.YesNo, MessageBoxImage.Question);
				e.Cancel = res != MessageBoxResult.Yes;
			}
		};
	}

	private void BuildUi()
	{
		Grid mainGrid = new Grid();
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		Border headerBorder = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Padding = new Thickness(24, 14, 24, 14),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(0, 0, 0, 1)
		};

		Grid headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel titleStack = new StackPanel();
		TextBlock titleText = new TextBlock
		{
			Text = "HIZLI SATIŞ & KASA",
			FontSize = 24,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
		};
		TextBlock subTitle = new TextBlock
		{
			Text = "Barkod okutun veya hızlı ürün butonlarını kullanın",
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 3, 0, 0)
		};
		titleStack.Children.Add(titleText);
		titleStack.Children.Add(subTitle);
		Grid.SetColumn(titleStack, 0);
		headerGrid.Children.Add(titleStack);

		Border badge = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064E3B")),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(14, 6, 14, 6),
			VerticalAlignment = VerticalAlignment.Center,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
			BorderThickness = new Thickness(1)
		};
		badge.Child = new TextBlock
		{
			Text = "BEREKET MARKET KASA",
			FontWeight = FontWeights.Bold,
			FontSize = 12,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7"))
		};
		Grid.SetColumn(badge, 1);
		headerGrid.Children.Add(badge);

		headerBorder.Child = headerGrid;
		Grid.SetRow(headerBorder, 0);
		mainGrid.Children.Add(headerBorder);

		Grid contentGrid = new Grid { Margin = new Thickness(20) };
		contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
		contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(380) });

		Grid leftGrid = new Grid();
		leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		Border scanCard = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(14),
			Margin = new Thickness(0, 0, 0, 14),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		Grid scanGrid = new Grid();
		scanGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		scanGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
		scanGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_barcodeInput = new TextBox
		{
			FontSize = 20,
			FontWeight = FontWeights.SemiBold,
			Padding = new Thickness(14, 10, 14, 10),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
			BorderThickness = new Thickness(1.5),
			VerticalContentAlignment = VerticalAlignment.Center
		};
		_barcodeInput.KeyDown += async (s, e) =>
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				await SubmitBarcodeAsync();
			}
		};
		Grid.SetColumn(_barcodeInput, 0);
		scanGrid.Children.Add(_barcodeInput);

		Button btnAddBarcode = CreateButton("Ürünü Ekle", "#2563EB", "#FFFFFF", 14, true);
		btnAddBarcode.Padding = new Thickness(20, 10, 20, 10);
		btnAddBarcode.Click += async (s, e) => await SubmitBarcodeAsync();
		Grid.SetColumn(btnAddBarcode, 2);
		scanGrid.Children.Add(btnAddBarcode);

		scanCard.Child = scanGrid;
		Grid.SetRow(scanCard, 0);
		leftGrid.Children.Add(scanCard);

		Border cartCard = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(14),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		_cartGrid = new DataGrid
		{
			AutoGenerateColumns = false,
			IsReadOnly = true,
			CanUserAddRows = false,
			HeadersVisibility = DataGridHeadersVisibility.Column,
			GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
			HorizontalGridLinesBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0),
			RowHeight = 44,
			FontSize = 14,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
			RowBackground = Brushes.Transparent,
			AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#152238"))
		};

		_cartGrid.Columns.Add(new DataGridTextColumn { Header = "Barkod", Binding = new Binding("Barcode"), Width = new DataGridLength(110) });
		_cartGrid.Columns.Add(new DataGridTextColumn { Header = "Ürün Adı", Binding = new Binding("Name"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
		_cartGrid.Columns.Add(new DataGridTextColumn { Header = "Fiyat", Binding = new Binding("UnitPrice") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(100) });
		_cartGrid.Columns.Add(new DataGridTextColumn { Header = "Adet", Binding = new Binding("Quantity"), Width = new DataGridLength(70) });
		_cartGrid.Columns.Add(new DataGridTextColumn { Header = "Tutar", Binding = new Binding("LineTotal") { StringFormat = "{0:N2} TL" }, Width = new DataGridLength(110) });

		cartCard.Child = _cartGrid;
		Grid.SetRow(cartCard, 1);
		leftGrid.Children.Add(cartCard);

		StackPanel cartActions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(0, 12, 0, 0)
		};

		Button btnInc = CreateButton("➕ Adet Artır", "#334155", "#FFFFFF", 13, false);
		btnInc.Padding = new Thickness(14, 8, 14, 8);
		btnInc.Click += async (s, e) =>
		{
			if (_cartGrid.SelectedItem is CartItem item)
			{
				await AddBarcodeAsync(item.Barcode);
			}
		};
		cartActions.Children.Add(btnInc);

		Button btnDec = CreateButton("➖ Adet Azalt", "#334155", "#FFFFFF", 13, false);
		btnDec.Padding = new Thickness(14, 8, 14, 8);
		btnDec.Margin = new Thickness(8, 0, 0, 0);
		btnDec.Click += (s, e) =>
		{
			if (_cartGrid.SelectedItem is CartItem item)
			{
				if (item.Quantity > 1) item.Quantity--;
				else _cart.Remove(item);
				RefreshCart();
				_barcodeInput.Focus();
			}
		};
		cartActions.Children.Add(btnDec);

		Button btnDel = CreateButton("🗑 Satırı Sil", "#991B1B", "#FFFFFF", 13, false);
		btnDel.Padding = new Thickness(14, 8, 14, 8);
		btnDel.Margin = new Thickness(8, 0, 0, 0);
		btnDel.Click += (s, e) =>
		{
			if (_cartGrid.SelectedItem is CartItem item)
			{
				_cart.Remove(item);
				RefreshCart();
				_barcodeInput.Focus();
			}
		};
		cartActions.Children.Add(btnDel);

		Button btnClear = CreateButton("❌ Sepeti Temizle", "#475569", "#FFFFFF", 13, false);
		btnClear.Padding = new Thickness(14, 8, 14, 8);
		btnClear.Margin = new Thickness(8, 0, 0, 0);
		btnClear.Click += (s, e) =>
		{
			if (_cart.Count > 0 && MessageBox.Show("Sepetteki tüm ürünler kaldırılsın mı?", "Sepeti Temizle", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				_cart.Clear();
				RefreshCart();
				_barcodeInput.Focus();
			}
		};
		cartActions.Children.Add(btnClear);

		Grid.SetRow(cartActions, 2);
		leftGrid.Children.Add(cartActions);

		Grid.SetColumn(leftGrid, 0);
		contentGrid.Children.Add(leftGrid);

		Grid rightGrid = new Grid();
		rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		Border totalCard = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(20),
			Margin = new Thickness(0, 0, 0, 16),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
			BorderThickness = new Thickness(2)
		};

		StackPanel totalStack = new StackPanel();
		TextBlock totalLbl = new TextBlock
		{
			Text = "ÖDENECEK TOPLAM TUTAR",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"))
		};
		_totalTextBlock = new TextBlock
		{
			Text = "0,00 TL",
			FontSize = 38,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
			Margin = new Thickness(0, 4, 0, 0)
		};
		_cartSummaryTextBlock = new TextBlock
		{
			Text = "0 ürün çeşidi • 0 adet",
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
			Margin = new Thickness(0, 4, 0, 0)
		};
		totalStack.Children.Add(totalLbl);
		totalStack.Children.Add(_totalTextBlock);
		totalStack.Children.Add(_cartSummaryTextBlock);
		totalCard.Child = totalStack;

		Grid.SetRow(totalCard, 0);
		rightGrid.Children.Add(totalCard);

		Border quickCard = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(16),
			Margin = new Thickness(0, 0, 0, 16),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		Grid quickGrid = new Grid();
		quickGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		quickGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		TextBlock quickTitle = new TextBlock
		{
			Text = "HIZLI / BARKODSUZ ÜRÜNLER",
			FontSize = 12,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 0, 0, 10)
		};
		Grid.SetRow(quickTitle, 0);
		quickGrid.Children.Add(quickTitle);

		ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		_quickProductsPanel = new WrapPanel();
		scroll.Content = _quickProductsPanel;
		Grid.SetRow(scroll, 1);
		quickGrid.Children.Add(scroll);

		quickCard.Child = quickGrid;
		Grid.SetRow(quickCard, 1);
		rightGrid.Children.Add(quickCard);

		StackPanel checkoutPanel = new StackPanel();

		_btnCashSale = CreateButton("💵 NAKİT SATIŞ (TAMAMLA)", "#10B981", "#FFFFFF", 16, true);
		_btnCashSale.Height = 56;
		_btnCashSale.Click += async (s, e) => await CompleteSaleAsync("Cash");
		checkoutPanel.Children.Add(_btnCashSale);

		_btnDebtSale = CreateButton("📒 VERESİYEYE YAZ (BORÇ)", "#F59E0B", "#0F172A", 16, true);
		_btnDebtSale.Height = 56;
		_btnDebtSale.Margin = new Thickness(0, 10, 0, 0);
		_btnDebtSale.Click += async (s, e) => await HandleDebtSaleAsync();
		checkoutPanel.Children.Add(_btnDebtSale);

		Grid.SetRow(checkoutPanel, 2);
		rightGrid.Children.Add(checkoutPanel);

		Grid.SetColumn(rightGrid, 2);
		contentGrid.Children.Add(rightGrid);

		Grid.SetRow(contentGrid, 1);
		mainGrid.Children.Add(contentGrid);

		Border footerBorder = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1120")),
			Padding = new Thickness(24, 10, 24, 10),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			BorderThickness = new Thickness(0, 1, 0, 0)
		};
		_statusTextBlock = new TextBlock
		{
			Text = "Yeni müşteri için hazır. Barkod okutun veya hızlı ürün seçin.",
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399"))
		};
		footerBorder.Child = _statusTextBlock;
		Grid.SetRow(footerBorder, 2);
		mainGrid.Children.Add(footerBorder);

		Content = mainGrid;
	}

	private async Task LoadQuickProductsAsync()
	{
		List<QuickProduct> list = await _quickRepo.GetQuickProductsAsync();
		_quickProductsPanel.Children.Clear();
		foreach (QuickProduct p in list)
		{
			Button btn = new Button
			{
				Width = 160,
				Height = 65,
				Margin = new Thickness(4),
				Cursor = Cursors.Hand,
				BorderThickness = new Thickness(0),
				Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(p.ColorHex))
			};

			StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
			TextBlock name = new TextBlock
			{
				Text = p.Name,
				FontWeight = FontWeights.Bold,
				FontSize = 14,
				Foreground = Brushes.White,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			TextBlock prc = new TextBlock
			{
				Text = $"{p.PriceLira:N2} TL",
				FontWeight = FontWeights.SemiBold,
				FontSize = 13,
				Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF08A")),
				HorizontalAlignment = HorizontalAlignment.Center,
				Margin = new Thickness(0, 2, 0, 0)
			};
			sp.Children.Add(name);
			sp.Children.Add(prc);
			btn.Content = sp;

			btn.Click += async (s, e) =>
			{
				if (!string.IsNullOrEmpty(p.Barcode))
				{
					await AddBarcodeAsync(p.Barcode);
				}
				_barcodeInput.Focus();
			};

			_quickProductsPanel.Children.Add(btn);
		}
	}

	private async Task SubmitBarcodeAsync()
	{
		if (_isCompleting) return;
		string text = _barcodeInput.Text.Trim();
		if (string.IsNullOrEmpty(text))
		{
			ShowStatus("Barkodu okutun veya elle yazın.", isError: true);
		}
		else
		{
			_barcodeInput.Clear();
			await AddBarcodeAsync(text);
		}
		_barcodeInput.Focus();
	}

	private async Task AddBarcodeAsync(string barcode)
	{
		_pendingScans++;
		await _scanLock.WaitAsync();
		try
		{
			SaleProduct product = await Task.Run(() => SalesRepository.FindByBarcode(barcode));
			if (product == null)
			{
				ShowStatus($"Ürün bulunamadı: {barcode}. Önce ürün kaydı yapın.", isError: true);
				return;
			}
			if (product.PriceKurus <= 0)
			{
				ShowStatus($"{product.Name}: Geçerli bir satış fiyatı tanımlayın.", isError: true);
				return;
			}
			CartItem? cartItem = _cart.FirstOrDefault(item => item.ProductId == product.Id);
			if ((cartItem?.Quantity ?? 0) >= product.Stock)
			{
				ShowStatus($"{product.Name}: Stok yetersiz. Mevcut: {product.Stock} adet.", isError: true);
				return;
			}
			if (cartItem != null)
			{
				if (cartItem.UnitPriceKurus != product.PriceKurus)
				{
					ShowStatus($"{product.Name}: Fiyat değişmiş. Sepetten kaldırıp yeniden okutun.", isError: true);
					return;
				}
				cartItem.Quantity++;
				_cartGrid.SelectedItem = cartItem;
			}
			else
			{
				CartItem newItem = new CartItem
				{
					ProductId = product.Id,
					Barcode = product.Barcode,
					Name = product.Name,
					UnitPriceKurus = product.PriceKurus,
					Quantity = 1
				};
				_cart.Add(newItem);
				_cartGrid.SelectedItem = newItem;
			}
			RefreshCart();
			ShowStatus($"{product.Name} sepete eklendi.", isError: false);
		}
		catch (Exception ex)
		{
			ShowStatus($"Ürün eklenemedi ({barcode}): {ex.Message}", isError: true);
		}
		finally
		{
			_scanLock.Release();
			_pendingScans--;
			_barcodeInput.Focus();
		}
	}

	private void RefreshCart()
	{
		_cartGrid.Items.Refresh();
		decimal total = _cart.Sum(item => item.LineTotal);
		long count = _cart.Sum(item => (long)item.Quantity);
		_totalTextBlock.Text = $"{total:N2} TL";
		_cartSummaryTextBlock.Text = $"{_cart.Count} ürün çeşidi • {count} adet";
	}

	private void ShowStatus(string message, bool isError)
	{
		_statusTextBlock.Text = message;
		_statusTextBlock.Foreground = new SolidColorBrush(isError
			? (Color)ColorConverter.ConvertFromString("#EF4444")
			: (Color)ColorConverter.ConvertFromString("#34D399"));
	}

	private async Task HandleDebtSaleAsync()
	{
		if (_cart.Count == 0)
		{
			ShowStatus("Sepet boş. Önce ürün ekleyin.", isError: true);
			return;
		}

		SelectCustomerDialog dlg = new SelectCustomerDialog { Owner = this };
		if (dlg.ShowDialog() == true && dlg.SelectedCustomer != null)
		{
			await CompleteSaleAsync("Debt", dlg.SelectedCustomer);
		}
		_barcodeInput.Focus();
	}

	private async Task CompleteSaleAsync(string paymentType, Customer? customer = null)
	{
		if (_isCompleting) return;
		if (_pendingScans > 0)
		{
			ShowStatus("Önce okutulan ürünlerin işlenmesini bekleyin.", isError: true);
			return;
		}
		if (_cart.Count == 0)
		{
			ShowStatus("Sepet boş. Önce ürün ekleyin.", isError: true);
			return;
		}

		List<CheckoutLine> lines = _cart.Select(item => new CheckoutLine(item.ProductId, item.UnitPriceKurus, item.Quantity)).ToList();
		_isCompleting = true;
		IsEnabled = false;
		ShowStatus("Satış kaydediliyor...", isError: false);

		CheckoutResult? result = null;
		try
		{
			int? customerId = customer?.Id;
			result = await Task.Run(() => CheckoutRepository.Complete(lines, paymentType, customerId));
		}
		catch (Exception ex)
		{
			ShowStatus("Satış tamamlanamadı. Sepet korunuyor.", isError: true);
			MessageBox.Show(ex.Message, "Satış Tamamlanamadı", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		finally
		{
			_isCompleting = false;
			IsEnabled = true;
		}

		if (result != null)
		{
			_cart.Clear();
			_barcodeInput.Clear();
			RefreshCart();
			decimal totalLira = result.TotalKurus / 100m;
			if (paymentType == "Debt" && customer != null)
			{
				ShowStatus($"Fiş #{result.SaleId} veresiye yazıldı: {customer.FullName} — {totalLira:N2} TL.", isError: false);
			}
			else
			{
				ShowStatus($"Satış #{result.SaleId} nakit tamamlandı — {totalLira:N2} TL. Yeni müşteri için hazır.", isError: false);
			}
		}
		_barcodeInput.Focus();
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
