using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace BereketMarket;

public class DebtBookWindow : Window
{
	private readonly CustomerRepository _repository = new CustomerRepository();
	private TextBox _searchBox = null!;
	private ListBox _customerList = null!;
	private TextBlock _selectedCustomerTitle = null!;
	private TextBlock _selectedCustomerPhone = null!;
	private TextBlock _selectedCustomerBalance = null!;
	private DataGrid _transactionsGrid = null!;
	private Button _btnPayment = null!;
	private Button _btnManualDebt = null!;
	private Customer? _currentCustomer;

	public DebtBookWindow(Customer? preselectedCustomer = null)
	{
		Title = "Bereket Market — Veresiye Defteri";
		Width = 1100;
		Height = 720;
		MinWidth = 950;
		MinHeight = 600;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		FontFamily = new FontFamily("Segoe UI");
		FontSize = 14;

		_currentCustomer = preselectedCustomer;
		BuildUi();
		Loaded += async (s, e) =>
		{
			await LoadCustomersAsync();
			if (_currentCustomer != null)
			{
				SelectCustomerById(_currentCustomer.Id);
			}
		};
	}

	private void BuildUi()
	{
		Grid mainGrid = new Grid();
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		Border headerBorder = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Padding = new Thickness(24, 18, 24, 18),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(0, 0, 0, 1)
		};

