using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

public class EditConnectionWindow : Window, INotifyPropertyChanged, IComponentConnector
{
	private string _plainPassword = string.Empty;

	private bool _showPlainPassword;

	private bool _suppressPwdChanged;

	internal TextBox IpBox;

	internal PasswordBox PwdBox;

	internal TextBox PlainBox;

	internal Button OkButton;

	private bool _contentLoaded;

	public bool IsEdit { get; }

	public ConnectionItem? Source { get; }

	public string PlainPassword
	{
		get
		{
			return _plainPassword;
		}
		set
		{
			if (!(_plainPassword == value))
			{
				_plainPassword = value;
				if (PwdBox != null && PwdBox.Password != value)
				{
					_suppressPwdChanged = true;
					PwdBox.Password = value;
					_suppressPwdChanged = false;
				}
				OnPropertyChanged("PlainPassword");
			}
		}
	}

	public bool ShowPlainPassword
	{
		get
		{
			return _showPlainPassword;
		}
		set
		{
			if (_showPlainPassword != value)
			{
				_showPlainPassword = value;
				if (value && PwdBox != null)
				{
					PlainPassword = PwdBox.Password;
				}
				OnPropertyChanged("ShowPlainPassword");
			}
		}
	}

	public string TitleText
	{
		get
		{
			if (!IsEdit)
			{
				return "添加服务器列表";
			}
			return "编辑服务器";
		}
	}

	public string OkButtonText
	{
		get
		{
			if (!IsEdit)
			{
				return "添 加";
			}
			return "保 存";
		}
	}

	public ConnectionFormResult Result { get; } = new ConnectionFormResult();


	public event PropertyChangedEventHandler? PropertyChanged;

	public EditConnectionWindow(ConnectionItem? source = null)
	{
		InitializeComponent();
		Source = source;
		IsEdit = source != null;
		base.DataContext = Result;
		if (source != null)
		{
			Result.Host = source.Host;
			Result.Port = source.Port;
			Result.UserName = source.UserName;
			Result.Remark = source.Remark;
			string plain = CryptoService.Instance.Decrypt(source.EncryptedPassword);
			base.Loaded += delegate
			{
				_suppressPwdChanged = true;
				PwdBox.Password = plain;
				_suppressPwdChanged = false;
				PlainPassword = plain;
				IpBox.Focus();
				IpBox.SelectAll();
			};
		}
		else
		{
			Result.Port = 3389;
			Result.UserName = "Administrator";
			base.Loaded += delegate
			{
				IpBox.Focus();
			};
		}
	}

	private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (!_suppressPwdChanged && sender is PasswordBox passwordBox && _plainPassword != passwordBox.Password)
		{
			_plainPassword = passwordBox.Password;
			OnPropertyChanged("PlainPassword");
		}
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(Result.Host))
		{
			MessageBox.Show("IP/域名不能为空。", "提示");
			IpBox.Focus();
			return;
		}
		int port = Result.Port;
		if ((port < 1 || port > 65535) ? true : false)
		{
			MessageBox.Show("端口必须在 1~65535 之间。", "提示");
			return;
		}
		if (string.IsNullOrWhiteSpace(Result.UserName))
		{
			MessageBox.Show("登陆账号不能为空。", "提示");
			return;
		}
		Result.Password = (ShowPlainPassword ? PlainPassword : PwdBox.Password);
		if (!string.IsNullOrEmpty(Result.Password) || MessageBox.Show("登陆密码为空，确定继续吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
		{
			base.DialogResult = true;
			Close();
		}
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	private void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/远程桌面连接++;component/views/editconnectionwindow.xaml", UriKind.Relative);
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
			IpBox = (TextBox)target;
			break;
		case 2:
			PwdBox = (PasswordBox)target;
			PwdBox.PasswordChanged += PwdBox_PasswordChanged;
			break;
		case 3:
			PlainBox = (TextBox)target;
			break;
		case 4:
			((Button)target).Click += Cancel_Click;
			break;
		case 5:
			OkButton = (Button)target;
			OkButton.Click += Ok_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
