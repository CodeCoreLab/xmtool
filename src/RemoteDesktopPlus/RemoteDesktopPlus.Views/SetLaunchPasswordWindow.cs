using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

public class SetLaunchPasswordWindow : Window, IComponentConnector
{
	internal PasswordBox OldPwd;

	internal PasswordBox NewPwd;

	internal PasswordBox ConfirmPwd;

	private bool _contentLoaded;

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
		DatabaseService instance = DatabaseService.Instance;
		if (instance.HasLaunchPassword())
		{
			string hashB = instance.GetSetting("launch.password.hash") ?? string.Empty;
			string saltB = instance.GetSetting("launch.password.salt") ?? string.Empty;
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
				instance.DeleteSetting("launch.password.hash");
				instance.DeleteSetting("launch.password.salt");
				MessageBox.Show("启动密码已移除。", "提示");
				base.DialogResult = true;
				Close();
			}
		}
		else if (NewPwd.Password != ConfirmPwd.Password)
		{
			MessageBox.Show("两次输入的新密码不一致。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		else
		{
			string saltB2;
			string value = CryptoService.HashLaunchPassword(NewPwd.Password, out saltB2);
			instance.SetSetting("launch.password.hash", value);
			instance.SetSetting("launch.password.salt", saltB2);
			MessageBox.Show("启动密码已设置。", "提示");
			base.DialogResult = true;
			Close();
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/远程桌面连接++;component/views/setlaunchpasswordwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			OldPwd = (PasswordBox)target;
			break;
		case 2:
			NewPwd = (PasswordBox)target;
			break;
		case 3:
			ConfirmPwd = (PasswordBox)target;
			break;
		case 4:
			((Button)target).Click += Cancel_Click;
			break;
		case 5:
			((Button)target).Click += Ok_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
