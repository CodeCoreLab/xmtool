using System.Windows;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

/// <summary>
/// 设置 / 修改 / 解锁主密码的窗口。视图结构见 Views/MasterPasswordWindow.xaml。
/// </summary>
public partial class MasterPasswordWindow : Window
{
	public string TitleText { get; }

	public Visibility ShowOldPassword { get; }

	public MasterPasswordPurpose Purpose { get; }

	public MasterPasswordWindow(MasterPasswordPurpose purpose)
	{
		InitializeComponent();
		Purpose = purpose;
		bool configured = CryptoService.Instance.IsMasterPasswordConfigured();
		// 解锁场景必然要填原主密码；设置场景仅在「已有主密码、需先验原密码」时才需要。
		// 一次算清，避免在下方分支里再改一次只读属性。
		ShowOldPassword = purpose == MasterPasswordPurpose.Unlock || configured
			? Visibility.Visible
			: Visibility.Collapsed;
		TitleText = purpose switch
		{
			MasterPasswordPurpose.Set => configured ? "修改主密码" : "设置主密码",
			MasterPasswordPurpose.Unlock => "请输入主密码以解锁",
			_ => "主密码",
		};
		base.DataContext = this;
		if (purpose == MasterPasswordPurpose.Unlock)
		{
			// 解锁场景只需要输入原主密码，隐藏新密码相关控件。
			NewPwd.Visibility = Visibility.Collapsed;
			ConfirmPwd.Visibility = Visibility.Collapsed;
			OldPwd.Visibility = Visibility.Visible;
		}
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		if (Purpose == MasterPasswordPurpose.Unlock)
		{
			if (CryptoService.Instance.Unlock(OldPwd.Password))
			{
				base.DialogResult = true;
				Close();
			}
			else
			{
				MessageBox.Show("主密码错误。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			return;
		}
		if (CryptoService.Instance.IsMasterPasswordConfigured() && !CryptoService.Instance.HasMasterKey && !CryptoService.Instance.Unlock(OldPwd.Password))
		{
			MessageBox.Show("原主密码不正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		var existingItems = DatabaseService.Instance.LoadAll();
		if (string.IsNullOrEmpty(NewPwd.Password))
		{
			if (MessageBox.Show("新主密码为空，将移除主密码并改回 DPAPI。是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				CryptoService.Instance.RemoveMasterPassword(existingItems);
				MessageBox.Show("主密码已移除。", "提示");
				base.DialogResult = true;
				Close();
			}
			return;
		}
		if (NewPwd.Password != ConfirmPwd.Password)
		{
			MessageBox.Show("两次输入不一致。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		CryptoService.Instance.SetMasterPassword(NewPwd.Password, existingItems);
		MessageBox.Show("主密码已保存。", "提示");
		base.DialogResult = true;
		Close();
	}
}
