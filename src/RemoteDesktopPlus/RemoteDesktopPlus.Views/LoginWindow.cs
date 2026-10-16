using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

/// <summary>
/// 启动密码解锁窗口。视图结构见 Views/LoginWindow.xaml。
/// </summary>
public partial class LoginWindow : Window
{
	private const int MaxFailures = 5;

	private int _failures;

	public LoginWindow()
	{
		InitializeComponent();
		base.Loaded += delegate
		{
			PwdBox.Focus();
		};
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		TryLogin();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	private void PwdBox_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter)
		{
			TryLogin();
		}
	}

	private void TryLogin()
	{
		string hashB = DatabaseService.Instance.GetSetting(SettingKeys.LaunchPasswordHash) ?? string.Empty;
		string saltB = DatabaseService.Instance.GetSetting(SettingKeys.LaunchPasswordSalt) ?? string.Empty;
		if (CryptoService.VerifyLaunchPassword(PwdBox.Password, hashB, saltB))
		{
			base.DialogResult = true;
			Close();
			return;
		}
		_failures++;
		PwdBox.Clear();
		if (_failures >= MaxFailures)
		{
			MessageBox.Show("密码错误次数过多，程序将退出。", "提示", MessageBoxButton.OK, MessageBoxImage.Hand);
			base.DialogResult = false;
			Close();
		}
		else
		{
			MessageBox.Show($"密码错误，剩余 {MaxFailures - _failures} 次尝试。", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}
}