		Grid headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel titlePanel = new StackPanel();
		TextBlock headerTitle = new TextBlock
		{
			Text = "VERESİYE DEFTERİ",
			FontSize = 24,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
		};
		TextBlock headerSub = new TextBlock
		{
			Text = "Köy veresiye ve müşteri bakiye takip sistemi",
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 4, 0, 0)
		};
		titlePanel.Children.Add(headerTitle);
		titlePanel.Children.Add(headerSub);
		Grid.SetColumn(titlePanel, 0);
		headerGrid.Children.Add(titlePanel);

		Button btnNewCustomer = CreateStyledButton("+ Yeni Müşteri Kaydet", "#0D9488", "#FFFFFF", 14, true);
		btnNewCustomer.Padding = new Thickness(18, 10, 18, 10);
		btnNewCustomer.Click += BtnNewCustomer_Click;
		Grid.SetColumn(btnNewCustomer, 1);
		headerGrid.Children.Add(btnNewCustomer);

		headerBorder.Child = headerGrid;
		Grid.SetRow(headerBorder, 0);
		mainGrid.Children.Add(headerBorder);

		Grid contentGrid = new Grid { Margin = new Thickness(20) };
		contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
		contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
		contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		Border leftCard = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(16),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		Grid leftGrid = new Grid();
		leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		_searchBox = new TextBox
		{
			FontSize = 14,
			Padding = new Thickness(12, 10, 12, 10),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
			BorderThickness = new Thickness(1),
			Margin = new Thickness(0, 0, 0, 12)
		};
		_searchBox.TextChanged += async (s, e) => await LoadCustomersAsync(_searchBox.Text);
		Grid.SetRow(_searchBox, 0);
		leftGrid.Children.Add(_searchBox);

		_customerList = new ListBox
		{
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0),
			Foreground = Brushes.White,
			ItemContainerStyle = CreateListBoxItemStyle()
		};
		_customerList.SelectionChanged += CustomerList_SelectionChanged;
		Grid.SetRow(_customerList, 1);
		leftGrid.Children.Add(_customerList);

		leftCard.Child = leftGrid;
		Grid.SetColumn(leftCard, 0);
		contentGrid.Children.Add(leftCard);

		Border rightCard = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(20),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		Grid rightGrid = new Grid();
		rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		Border customerBanner = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(18, 14, 18, 14),
			Margin = new Thickness(0, 0, 0, 16),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			BorderThickness = new Thickness(1)
		};

		Grid bannerGrid = new Grid();
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel infoStack = new StackPanel();
		_selectedCustomerTitle = new TextBlock
		{
			Text = "Lütfen listeden bir müşteri seçin",
			FontSize = 20,
			FontWeight = FontWeights.Bold,
			Foreground = Brushes.White
		};
		_selectedCustomerPhone = new TextBlock
		{
			Text = "Telefon: -",
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 4, 0, 0)
		};
		infoStack.Children.Add(_selectedCustomerTitle);
		infoStack.Children.Add(_selectedCustomerPhone);
		Grid.SetColumn(infoStack, 0);
		bannerGrid.Children.Add(infoStack);

		StackPanel balanceStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
		TextBlock balanceLabel = new TextBlock
		{
			Text = "GÜNCEL TOPLAM BORÇ",
			FontSize = 11,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		_selectedCustomerBalance = new TextBlock
		{
			Text = "0,00 TL",
			FontSize = 26,
			FontWeight = FontWeights.ExtraBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
			HorizontalAlignment = HorizontalAlignment.Right
		};
		balanceStack.Children.Add(balanceLabel);
		balanceStack.Children.Add(_selectedCustomerBalance);
		Grid.SetColumn(balanceStack, 1);
		bannerGrid.Children.Add(balanceStack);

		customerBanner.Child = bannerGrid;
		Grid.SetRow(customerBanner, 0);
		rightGrid.Children.Add(customerBanner);

		StackPanel actionsBar = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(0, 0, 0, 16)
		};

		_btnPayment = CreateStyledButton("💵 Tahsilat Al (Borç Düş)", "#10B981", "#FFFFFF", 14, true);
		_btnPayment.Padding = new Thickness(18, 10, 18, 10);
		_btnPayment.IsEnabled = false;
		_btnPayment.Click += BtnPayment_Click;
		actionsBar.Children.Add(_btnPayment);

		_btnManualDebt = CreateStyledButton("📝 Manuel Borç Yaz", "#F59E0B", "#1E293B", 14, true);
		_btnManualDebt.Padding = new Thickness(18, 10, 18, 10);
		_btnManualDebt.Margin = new Thickness(12, 0, 0, 0);
		_btnManualDebt.IsEnabled = false;
		_btnManualDebt.Click += BtnManualDebt_Click;
		actionsBar.Children.Add(_btnManualDebt);

		Grid.SetRow(actionsBar, 1);
		rightGrid.Children.Add(actionsBar);

		_transactionsGrid = new DataGrid
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
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
			RowBackground = Brushes.Transparent,
			AlternatingRowBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#152238"))
		};

		DataGridTextColumn colDate = new DataGridTextColumn
		{
			Header = "Tarih & Saat",
			Binding = new Binding("CreatedAt") { StringFormat = "dd.MM.yyyy HH:mm" },
			Width = new DataGridLength(150)
		};
		DataGridTextColumn colType = new DataGridTextColumn
		{
			Header = "İşlem",
			Binding = new Binding("DisplayType"),
			Width = new DataGridLength(150)
		};
		DataGridTextColumn colDesc = new DataGridTextColumn
		{
			Header = "Açıklama / Kalem Detayları",
			Binding = new Binding("Description"),
			Width = new DataGridLength(1, DataGridLengthUnitType.Star)
		};
		DataGridTextColumn colAmount = new DataGridTextColumn
		{
			Header = "Tutar",
			Binding = new Binding("AmountLira") { StringFormat = "{0:N2} TL" },
			Width = new DataGridLength(120)
		};

		_transactionsGrid.Columns.Add(colDate);
		_transactionsGrid.Columns.Add(colType);
		_transactionsGrid.Columns.Add(colDesc);
		_transactionsGrid.Columns.Add(colAmount);

		Grid.SetRow(_transactionsGrid, 2);
		rightGrid.Children.Add(_transactionsGrid);

		rightCard.Child = rightGrid;
		Grid.SetColumn(rightCard, 2);
		contentGrid.Children.Add(rightCard);

		Grid.SetRow(contentGrid, 1);
		mainGrid.Children.Add(contentGrid);

		Content = mainGrid;
	}

	private async System.Threading.Tasks.Task LoadCustomersAsync(string? search = null)
	{
		List<Customer> list = await _repository.GetAllCustomersAsync(search);
		_customerList.Items.Clear();
		foreach (Customer c in list)
		{
			_customerList.Items.Add(CreateCustomerListItem(c));
		}
	}

	private ListBoxItem CreateCustomerListItem(Customer customer)
	{
		ListBoxItem item = new ListBoxItem { Tag = customer, Padding = new Thickness(10) };

		Grid g = new Grid();
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel sp = new StackPanel();
		TextBlock nameText = new TextBlock
		{
			Text = customer.FullName,
			FontWeight = FontWeights.SemiBold,
			FontSize = 15,
			Foreground = Brushes.White
		};
		TextBlock phoneText = new TextBlock
		{
			Text = string.IsNullOrEmpty(customer.Phone) ? "Tel: Yok" : customer.Phone,
			FontSize = 12,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
			Margin = new Thickness(0, 3, 0, 0)
		};
		sp.Children.Add(nameText);
		sp.Children.Add(phoneText);
		Grid.SetColumn(sp, 0);
		g.Children.Add(sp);

		Border badge = new Border
		{
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(8, 4, 8, 4),
			VerticalAlignment = VerticalAlignment.Center,
			Background = customer.BalanceLira > 0
				? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"))
				: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064E3B"))
		};
		TextBlock badgeText = new TextBlock
		{
			Text = $"{customer.BalanceLira:N2} TL",
			FontWeight = FontWeights.Bold,
			FontSize = 13,
			Foreground = customer.BalanceLira > 0
				? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"))
				: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7"))
		};
		badge.Child = badgeText;
		Grid.SetColumn(badge, 1);
		g.Children.Add(badge);

		item.Content = g;
		return item;
	}

	private async void CustomerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_customerList.SelectedItem is ListBoxItem item && item.Tag is Customer customer)
		{
			_currentCustomer = customer;
			_selectedCustomerTitle.Text = customer.FullName;
			_selectedCustomerPhone.Text = $"Telefon: {(string.IsNullOrEmpty(customer.Phone) ? "Kayıtsız" : customer.Phone)} | Kayıt: {customer.CreatedAt:dd.MM.yyyy}";
			_selectedCustomerBalance.Text = $"{customer.BalanceLira:N2} TL";
			_selectedCustomerBalance.Foreground = customer.BalanceLira > 0
				? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"))
				: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));

			_btnPayment.IsEnabled = true;
			_btnManualDebt.IsEnabled = true;

			await LoadTransactionsAsync(customer.Id);
		}
		else
		{
			_currentCustomer = null;
			_selectedCustomerTitle.Text = "Lütfen listeden bir müşteri seçin";
			_selectedCustomerPhone.Text = "Telefon: -";
			_selectedCustomerBalance.Text = "0,00 TL";
			_btnPayment.IsEnabled = false;
			_btnManualDebt.IsEnabled = false;
			_transactionsGrid.ItemsSource = null;
		}
	}

	private async System.Threading.Tasks.Task LoadTransactionsAsync(int customerId)
	{
		List<CustomerTransaction> list = await _repository.GetTransactionsAsync(customerId);
		foreach (CustomerTransaction tx in list)
		{
			if (tx.SaleId.HasValue)
			{
				List<string> lines = await _repository.GetSaleItemLinesAsync(tx.SaleId.Value);
				if (lines.Count > 0)
				{
					tx.Description = $"{tx.Description} [ {string.Join(", ", lines)} ]";
				}
			}
		}
		_transactionsGrid.ItemsSource = list;
	}

	private async void BtnNewCustomer_Click(object sender, RoutedEventArgs e)
	{
		SimplePromptDialog dlg = new SimplePromptDialog("Yeni Müşteri Ekle", "Müşteri Adı Soyadı:", "Telefon Numarası (İsteğe bağlı):");
		dlg.Owner = this;
		if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.InputText1))
		{
			int id = await _repository.AddCustomerAsync(dlg.InputText1, dlg.InputText2, null);
			await LoadCustomersAsync();
			SelectCustomerById(id);
		}
	}

	private async void BtnPayment_Click(object sender, RoutedEventArgs e)
	{
		if (_currentCustomer == null) return;
		NumberPromptDialog dlg = new NumberPromptDialog("Tahsilat Al", $"{_currentCustomer.FullName} için ödenecek nakit tutar (TL):", _currentCustomer.BalanceLira);
		dlg.Owner = this;
		if (dlg.ShowDialog() == true && dlg.AmountLira > 0)
		{
			int kurus = (int)Math.Round(dlg.AmountLira * 100);
			await _repository.AddPaymentAsync(_currentCustomer.Id, kurus, dlg.DescriptionText);
			await LoadCustomersAsync();
			SelectCustomerById(_currentCustomer.Id);
		}
	}

	private async void BtnManualDebt_Click(object sender, RoutedEventArgs e)
	{
		if (_currentCustomer == null) return;
		NumberPromptDialog dlg = new NumberPromptDialog("Borç Yaz", $"{_currentCustomer.FullName} hesabına yazılacak borç tutarı (TL):", 0);
		dlg.Owner = this;
		if (dlg.ShowDialog() == true && dlg.AmountLira > 0)
		{
			int kurus = (int)Math.Round(dlg.AmountLira * 100);
			await _repository.AddManualDebtAsync(_currentCustomer.Id, kurus, dlg.DescriptionText);
			await LoadCustomersAsync();
			SelectCustomerById(_currentCustomer.Id);
		}
	}

	private void SelectCustomerById(int customerId)
	{
		foreach (ListBoxItem item in _customerList.Items)
		{
			if (item.Tag is Customer c && c.Id == customerId)
			{
				_customerList.SelectedItem = item;
				_customerList.ScrollIntoView(item);
				break;
			}
		}
	}

	private Button CreateStyledButton(string content, string bgHex, string fgHex, double fontSize, bool bold)
	{
		Button btn = new Button
		{
			Content = content,
			FontSize = fontSize,
			FontWeight = bold ? FontWeights.Bold : FontWeights.SemiBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
			Cursor = Cursors.Hand,
			BorderThickness = new Thickness(0)
		};

		ControlTemplate template = new ControlTemplate(typeof(Button));
		FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
		borderFactory.Name = "border";
		borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
		borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
		borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

		FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
		presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
		presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
		borderFactory.AppendChild(presenter);

		template.VisualTree = borderFactory;

		Trigger mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		mouseOverTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.88));
		template.Triggers.Add(mouseOverTrigger);

		Trigger pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
		pressedTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.72));
		template.Triggers.Add(pressedTrigger);

		btn.Template = template;
		return btn;
	}

	private Style CreateListBoxItemStyle()
	{
		Style style = new Style(typeof(ListBoxItem));
		ControlTemplate template = new ControlTemplate(typeof(ListBoxItem));
		FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
		border.Name = "Bd";
		border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
		border.SetValue(Border.MarginProperty, new Thickness(0, 3, 0, 3));
		border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
		border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
		border.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")));

		FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
		border.AppendChild(presenter);
		template.VisualTree = border;

		Trigger isSelectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
		isSelectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E3A8A")), "Bd"));
		isSelectedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")), "Bd"));
		template.Triggers.Add(isSelectedTrigger);

		Trigger isMouseOverTrigger = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
		isMouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#273549")), "Bd"));
		template.Triggers.Add(isMouseOverTrigger);

		style.Setters.Add(new Setter(ListBoxItem.TemplateProperty, template));
		return style;
	}
}
