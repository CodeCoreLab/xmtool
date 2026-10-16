using System.Windows;
using System.Windows.Controls;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

/// <summary>
/// 设置/修改启动密码的窗口。视图结构见 Views/SetLaunchPasswordWindow.xaml。
/// </summary>
public partial class SetLaunchPasswordWindow : Window
{
	public SetLaunchPasswordWindow()
	{
		InitializeComponent();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		DatabaseService db = DatabaseService.Instance;
		if (db.HasLaunchPassword())
		{
			string hashB = db.GetSetting(SettingKeys.LaunchPasswordHash) ?? string.Empty;
			string saltB = db.GetSetting(SettingKeys.LaunchPasswordSalt) ?? string.Empty;
			if (!CryptoService.VerifyLaunchPassword(OldPwd.Password, hashB, saltB))
			{
				MessageBox.Show("原密码不正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
				return;
			}
		}
		if (string.IsNullOrEmpty(NewPwd.Password))
		{
			if (MessageBox.Show("新密码为空，将移除启动密码。是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				db.DeleteSetting(SettingKeys.LaunchPasswordHash);
				db.DeleteSetting(SettingKeys.LaunchPasswordSalt);
				MessageBox.Show("启动密码已移除。", "提示");
				base.DialogResult = true;
				Close();
			}
			return;
		}
		if (NewPwd.Password != ConfirmPwd.Password)
		{
			MessageBox.Show("两次输入的新密码不一致。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		string hash = CryptoService.HashLaunchPassword(NewPwd.Password, out string salt);
		db.SetSetting(SettingKeys.LaunchPasswordHash, hash);
		db.SetSetting(SettingKeys.LaunchPasswordSalt, salt);
		MessageBox.Show("启动密码已设置。", "提示");
		base.DialogResult = true;
		Close();
	}
}
