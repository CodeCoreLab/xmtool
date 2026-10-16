using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RemoteDesktopPlus.Views;

public sealed class ConnectionFormResult : INotifyPropertyChanged
{
	private string _host = string.Empty;

	private int _port = 3389;

	private string _userName = "Administrator";

	private string _remark = string.Empty;

	public string Host
	{
		get
		{
			return _host;
		}
		set
		{
			_host = value;
			Notify("Host");
		}
	}

	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			_port = value;
			Notify("Port");
		}
	}

	public string UserName
	{
		get
		{
			return _userName;
		}
		set
		{
			_userName = value;
			Notify("UserName");
		}
	}

	public string Remark
	{
		get
		{
			return _remark;
		}
		set
		{
			_remark = value;
			Notify("Remark");
		}
	}

	public string Password { get; set; } = string.Empty;


	public event PropertyChangedEventHandler? PropertyChanged;

	private void Notify([CallerMemberName] string? n = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n ?? string.Empty));
	}
}
