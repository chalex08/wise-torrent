using Microsoft.AspNetCore.Components.WebView.Wpf;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using WiseTorrent.UI.Services;

namespace WiseTorrent.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public static MainWindow Instance { get; private set; }
	private readonly FullscreenStateService _fullscreenService;

	public MainWindow()
	{
		InitializeComponent();
		if (FindName("BlazorHost") is BlazorWebView blazorWebView)
		{
			blazorWebView.Services = ((App)Application.Current).Services;
		}

		Instance = this;
		_fullscreenService = ((App)Application.Current).Services.GetRequiredService<FullscreenStateService>();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (e.Key == Key.F11)
		{
			ToggleFullscreen();
		}
		base.OnKeyDown(e);
	}

	public void ToggleFullscreen()
	{
		_fullscreenService.Toggle();

		if (_fullscreenService.IsFullscreen)
		{
			ResizeMode = ResizeMode.NoResize;
			WindowState = WindowState.Maximized;
		}
		else
		{
			ResizeMode = ResizeMode.CanResize;
			WindowState = WindowState.Normal;
		}
	}

}