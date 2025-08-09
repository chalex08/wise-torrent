using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WiseTorrent.UI.Services;

namespace WiseTorrent.UI
{
	public partial class App : Application
	{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
		public IServiceProvider Services { get; private set; }

		protected override void OnStartup(StartupEventArgs e)
		{
			var serviceCollection = new ServiceCollection();
			ServiceConfig.ConfigureServices(serviceCollection);
			UIServiceConfig.ConfigureServices(serviceCollection);
			serviceCollection.AddWpfBlazorWebView();
			Services = serviceCollection.BuildServiceProvider();

			var mainWindow = new MainWindow();
			mainWindow.Resources.Add(typeof(IServiceProvider), Services);
			mainWindow.Show();
		}
	}
}