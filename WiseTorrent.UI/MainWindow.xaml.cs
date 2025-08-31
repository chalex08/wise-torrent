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
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace WiseTorrent.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public static MainWindow? Instance { get; private set; }
	private readonly FullscreenStateService _fullscreenService;

	[DllImport("user32.dll")]
	private static extern bool ReleaseCapture();

	[DllImport("user32.dll")]
	private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

	private const int WM_NCLBUTTONDOWN = 0xA1;
	private const int HTCAPTION = 0x2;

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

	public void DragWindow()
	{
		var hwnd = new WindowInteropHelper(this).Handle;
		ReleaseCapture();
		SendMessage(hwnd, WM_NCLBUTTONDOWN, HTCAPTION, 0);
	}
}