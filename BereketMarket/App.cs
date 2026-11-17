using System;
using System.IO;
using System.Windows;

namespace BereketMarket;

public class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		try
		{
			Database.Initialize();
			HomeWindow window = new HomeWindow();
			window.Show();
		}
		catch (Exception ex)
		{
			string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "last_error.txt");
			File.WriteAllText(logPath, ex.ToString());
			MessageBox.Show("Veritabanı hazırlanamadı.\n\n" + ex.Message + "\n\nAyrıntı: " + logPath, "Bereket Market - Hata", MessageBoxButton.OK, MessageBoxImage.Hand);
			Shutdown();
		}
	}

	[STAThread]
	public static void Main()
	{
		App app = new App();
		app.Run();
	}
}
