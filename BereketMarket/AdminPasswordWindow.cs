using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace BereketMarket;

public class AdminPasswordWindow : Window, IComponentConnector
{
	private readonly bool changePassword;

	private bool setupMode;

	private bool busy;

	internal TextBlock TitleTextBlock;

	internal TextBlock DescriptionTextBlock;

	internal StackPanel FieldsPanel;

	internal StackPanel CurrentPasswordPanel;

	internal TextBlock CurrentPasswordLabel;

	internal PasswordBox CurrentPasswordBox;

	internal StackPanel NewPasswordPanel;

	internal PasswordBox NewPasswordBox;

	internal PasswordBox ConfirmPasswordBox;

	internal TextBlock StatusTextBlock;

	internal Button SubmitButton;

	internal Button CancelButton;

	private bool _contentLoaded;

	public AdminPasswordWindow(bool changePassword = false)
	{
		if (changePassword)
		{
			AdminSecurity.RequireAdmin();
		}
		this.changePassword = changePassword;
		InitializeComponent();
		Loaded += async (object _, RoutedEventArgs _) =>
		{
			await PrepareAsync();
		};
		Closing += (object? _, CancelEventArgs e) =>
		{
			if (busy)
			{
				e.Cancel = true;
			}
		};
		Closed += (object? _, EventArgs _) =>
		{
			CurrentPasswordBox.Clear();
			NewPasswordBox.Clear();
			ConfirmPasswordBox.Clear();
		};
	}

	private async Task PrepareAsync()
	{
		busy = true;
		FieldsPanel.IsEnabled = false;
		CancelButton.IsEnabled = false;
		try
		{
			bool flag = await Task.Run((Func<bool>)AdminSecurity.HasPassword);
			if (changePassword && !flag)
			{
				throw new InvalidOperationException("Mevcut yönetici şifresi bulunamadı.");
			}
			setupMode = !flag;
			if (changePassword)
			{
				TitleTextBlock.Text = "Şifre değiştir";
				DescriptionTextBlock.Text = "Mevcut şifreni doğrula ve yeni şifreni belirle.";
				CurrentPasswordLabel.Text = "Mevcut şifre";
				SubmitButton.Content = "Şifreyi değiştir";
			}
			else if (setupMode)
			{
				TitleTextBlock.Text = "Yönetici şifresi oluştur";
				DescriptionTextBlock.Text = "İlk yönetici girişinde kendi şifreni belirle.";
				CurrentPasswordPanel.Visibility = Visibility.Collapsed;
				SubmitButton.Content = "Şifreyi oluştur";
			}
			else
			{
				TitleTextBlock.Text = "Yönetici girişi";
				DescriptionTextBlock.Text = "Yönetim bölümünü açmak için şifreni gir.";
				NewPasswordPanel.Visibility = Visibility.Collapsed;
				SubmitButton.Content = "Giriş yap";
			}
			SubmitButton.IsEnabled = true;
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = ex.Message;
			SubmitButton.IsEnabled = false;
		}
		finally
		{
			busy = false;
			FieldsPanel.IsEnabled = true;
			CancelButton.IsEnabled = true;
		}
		if (setupMode)
		{
			NewPasswordBox.Focus();
		}
		else
		{
			CurrentPasswordBox.Focus();
		}
	}

	private async void SubmitButton_Click(object sender, RoutedEventArgs e)
	{
		if (busy)
		{
			return;
		}
		string currentPassword = CurrentPasswordBox.Password;
		string newPassword = NewPasswordBox.Password;
		if (setupMode || changePassword)
		{
			if (newPassword != ConfirmPasswordBox.Password)
			{
				StatusTextBlock.Text = "Yeni şifreler eşleşmiyor.";
				ConfirmPasswordBox.Focus();
				return;
			}
			if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 12)
			{
				StatusTextBlock.Text = "En az 12 karakterlik bir şifre belirle.";
				NewPasswordBox.Focus();
				return;
			}
		}
		if (!setupMode && currentPassword.Length == 0)
		{
			StatusTextBlock.Text = "Mevcut şifreni gir.";
			CurrentPasswordBox.Focus();
			return;
		}
		busy = true;
		SubmitButton.IsEnabled = false;
		CancelButton.IsEnabled = false;
		FieldsPanel.IsEnabled = false;
		StatusTextBlock.Text = "İşlem yapılıyor...";
		bool succeeded = false;
		try
		{
			await Task.Run(() =>
			{
				if (changePassword)
				{
					AdminSecurity.ChangePassword(currentPassword, newPassword);
				}
				else if (setupMode)
				{
					AdminSecurity.CreatePassword(newPassword);
				}
				else
				{
					AdminSecurity.Login(currentPassword);
				}
			});
			succeeded = true;
		}
		catch (Exception ex)
		{
			StatusTextBlock.Text = ex.Message;
			CurrentPasswordBox.Clear();
		}
		finally
		{
			busy = false;
			SubmitButton.IsEnabled = true;
			CancelButton.IsEnabled = true;
			FieldsPanel.IsEnabled = true;
		}
		if (succeeded)
		{
			DialogResult = true;
		}
		else if (!setupMode)
		{
			CurrentPasswordBox.Focus();
		}
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		if (!busy)
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
			Uri resourceLocator = new Uri("/BereketMarket;component/adminpasswordwindow.xaml", UriKind.Relative);
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
			TitleTextBlock = (TextBlock)target;
			break;
		case 2:
			DescriptionTextBlock = (TextBlock)target;
			break;
		case 3:
			FieldsPanel = (StackPanel)target;
			break;
		case 4:
			CurrentPasswordPanel = (StackPanel)target;
			break;
		case 5:
			CurrentPasswordLabel = (TextBlock)target;
			break;
		case 6:
			CurrentPasswordBox = (PasswordBox)target;
			break;
		case 7:
			NewPasswordPanel = (StackPanel)target;
			break;
		case 8:
			NewPasswordBox = (PasswordBox)target;
			break;
		case 9:
			ConfirmPasswordBox = (PasswordBox)target;
			break;
		case 10:
			StatusTextBlock = (TextBlock)target;
			break;
		case 11:
			SubmitButton = (Button)target;
			SubmitButton.Click += SubmitButton_Click;
			break;
		case 12:
			CancelButton = (Button)target;
			CancelButton.Click += CancelButton_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
