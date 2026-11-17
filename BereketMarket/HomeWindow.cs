using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BereketMarket;

public class HomeWindow : Window
{
	public HomeWindow()
	{
		Title = "Bereket Market — Ana Menü";
		Width = 1080;
		Height = 720;
		MinWidth = 900;
		MinHeight = 600;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0B1120"));
		FontFamily = new FontFamily("Segoe UI");
		FontSize = 14;

		BuildUi();

		Closed += (s, e) => AdminSecurity.Logout();
	}

	private void BuildUi()
	{
		Grid mainGrid = new Grid();
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		Border headerBorder = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
			Padding = new Thickness(36, 24, 36, 24),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			BorderThickness = new Thickness(0, 0, 0, 1)
		};

		Grid headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		StackPanel titleStack = new StackPanel();
		TextBlock title = new TextBlock
		{
			Text = "BEREKET MARKET",
			FontSize = 32,
			FontWeight = FontWeights.Black,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"))
		};
		TextBlock subtitle = new TextBlock
		{
			Text = "Hızlı Kasa • Veresiye Defteri • Günlük Satış Takibi",
			FontSize = 14,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")),
			Margin = new Thickness(0, 4, 0, 0),
			FontWeight = FontWeights.SemiBold
		};
		titleStack.Children.Add(title);
		titleStack.Children.Add(subtitle);
		Grid.SetColumn(titleStack, 0);
		headerGrid.Children.Add(titleStack);

