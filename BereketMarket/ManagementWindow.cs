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

public class ManagementWindow : Window, IComponentConnector
{
	private bool dashboardLoading;

	private bool windowClosed;

	internal Button RefreshDashboardButton;

	internal TabControl ManagementTabs;

	internal TextBlock TodayTotalTextBlock;

	internal TextBlock MonthTotalTextBlock;

	internal TextBlock TodayCountTextBlock;

	internal TextBlock LowStockCountTextBlock;

	internal DataGrid DailySalesGrid;

	internal TextBlock LowStockHintTextBlock;

	internal DataGrid LowStockGrid;

	internal DataGrid TopProductsGrid;

	internal TextBlock DashboardStatusTextBlock;

	private bool _contentLoaded;

	public ManagementWindow()
	{
		AdminSecurity.RequireAdmin();
		InitializeComponent();
		LowStockHintTextBlock.Text = $"Aktif ürünlerde {5}" + " adet ve altı. Stoku sıfır olanlar kırmızı gösterilir.";
		Loaded += async (object _, RoutedEventArgs _) =>
		{
			await RefreshDashboardAsync();
		};
		Closed += (object? _, EventArgs _) =>
		{
			windowClosed = true;
			AdminSecurity.Logout();
		};
	}

	private async Task RefreshDashboardAsync()
	{
		if (dashboardLoading || windowClosed)
		{
			return;
		}
		dashboardLoading = true;
		RefreshDashboardButton.IsEnabled = false;
		ManagementTabs.IsEnabled = false;
		DashboardStatusTextBlock.Text = "Özet hazırlanıyor...";
		try
		{
			DashboardData dashboardData = await Task.Run((Func<DashboardData>)DashboardRepository.Load);
			if (!windowClosed)
			{
				CultureInfo cultureInfo = CultureInfo.GetCultureInfo("tr-TR");
				TodayTotalTextBlock.Text = dashboardData.TodayTotal.ToString("N2", cultureInfo) + " TL";
				MonthTotalTextBlock.Text = dashboardData.MonthTotal.ToString("N2", cultureInfo) + " TL";
				TodayCountTextBlock.Text = dashboardData.TodayTransactionCount.ToString(cultureInfo);
				LowStockCountTextBlock.Text = dashboardData.LowStockProducts.Count.ToString(cultureInfo);
				DailySalesGrid.ItemsSource = dashboardData.DailySales;
				LowStockGrid.ItemsSource = dashboardData.LowStockProducts;
				TopProductsGrid.ItemsSource = dashboardData.TopProducts;
				DashboardStatusTextBlock.Text = "Son yenileme: " + dashboardData.LoadedAt.ToString("dd.MM.yyyy HH:mm:ss");
			}
		}
		catch (Exception ex)
		{
			if (!windowClosed)
			{
				TodayTotalTextBlock.Text = "—";
				MonthTotalTextBlock.Text = "—";
				TodayCountTextBlock.Text = "—";
				LowStockCountTextBlock.Text = "—";
				DailySalesGrid.ItemsSource = null;
				LowStockGrid.ItemsSource = null;
				TopProductsGrid.ItemsSource = null;
				DashboardStatusTextBlock.Text = "Özet yüklenemedi. Yeniden deneyebilirsin.";
				MessageBox.Show(ex.Message, "Yönetim özeti", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}
		finally
		{
			dashboardLoading = false;
			if (!windowClosed)
			{
				RefreshDashboardButton.IsEnabled = true;
				ManagementTabs.IsEnabled = true;
			}
		}
	}

	private async void RefreshDashboardButton_Click(object sender, RoutedEventArgs e)
	{
		await RefreshDashboardAsync();
	}

	private async void NewProductButton_Click(object sender, RoutedEventArgs e)
	{
		AdminSecurity.RequireAdmin();
		MainWindow mainWindow = new MainWindow();
		mainWindow.Owner = this;
		mainWindow.ShowDialog();
		await RefreshDashboardAsync();
	}

	private async void ProductsButton_Click(object sender, RoutedEventArgs e)
	{
		AdminSecurity.RequireAdmin();
		ProductListWindow productListWindow = new ProductListWindow();
		productListWindow.Owner = this;
		productListWindow.ShowDialog();
		await RefreshDashboardAsync();
	}

	private async void SalesHistoryButton_Click(object sender, RoutedEventArgs e)
	{
		AdminSecurity.RequireAdmin();
		SalesHistoryWindow salesHistoryWindow = new SalesHistoryWindow();
		salesHistoryWindow.Owner = this;
		salesHistoryWindow.ShowDialog();
		await RefreshDashboardAsync();
	}

	private void BackupButton_Click(object sender, RoutedEventArgs e)
	{
		BackupWindow backupWindow = new BackupWindow();
		backupWindow.Owner = this;
		backupWindow.ShowDialog();
	}

	private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
	{
		AdminSecurity.RequireAdmin();
		if (new AdminPasswordWindow(changePassword: true)
		{
			Owner = this
		}.ShowDialog() == true)
		{
			MessageBox.Show("Yönetici şifresi değiştirildi. Sonraki girişte yeni şifreni kullan.", "Şifre güncellendi", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
	}

	private void BackButton_Click(object sender, RoutedEventArgs e)
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
			Uri resourceLocator = new Uri("/BereketMarket;component/managementwindow.xaml", UriKind.Relative);
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
			RefreshDashboardButton = (Button)target;
			RefreshDashboardButton.Click += RefreshDashboardButton_Click;
			break;
		case 2:
			ManagementTabs = (TabControl)target;
			break;
		case 3:
			TodayTotalTextBlock = (TextBlock)target;
			break;
		case 4:
			MonthTotalTextBlock = (TextBlock)target;
			break;
		case 5:
			TodayCountTextBlock = (TextBlock)target;
			break;
		case 6:
			LowStockCountTextBlock = (TextBlock)target;
			break;
		case 7:
			DailySalesGrid = (DataGrid)target;
			break;
		case 8:
			LowStockHintTextBlock = (TextBlock)target;
			break;
		case 9:
			LowStockGrid = (DataGrid)target;
			break;
		case 10:
			TopProductsGrid = (DataGrid)target;
			break;
		case 11:
			((Button)target).Click += NewProductButton_Click;
			break;
		case 12:
			((Button)target).Click += ProductsButton_Click;
			break;
		case 13:
			((Button)target).Click += SalesHistoryButton_Click;
			break;
		case 14:
			((Button)target).Click += BackupButton_Click;
			break;
		case 15:
			((Button)target).Click += ChangePasswordButton_Click;
			break;
		case 16:
			DashboardStatusTextBlock = (TextBlock)target;
			break;
		case 17:
			((Button)target).Click += BackButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
