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

namespace WiseTorrent.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
		var blazorWebView = this.FindName("BlazorHost") as BlazorWebView;
		if (blazorWebView != null)
		{
			blazorWebView.Services = ((App)Application.Current).Services;
		}

		Console.WriteLine("MainWindow loaded");
	}
}