		Border statusBadge = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#064E3B")),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 8, 16, 8),
			VerticalAlignment = VerticalAlignment.Center,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
			BorderThickness = new Thickness(1)
		};
		statusBadge.Child = new TextBlock
		{
			Text = "🟢 SİSTEM ÇALIŞIYOR",
			FontWeight = FontWeights.Bold,
			FontSize = 12,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6EE7B7"))
		};
		Grid.SetColumn(statusBadge, 1);
		headerGrid.Children.Add(statusBadge);

		headerBorder.Child = headerGrid;
		Grid.SetRow(headerBorder, 0);
		mainGrid.Children.Add(headerBorder);

		Grid menuGrid = new Grid { Margin = new Thickness(36, 24, 36, 24) };
		menuGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		menuGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
		menuGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		menuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		menuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
		menuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		Button cardSales = CreateMenuCard(
			"🛒 HIZLI KASA & SATIŞ",
			"Barkod okutma, hızlı ürün butonları, nakit veya veresiye satış",
			"#059669", "#10B981");
		cardSales.Click += (s, e) =>
		{
			SalesWindow w = new SalesWindow { Owner = this };
			w.ShowDialog();
		};
		Grid.SetRow(cardSales, 0);
		Grid.SetColumn(cardSales, 0);
		menuGrid.Children.Add(cardSales);

		Button cardDebt = CreateMenuCard(
			"📒 VERESİYE DEFTERİ",
			"Müşteri hesapları, borç takibi, tahsilat alma ve hesap ekstresi",
			"#1D4ED8", "#3B82F6");
		cardDebt.Click += (s, e) =>
		{
			DebtBookWindow w = new DebtBookWindow { Owner = this };
			w.ShowDialog();
		};
		Grid.SetRow(cardDebt, 0);
		Grid.SetColumn(cardDebt, 2);
		menuGrid.Children.Add(cardDebt);

		Button cardRegister = CreateMenuCard(
			"📖 GÜNLÜK KASA DEFTERİ",
			"Bugün satılan tüm ürünler, anlık nakit ciro ve kâr hesabı",
			"#D97706", "#F59E0B");
		cardRegister.Click += (s, e) =>
		{
			CashRegisterBookWindow w = new CashRegisterBookWindow { Owner = this };
			w.ShowDialog();
		};
		Grid.SetRow(cardRegister, 2);
		Grid.SetColumn(cardRegister, 0);
		menuGrid.Children.Add(cardRegister);

		Button cardProducts = CreateMenuCard(
			"📦 ÜRÜNLER & STOKLAR",
			"Ürün ekleme, alış/satış fiyatı düzenleme ve kritik stok takibi",
			"#6D28D9", "#8B5CF6");
		cardProducts.Click += (s, e) =>
		{
			ProductListWindow w = new ProductListWindow { Owner = this };
			w.ShowDialog();
		};
		Grid.SetRow(cardProducts, 2);
		Grid.SetColumn(cardProducts, 2);
		menuGrid.Children.Add(cardProducts);

		Grid.SetRow(menuGrid, 1);
		mainGrid.Children.Add(menuGrid);

		Border footerBorder = new Border
		{
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
			Padding = new Thickness(36, 14, 36, 14),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			BorderThickness = new Thickness(0, 1, 0, 0)
		};

		Grid footerGrid = new Grid();
		footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		Button btnAdmin = CreateSmallButton("⚙️ Yönetici & Yedekleme", "#334155", "#FFFFFF");
		btnAdmin.Click += (s, e) =>
		{
			AdminSecurity.Logout();
			try
			{
				if (new AdminPasswordWindow { Owner = this }.ShowDialog() == true)
				{
					ManagementWindow m = new ManagementWindow { Owner = this };
					m.ShowDialog();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Yönetici Girişi", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			finally
			{
				AdminSecurity.Logout();
			}
		};
		Grid.SetColumn(btnAdmin, 0);
		footerGrid.Children.Add(btnAdmin);

		Button btnExit = CreateSmallButton("❌ Programı Kapat", "#991B1B", "#FFFFFF");
		btnExit.Click += (s, e) => Close();
		Grid.SetColumn(btnExit, 2);
		footerGrid.Children.Add(btnExit);

		footerBorder.Child = footerGrid;
		Grid.SetRow(footerBorder, 2);
		mainGrid.Children.Add(footerBorder);

		Content = mainGrid;
	}

	private Button CreateMenuCard(string title, string description, string bgDark, string bgLight)
	{
		Button btn = new Button
		{
			Cursor = Cursors.Hand,
			BorderThickness = new Thickness(0),
			Background = Brushes.Transparent
		};

		LinearGradientBrush grad = new LinearGradientBrush
		{
			StartPoint = new Point(0, 0),
			EndPoint = new Point(1, 1)
		};
		grad.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(bgDark), 0));
		grad.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(bgLight), 1));

		Border card = new Border
		{
			Background = grad,
			CornerRadius = new CornerRadius(16),
			Padding = new Thickness(24)
		};

		StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
		TextBlock t = new TextBlock
		{
			Text = title,
			FontSize = 20,
			FontWeight = FontWeights.ExtraBold,
			Foreground = Brushes.White
		};
		TextBlock d = new TextBlock
		{
			Text = description,
			FontSize = 13,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0, 8, 0, 0),
			Opacity = 0.95
		};
		sp.Children.Add(t);
		sp.Children.Add(d);
		card.Child = sp;

		ControlTemplate template = new ControlTemplate(typeof(Button));
		FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
		borderFactory.Name = "Bd";
		borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
		borderFactory.SetValue(Border.BackgroundProperty, grad);

		FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
		borderFactory.AppendChild(presenter);
		template.VisualTree = borderFactory;

		Trigger mOver = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
		mOver.Setters.Add(new Setter(UIElement.OpacityProperty, 0.90));
		template.Triggers.Add(mOver);

		Trigger pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
		pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.75));
		template.Triggers.Add(pressed);

		btn.Template = template;
		btn.Content = card;
		return btn;
	}

	private Button CreateSmallButton(string text, string bgHex, string fgHex)
	{
		Button btn = new Button
		{
			Content = text,
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
			Cursor = Cursors.Hand,
			Padding = new Thickness(16, 8, 16, 8),
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
