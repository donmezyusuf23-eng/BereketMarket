using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Data.Sqlite;

namespace BereketMarket;

public class ProductEditWindow : Window, IComponentConnector
{
	private readonly long productId;

	private readonly string oldBarcode;

	private readonly string oldName;

	private bool isSaving;

	internal TextBox BarcodeTextBox;

	internal TextBox NameTextBox;

	internal TextBlock StatusTextBlock;

	internal Button SaveButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public ProductEditWindow(ProductListItem product)
	{
		InitializeComponent();
		productId = product.Id;
		oldBarcode = product.Barcode;
		oldName = product.Name;
		BarcodeTextBox.Text = oldBarcode;
		NameTextBox.Text = oldName;
		Loaded += (object _, RoutedEventArgs _) =>
		{
			NameTextBox.Focus();
			NameTextBox.SelectAll();
		};
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (isSaving)
			{
				e.Cancel = true;
			}
		};
	}

	private void BarcodeTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			e.Handled = true;
			NameTextBox.Focus();
		}
	}

	private async void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (isSaving)
		{
			return;
		}
		string newBarcode = BarcodeTextBox.Text.Trim();
		string newName = NameTextBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(newBarcode))
		{
			StatusTextBlock.Text = "Barkod boş olamaz.";
			BarcodeTextBox.Focus();
			return;
		}
		if (string.IsNullOrWhiteSpace(newName))
		{
			StatusTextBlock.Text = "Ürün adı boş olamaz.";
			NameTextBox.Focus();
			return;
		}
		if (newBarcode == oldBarcode && newName == oldName)
		{
			StatusTextBlock.Text = "Henüz bir değişiklik yapmadın.";
			return;
		}
		isSaving = true;
		SaveButton.IsEnabled = false;
		CancelButton.IsEnabled = false;
		BarcodeTextBox.IsEnabled = false;
		NameTextBox.IsEnabled = false;
		StatusTextBlock.Text = "";
		SaveButton.Content = "Kaydediliyor...";
		bool succeeded = false;
		try
		{
			await Task.Run(() =>
			{
				ProductEditRepository.Update(productId, oldBarcode, oldName, newBarcode, newName);
			});
			succeeded = true;
		}
		catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
		{
			StatusTextBlock.Text = "Bu barkod başka bir üründe kullanılıyor. Değişiklikler kaydedilmedi.";
		}
		catch (Exception ex2)
		{
			StatusTextBlock.Text = "Değişiklikler kaydedilemedi: " + ex2.Message;
		}
		finally
		{
			isSaving = false;
			SaveButton.IsEnabled = true;
			CancelButton.IsEnabled = true;
			BarcodeTextBox.IsEnabled = true;
			NameTextBox.IsEnabled = true;
			SaveButton.Content = "Değişiklikleri kaydet";
		}
		if (succeeded)
		{
			DialogResult = true;
		}
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
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
			Uri resourceLocator = new Uri("/BereketMarket;component/producteditwindow.xaml", UriKind.Relative);
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
			BarcodeTextBox = (TextBox)target;
			BarcodeTextBox.PreviewKeyDown += BarcodeTextBox_PreviewKeyDown;
			break;
		case 2:
			NameTextBox = (TextBox)target;
			break;
		case 3:
			StatusTextBlock = (TextBlock)target;
			break;
		case 4:
			SaveButton = (Button)target;
			SaveButton.Click += SaveButton_Click;
			break;
		case 5:
			CancelButton = (Button)target;
			CancelButton.Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
