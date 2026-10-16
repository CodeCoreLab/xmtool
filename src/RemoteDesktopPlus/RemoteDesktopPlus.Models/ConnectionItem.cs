using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace RemoteDesktopPlus.Models;

public class ConnectionItem : ObservableObject
{
	[ObservableProperty]
	private int index;

	[ObservableProperty]
	private string host = string.Empty;

	[ObservableProperty]
	private int port = 3389;

	[ObservableProperty]
	private string userName = string.Empty;

	[ObservableProperty]
	private string remark = string.Empty;

	[ObservableProperty]
	private OnlineStatus status;

	public long Id { get; set; }

	public string EncryptedPassword { get; set; } = string.Empty;


	public int SortOrder { get; set; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Index
	{
		get
		{
			return index;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(index, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Index);
				index = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Index);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Host
	{
		get
		{
			return host;
		}
		[MemberNotNull("host")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(host, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Host);
				host = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Host);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Port
	{
		get
		{
			return port;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(port, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Port);
				port = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Port);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string UserName
	{
		get
		{
			return userName;
		}
		[MemberNotNull("userName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(userName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UserName);
				userName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UserName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Remark
	{
		get
		{
			return remark;
		}
		[MemberNotNull("remark")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(remark, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Remark);
				remark = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Remark);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public OnlineStatus Status
	{
		get
		{
			return status;
		}
		set
		{
			if (!EqualityComparer<OnlineStatus>.Default.Equals(status, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Status);
				status = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Status);
			}
		}
	}
}
