using System.Windows;

namespace RemoteDesktopPlus.Views;

/// <summary>
/// 关于窗口。视图结构见 Views/AboutWindow.xaml。
/// </summary>
public partial class AboutWindow : Window
{
	public AboutWindow()
	{
		InitializeComponent();
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
