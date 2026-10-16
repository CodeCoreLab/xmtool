using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

/// <summary>
/// 添加/编辑服务器的对话框。视图结构见 Views/EditConnectionWindow.xaml。
/// </summary>
public partial class EditConnectionWindow : Window, INotifyPropertyChanged
{
	private string _plainPassword = string.Empty;

	private bool _showPlainPassword;

	private bool _suppressPwdChanged;

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
			if (_plainPassword == value)
			{
				return;
			}
			_plainPassword = value;
			if (PwdBox != null && PwdBox.Password != value)
			{
				_suppressPwdChanged = true;
				PwdBox.Password = value;
				_suppressPwdChanged = false;
			}
			OnPropertyChanged();
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
			if (_showPlainPassword == value)
			{
				return;
			}
			_showPlainPassword = value;
			if (value && PwdBox != null)
			{
				PlainPassword = PwdBox.Password;
			}
			OnPropertyChanged();
		}
	}

	public string TitleText => IsEdit ? "编辑服务器" : "添加服务器列表";

	public string OkButtonText => IsEdit ? "保 存" : "添 加";

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
			OnPropertyChanged(nameof(PlainPassword));
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
		if (Result.Port < 1 || Result.Port > 65535)
		{
			MessageBox.Show("端口必须在 1~65535 之间。", "提示");
			return;
		}
		if (string.IsNullOrWhiteSpace(Result.UserName))
		{
			MessageBox.Show("登陆账号不能为空。", "提示");
			return;
		}
		Result.Password = ShowPlainPassword ? PlainPassword : PwdBox.Password;
		if (!string.IsNullOrEmpty(Result.Password)
			|| MessageBox.Show("登陆密码为空，确定继续吗？", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
}
