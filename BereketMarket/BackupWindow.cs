using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;

namespace BereketMarket;

public class BackupWindow : Window, IComponentConnector
{
	private bool busy;

	internal Button ExportButton;

	internal TextBlock StatusTextBlock;

	internal TextBox ResultPathTextBox;

	internal Button CloseButton;

	private bool _contentLoaded;

	public BackupWindow()
	{
		InitializeComponent();
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (busy)
			{
				e.Cancel = true;
			}
		};
	}

	private async void ExportButton_Click(object sender, RoutedEventArgs e)
	{
		if (busy)
		{
			return;
		}
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "Excel dosyasının kaydedileceği yeri seç",
			Filter = "Excel dosyası (*.xlsx)|*.xlsx",
			DefaultExt = ".xlsx",
			AddExtension = true,
			CheckPathExists = true,
			OverwritePrompt = true,
			FileName = $"BereketMarket_{DateTime.Now:yyyy-MM-dd_HHmmss_fff}.xlsx"
		};
		if (saveFileDialog.ShowDialog(this) != true)
		{
			return;
		}
		string destination = saveFileDialog.FileName;
		busy = true;
		ExportButton.IsEnabled = false;
		CloseButton.IsEnabled = false;
		ExportButton.Content = "Excel hazırlanıyor...";
		ResultPathTextBox.Visibility = Visibility.Collapsed;
		SetStatus("Kayıtlar Excel dosyasına aktarılıyor...", isError: false);
		try
		{
			string text = string.Join("\n", (await Task.Run(() => ExcelExportService.Export(destination))).Select((KeyValuePair<string, int> item) => $"{item.Key}: {item.Value} kayıt"));
			SetStatus("Excel dosyası oluşturuldu.\n\n" + text, isError: false);
			ResultPathTextBox.Text = destination;
			ResultPathTextBox.Visibility = Visibility.Visible;
			MessageBox.Show("Excel dosyası kaydedildi:\n\n" + destination, "Aktarım tamamlandı", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		catch (Exception ex)
		{
			SetStatus("Excel dosyası oluşturulamadı: " + ex.Message, isError: true);
		}
		finally
		{
			busy = false;
			ExportButton.IsEnabled = true;
			CloseButton.IsEnabled = true;
			ExportButton.Content = "Excel'e kaydet";
		}
	}

	private void SetStatus(string message, bool isError)
	{
		StatusTextBlock.Text = message;
		StatusTextBlock.Foreground = new SolidColorBrush(isError ? Color.FromRgb(185, 28, 28) : Color.FromRgb(0, 122, 121));
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		if (!busy)
		{
			Close();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "8.0.2.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/BereketMarket;component/backupwindow.xaml", UriKind.Relative);
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
			ExportButton = (Button)target;
			ExportButton.Click += ExportButton_Click;
			break;
		case 2:
			StatusTextBlock = (TextBlock)target;
			break;
		case 3:
			ResultPathTextBox = (TextBox)target;
			break;
		case 4:
			CloseButton = (Button)target;
			CloseButton.Click += CloseButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
