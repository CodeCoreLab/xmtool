using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RemoteDesktopPlus.Models;

/// <summary>
/// 「添加/编辑服务器」对话框的数据载体。
/// <para>
/// 它刻意只承载字段 + 变更通知，不含任何 WPF 控件类型；因此从 Views 移入 Models 后，
/// ViewModel 可以只用这个 DTO 与对话框交互，而不必知道窗口本身的存在。
/// </para>
/// </summary>
public sealed class ConnectionFormResult : INotifyPropertyChanged
{
	private string _host = string.Empty;

	private int _port = 3389;

	private string _userName = "Administrator";

	private string _remark = string.Empty;

	public string Host
	{
		get => _host;
		set
		{
			if (_host == value)
			{
				return;
			}
			_host = value;
			Notify();
		}
	}

	public int Port
	{
		get => _port;
		set
		{
			if (_port == value)
			{
				return;
			}
			_port = value;
			Notify();
		}
	}

	public string UserName
	{
		get => _userName;
		set
		{
			if (_userName == value)
			{
				return;
			}
			_userName = value;
			Notify();
		}
	}

	public string Remark
	{
		get => _remark;
		set
		{
			if (_remark == value)
			{
				return;
			}
			_remark = value;
			Notify();
		}
	}

	/// <summary>明文密码。仅短暂存在于内存，落库前由 CryptoService 加密。</summary>
	public string Password { get; set; } = string.Empty;

	public event PropertyChangedEventHandler? PropertyChanged;

	private void Notify([CallerMemberName] string? name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
	}
}
