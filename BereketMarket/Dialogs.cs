using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BereketMarket;

public class SimplePromptDialog : Window
{
	public string InputText1 => _box1.Text;
	public string InputText2 => _box2.Text;

	private TextBox _box1 = null!;
	private TextBox _box2 = null!;

	public SimplePromptDialog(string title, string label1, string label2)
	{
		Title = title;
		Width = 440;
		Height = 290;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		Foreground = Brushes.White;
		ResizeMode = ResizeMode.NoResize;
		FontFamily = new FontFamily("Segoe UI");

		StackPanel root = new StackPanel { Margin = new Thickness(24) };

		TextBlock l1 = new TextBlock { Text = label1, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Margin = new Thickness(0, 0, 0, 6) };
		_box1 = new TextBox { FontSize = 15, Padding = new Thickness(10, 8, 10, 8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), Margin = new Thickness(0, 0, 0, 16) };

		TextBlock l2 = new TextBlock { Text = label2, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Margin = new Thickness(0, 0, 0, 6) };
		_box2 = new TextBox { FontSize = 15, Padding = new Thickness(10, 8, 10, 8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")), Foreground = Brushes.White, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), Margin = new Thickness(0, 0, 0, 20) };

		StackPanel btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

		Button btnCancel = new Button { Content = "İptal", Width = 90, Height = 36, Margin = new Thickness(0, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
		btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

		Button btnOk = new Button { Content = "Kaydet", Width = 100, Height = 36, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D9488")), Foreground = Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0) };
		btnOk.Click += (s, e) => { DialogResult = true; Close(); };

		btnPanel.Children.Add(btnCancel);
		btnPanel.Children.Add(btnOk);

		root.Children.Add(l1);
		root.Children.Add(_box1);
		root.Children.Add(l2);
		root.Children.Add(_box2);
		root.Children.Add(btnPanel);

		Content = root;
		Loaded += (s, e) => _box1.Focus();
	}
}

public class NumberPromptDialog : Window
{
	public decimal AmountLira { get; private set; }
	public string DescriptionText => _descBox.Text;

	private TextBox _amountBox = null!;
	private TextBox _descBox = null!;

	public NumberPromptDialog(string title, string label, decimal defaultAmount)
	{
		Title = title;
		Width = 440;
		Height = 300;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		Foreground = Brushes.White;
		ResizeMode = ResizeMode.NoResize;
		FontFamily = new FontFamily("Segoe UI");

		StackPanel root = new StackPanel { Margin = new Thickness(24) };

		TextBlock l1 = new TextBlock { Text = label, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Margin = new Thickness(0, 0, 0, 6) };
		_amountBox = new TextBox
		{
			Text = defaultAmount > 0 ? defaultAmount.ToString("0.00", CultureInfo.InvariantCulture) : "",
			FontSize = 18,
			FontWeight = FontWeights.Bold,
			Padding = new Thickness(10, 8, 10, 8),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#38BDF8")),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			Margin = new Thickness(0, 0, 0, 16)
		};

		TextBlock l2 = new TextBlock { Text = "Açıklama / Not:", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Margin = new Thickness(0, 0, 0, 6) };
		_descBox = new TextBox
		{
			FontSize = 14,
			Padding = new Thickness(10, 8, 10, 8),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			Margin = new Thickness(0, 0, 0, 20)
		};

		StackPanel btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

		Button btnCancel = new Button { Content = "İptal", Width = 90, Height = 36, Margin = new Thickness(0, 0, 10, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
		btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

		Button btnOk = new Button { Content = "Tamamla", Width = 110, Height = 36, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")), Foreground = Brushes.White, FontWeight = FontWeights.Bold, BorderThickness = new Thickness(0) };
		btnOk.Click += (s, e) =>
		{
			string txt = _amountBox.Text.Replace(',', '.').Trim();
			if (decimal.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val) && val > 0)
			{
				AmountLira = val;
				DialogResult = true;
				Close();
			}
			else
			{
				MessageBox.Show("Lütfen geçerli bir tutar girin.", "Geçersiz Tutar", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		};

		btnPanel.Children.Add(btnCancel);
		btnPanel.Children.Add(btnOk);

		root.Children.Add(l1);
		root.Children.Add(_amountBox);
		root.Children.Add(l2);
		root.Children.Add(_descBox);
		root.Children.Add(btnPanel);

		Content = root;
		Loaded += (s, e) =>
		{
			_amountBox.Focus();
			_amountBox.SelectAll();
		};
	}
}
