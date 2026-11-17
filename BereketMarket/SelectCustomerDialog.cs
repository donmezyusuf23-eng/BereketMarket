using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BereketMarket;

public class SelectCustomerDialog : Window
{
	public Customer? SelectedCustomer { get; private set; }
	private readonly CustomerRepository _repo = new CustomerRepository();
	private TextBox _searchBox = null!;
	private ListBox _list = null!;

	public SelectCustomerDialog()
	{
		Title = "Veresiye — Müşteri Seçimi";
		Width = 500;
		Height = 550;
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A"));
		Foreground = Brushes.White;
		FontFamily = new FontFamily("Segoe UI");

		Grid grid = new Grid { Margin = new Thickness(20) };
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		TextBlock title = new TextBlock
		{
			Text = "Veresiye Yazılacak Müşteriyi Seçin",
			FontSize = 18,
			FontWeight = FontWeights.Bold,
			Foreground = Brushes.White,
			Margin = new Thickness(0, 0, 0, 14)
		};
		Grid.SetRow(title, 0);
		grid.Children.Add(title);

		_searchBox = new TextBox
		{
			FontSize = 15,
			Padding = new Thickness(12, 10, 12, 10),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B")),
			Foreground = Brushes.White,
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			Margin = new Thickness(0, 0, 0, 14)
		};
		_searchBox.TextChanged += async (s, e) => await LoadAsync(_searchBox.Text);
		Grid.SetRow(_searchBox, 1);
		grid.Children.Add(_searchBox);

		_list = new ListBox
		{
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			Foreground = Brushes.White
		};
		_list.MouseDoubleClick += (s, e) => ConfirmSelection();
		Grid.SetRow(_list, 2);
		grid.Children.Add(_list);

		StackPanel btnPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right,
			Margin = new Thickness(0, 16, 0, 0)
		};

		Button btnNew = new Button
		{
			Content = "+ Yeni Müşteri",
			Padding = new Thickness(14, 8, 14, 8),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0),
			Margin = new Thickness(0, 0, 10, 0),
			Cursor = Cursors.Hand
		};
		btnNew.Click += async (s, e) =>
		{
			SimplePromptDialog dlg = new SimplePromptDialog("Yeni Müşteri Ekle", "Müşteri Adı Soyadı:", "Telefon Numarası (İsteğe bağlı):");
			dlg.Owner = this;
			if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.InputText1))
			{
				int id = await _repo.AddCustomerAsync(dlg.InputText1, dlg.InputText2, null);
				await LoadAsync();
			}
		};

		Button btnCancel = new Button
		{
			Content = "İptal",
			Width = 80,
			Height = 36,
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
			Foreground = Brushes.White,
			BorderThickness = new Thickness(0),
			Margin = new Thickness(0, 0, 10, 0),
			Cursor = Cursors.Hand
		};
		btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

		Button btnSelect = new Button
		{
			Content = "Bu Müşteriye Yaz",
			Padding = new Thickness(18, 8, 18, 8),
			Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
			Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
			FontWeight = FontWeights.Bold,
			BorderThickness = new Thickness(0),
			Cursor = Cursors.Hand
		};
		btnSelect.Click += (s, e) => ConfirmSelection();

		btnPanel.Children.Add(btnNew);
		btnPanel.Children.Add(btnCancel);
		btnPanel.Children.Add(btnSelect);
		Grid.SetRow(btnPanel, 3);
		grid.Children.Add(btnPanel);

		Content = grid;
		Loaded += async (s, e) =>
		{
			await LoadAsync();
			_searchBox.Focus();
		};
	}

	private async System.Threading.Tasks.Task LoadAsync(string? search = null)
	{
		List<Customer> customers = await _repo.GetAllCustomersAsync(search);
		_list.Items.Clear();
		foreach (Customer c in customers)
		{
			Grid row = new Grid { Margin = new Thickness(4) };
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			TextBlock txt = new TextBlock
			{
				Text = $"{c.FullName} {(string.IsNullOrEmpty(c.Phone) ? "" : $"({c.Phone})")}",
				FontSize = 14,
				FontWeight = FontWeights.SemiBold,
				Foreground = Brushes.White,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(txt, 0);
			row.Children.Add(txt);

			TextBlock debt = new TextBlock
			{
				Text = $"Borç: {c.BalanceLira:N2} TL",
				FontSize = 13,
				FontWeight = FontWeights.Bold,
				Foreground = c.BalanceLira > 0
					? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"))
					: new SolidColorBrush((Color)ColorConverter.ConvertFromString("#34D399")),
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(debt, 1);
			row.Children.Add(debt);

			ListBoxItem item = new ListBoxItem { Content = row, Tag = c };
			_list.Items.Add(item);
		}
	}

	private void ConfirmSelection()
	{
		if (_list.SelectedItem is ListBoxItem item && item.Tag is Customer c)
		{
			SelectedCustomer = c;
			DialogResult = true;
			Close();
		}
		else
		{
			MessageBox.Show("Lütfen listeden bir müşteri seçin.", "Müşteri Seçilmedi", MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}
}
