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

public class SaleCancellationWindow : Window, IComponentConnector
{
	private readonly long saleId;

	private bool isSaving;

	internal TextBlock SaleInfoTextBlock;

	internal TextBox ReasonTextBox;

	internal TextBlock StatusTextBlock;

	internal Button ConfirmButton;

	internal Button BackButton;

	private bool _contentLoaded;

	public SaleCancellationWindow(SaleHistoryItem sale)
	{
		InitializeComponent();
		saleId = sale.Id;
		string value = sale.Total.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"));
		SaleInfoTextBlock.Text = $"Satış #{sale.Id} • {sale.DateDisplay} • {value} TL";
		Loaded += (object _, RoutedEventArgs _) =>
		{
			ReasonTextBox.Focus();
		};
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (isSaving)
			{
				e.Cancel = true;
			}
		};
	}

	private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
	{
		if (isSaving)
		{
			return;
		}
		string reason = ReasonTextBox.Text.Trim();
		if (reason.Length < 3)
		{
			StatusTextBlock.Text = "En az 3 karakterlik bir iptal gerekçesi yaz.";
			ReasonTextBox.Focus();
			return;
		}
		isSaving = true;
		ConfirmButton.IsEnabled = false;
		BackButton.IsEnabled = false;
		ReasonTextBox.IsEnabled = false;
		StatusTextBlock.Text = "İptal işlemi uygulanıyor...";
		bool succeeded = false;
		try
		{
			await Task.Run(() =>
			{
				SaleCancellationRepository.Cancel(saleId, reason);
			});
			succeeded = true;
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = ex.Message;
		}
		finally
		{
			isSaving = false;
			ConfirmButton.IsEnabled = true;
			BackButton.IsEnabled = true;
			ReasonTextBox.IsEnabled = true;
		}
		if (succeeded)
		{
			DialogResult = true;
		}
	}

	private void BackButton_Click(object sender, RoutedEventArgs e)
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
			Uri resourceLocator = new Uri("/BereketMarket;component/salecancellationwindow.xaml", UriKind.Relative);
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
			SaleInfoTextBlock = (TextBlock)target;
			break;
		case 2:
			ReasonTextBox = (TextBox)target;
			break;
		case 3:
			StatusTextBlock = (TextBlock)target;
			break;
		case 4:
			ConfirmButton = (Button)target;
			ConfirmButton.Click += ConfirmButton_Click;
			break;
		case 5:
			BackButton = (Button)target;
			BackButton.Click += BackButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
