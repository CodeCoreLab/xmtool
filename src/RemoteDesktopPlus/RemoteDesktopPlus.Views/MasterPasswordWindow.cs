using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

public class MasterPasswordWindow : Window, IComponentConnector
{
	internal PasswordBox OldPwd;

	internal PasswordBox NewPwd;

	internal PasswordBox ConfirmPwd;

	private bool _contentLoaded;

	public string TitleText { get; }

	public Visibility ShowOldPassword { get; }

	public MasterPasswordPurpose Purpose { get; }

	public MasterPasswordWindow(MasterPasswordPurpose purpose)
	{
		InitializeComponent();
		Purpose = purpose;
		bool flag = CryptoService.IsMasterPasswordConfigured();
		ShowOldPassword = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		TitleText = purpose switch
		{
			MasterPasswordPurpose.Set => flag ? "修改主密码" : "设置主密码", 
			MasterPasswordPurpose.Unlock => "请输入主密码以解锁", 
			_ => "主密码", 
		};
		base.DataContext = this;
		if (purpose == MasterPasswordPurpose.Unlock)
		{
			NewPwd.Visibility = Visibility.Collapsed;
			ConfirmPwd.Visibility = Visibility.Collapsed;
			OldPwd.Visibility = Visibility.Visible;
			ShowOldPassword = Visibility.Visible;
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
		if (CryptoService.IsMasterPasswordConfigured() && !CryptoService.Instance.HasMasterKey && !CryptoService.Instance.Unlock(OldPwd.Password))
		{
			MessageBox.Show("原主密码不正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		List<ConnectionItem> existingItems = DatabaseService.Instance.LoadAll();
		if (string.IsNullOrEmpty(NewPwd.Password))
		{
			if (MessageBox.Show("新主密码为空，将移除主密码并改回 DPAPI。是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				CryptoService.Instance.RemoveMasterPassword(existingItems);
				MessageBox.Show("主密码已移除。", "提示");
				base.DialogResult = true;
				Close();
			}
		}
		else if (NewPwd.Password != ConfirmPwd.Password)
		{
			MessageBox.Show("两次输入不一致。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		else
		{
			CryptoService.Instance.SetMasterPassword(NewPwd.Password, existingItems);
			MessageBox.Show("主密码已保存。", "提示");
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
			Uri resourceLocator = new Uri("/远程桌面连接++;component/views/masterpasswordwindow.xaml", UriKind.Relative);
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
