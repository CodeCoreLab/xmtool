using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

public class LoginWindow : Window, IComponentConnector
{
	private int _failures;

	internal PasswordBox PwdBox;

	private bool _contentLoaded;

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
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.Key == 6)
		{
			TryLogin();
		}
	}

	private void TryLogin()
	{
		string hashB = DatabaseService.Instance.GetSetting("launch.password.hash") ?? string.Empty;
		string saltB = DatabaseService.Instance.GetSetting("launch.password.salt") ?? string.Empty;
		if (CryptoService.VerifyLaunchPassword(PwdBox.Password, hashB, saltB))
		{
			base.DialogResult = true;
			Close();
			return;
		}
		_failures++;
		PwdBox.Clear();
		if (_failures >= 5)
		{
			MessageBox.Show("密码错误次数过多，程序将退出。", "提示", MessageBoxButton.OK, MessageBoxImage.Hand);
			base.DialogResult = false;
			Close();
		}
		else
		{
			MessageBox.Show($"密码错误，剩余 {5 - _failures} 次尝试。", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/远程桌面连接++;component/views/loginwindow.xaml", UriKind.Relative);
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
			PwdBox = (PasswordBox)target;
			PwdBox.KeyDown += PwdBox_KeyDown;
			break;
		case 2:
			((Button)target).Click += Cancel_Click;
			break;
		case 3:
			((Button)target).Click += Ok_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
