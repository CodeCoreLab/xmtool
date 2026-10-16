using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GongSolutions.Wpf.DragDrop;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.ViewModels;

/// <summary>
/// 主窗口的视图模型（按职责拆成多个 partial 文件）。
/// <para>
/// 原先是单文件 958 行（反编译态），一个类同时承担连接列表、连接表单、服务器设置、
/// 加密设置四件事。现按职责分文件，行为完全不变：
/// </para>
/// <list type="bullet">
///   <item>MainViewModel.cs —— 本文件：可观察状态、依赖注入、选项持久化</item>
///   <item>MainViewModel.Connections.cs —— 服务器列表 CRUD、拖拽排序、在线扫描</item>
///   <item>MainViewModel.Connect.cs —— 连接表单与发起连接</item>
///   <item>MainViewModel.ServerSettings.cs —— 本机远程桌面开关与端口</item>
///   <item>MainViewModel.SecuritySettings.cs —— 启动密码 / 主密码 / 导入导出</item>
/// </list>
/// <para>
/// 本类由 CommunityToolkit.Mvvm 源生成器补全：属性来自 <c>[ObservableProperty]</c> 字段，
/// 命令来自 <c>[RelayCommand]</c> 方法，<c>OnXxxChanged</c> 是生成器约定的部分方法钩子。
/// 反编译版本里那些带 <c>[GeneratedCode]</c> 的手写属性/命令字段已全部删除——
/// 它们本是生成器产物，保留会与生成器产生 CS0102 重复定义。
/// </para>
/// <para>
/// 依赖方向（重构目标之一）：本类**不再引用** <c>RemoteDesktopPlus.Views</c>。
/// 需要弹窗时通过 <see cref="IDialogService"/> 表达意图，具体窗口由视图层的
/// <c>WpfDialogService</c> 提供（在 <c>App.OnStartup</c> 这个组合根里注入）。
/// </para>
/// </summary>
public partial class MainViewModel : ObservableObject, IDropTarget
{
	[ObservableProperty]
	private string host = string.Empty;

	[ObservableProperty]
	private int port = 3389;

	[ObservableProperty]
	private string userName = string.Empty;

	[ObservableProperty]
	private string password = string.Empty;

	[ObservableProperty]
	private string resolution = "1920*1080";

	[ObservableProperty]
	private bool redirectLocalDrive;

	[ObservableProperty]
	private string localDriveLetter = PickDefaultDriveLetter();

	[ObservableProperty]
	private bool forceConsole;

	[ObservableProperty]
	private bool optionsExpanded = true;

	[ObservableProperty]
	private bool isRemoteEnabled;

	[ObservableProperty]
	private int currentRdpPort = 3389;

	[ObservableProperty]
	private bool isPortListening;

	[ObservableProperty]
	private string newPort = string.Empty;

	[ObservableProperty]
	private bool hasMasterPassword;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor("ConnectCommand")]
	[NotifyCanExecuteChangedFor("DeleteSelectedCommand")]
	[NotifyCanExecuteChangedFor("EditSelectedCommand")]
	private ConnectionItem? selectedConnection;

	// —— 协作者：全部由构造函数注入，绝不在类内 new 或读静态单例 ——
	private readonly DatabaseService _db;

	private readonly CryptoService _crypto;

	private readonly IDialogService _dialogs;

	/// <summary>读选项期间抑制写回，避免「加载即保存」的自我覆盖。</summary>
	private bool _suppressOptionSave;

	public ObservableCollection<ConnectionItem> Connections { get; } = new ObservableCollection<ConnectionItem>();

	public IReadOnlyList<string> ResolutionOptions { get; } = new string[10] { "全屏", "1024*768", "1280*720", "1366*768", "1440*900", "1600*900", "1680*1050", "1920*1080", "2560*1440", "3840*2160" };

	public IReadOnlyList<string> DriveLetters { get; } = BuildDriveLetters();

	private static IReadOnlyList<string> BuildDriveLetters()
	{
		try
		{
			List<string> list = (from s in (from d in DriveInfo.GetDrives()
					where d.IsReady
					select (d.Name.Length <= 0) ? string.Empty : d.Name[0].ToString().ToUpperInvariant()).Where(delegate(string s)
				{
					if (s.Length == 1)
					{
						char c2 = s[0];
						if (c2 >= 'C')
						{
							return c2 <= 'Z';
						}
						return false;
					}
					return false;
				}).Distinct()
				orderby s
				select s).ToList();
			if (list.Count > 0)
			{
				return list;
			}
		}
		catch
		{
		}
		return (from c in Enumerable.Range(67, 24)
			select ((char)c).ToString()).ToList();
	}

	private static string PickDefaultDriveLetter()
	{
		return BuildDriveLetters().FirstOrDefault((string l) => !string.Equals(l, "C", StringComparison.OrdinalIgnoreCase)) ?? "C";
	}

	/// <summary>
	/// 唯一构造入口——组合根显式传入三个协作者，测试可注入临时库与假对话框。
	/// （原先只能靠静态单例，导致视图模型的行为无法在测试里单独观察。）
	/// </summary>
	public MainViewModel(DatabaseService db, CryptoService crypto, IDialogService dialogs)
	{
		_db = db ?? throw new ArgumentNullException(nameof(db));
		_crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
		_dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

		HasMasterPassword = _crypto.IsMasterPasswordConfigured();
		LoadOptions();
		ReloadConnections();
		RefreshServerStatus();
	}

	private void LoadOptions()
	{
		try
		{
			_suppressOptionSave = true;
			string setting = _db.GetSetting(SettingKeys.OptionResolution);
			if (!string.IsNullOrEmpty(setting) && ResolutionOptions.Contains(setting))
			{
				Resolution = setting;
			}
			if (bool.TryParse(_db.GetSetting(SettingKeys.OptionRedirectDrive), out var result))
			{
				RedirectLocalDrive = result;
			}
			string setting2 = _db.GetSetting(SettingKeys.OptionLocalDriveLetter);
			if (!string.IsNullOrEmpty(setting2) && DriveLetters.Contains(setting2))
			{
				LocalDriveLetter = setting2;
			}
			if (bool.TryParse(_db.GetSetting(SettingKeys.OptionForceConsole), out var result2))
			{
				ForceConsole = result2;
			}
		}
		finally
		{
			_suppressOptionSave = false;
		}
	}

	/// <summary>
	/// 把「1920*1080」「1280x720」「全屏」解析为像素尺寸。
	/// 全屏、空值、非法值、以及非正尺寸一律返回 (null, null)，
	/// 与 <see cref="RdpLauncher.BuildRdpContent"/> 的「非正尺寸即全屏」约定保持一致。
	/// </summary>
	public static (int? w, int? h) ParseResolution(string res)
	{
		if (string.IsNullOrWhiteSpace(res))
		{
			return (w: null, h: null);
		}
		res = res.Trim();
		if (res == "全屏")
		{
			return (w: null, h: null);
		}
		string[] array = res.Split('*', 'x', 'X');
		if (array.Length == 2 && int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2)
			&& result > 0 && result2 > 0)
		{
			return (w: result, h: result2);
		}
		return (w: null, h: null);
	}

	partial void OnResolutionChanged(string value)
	{
		if (!_suppressOptionSave)
		{
			_db.SetSetting(SettingKeys.OptionResolution, value ?? string.Empty);
		}
	}

	partial void OnRedirectLocalDriveChanged(bool value)
	{
		if (!_suppressOptionSave)
		{
			_db.SetSetting(SettingKeys.OptionRedirectDrive, value.ToString());
		}
	}

	partial void OnLocalDriveLetterChanged(string value)
	{
		if (!_suppressOptionSave)
		{
			_db.SetSetting(SettingKeys.OptionLocalDriveLetter, value ?? string.Empty);
		}
	}

	partial void OnForceConsoleChanged(bool value)
	{
		if (!_suppressOptionSave)
		{
			_db.SetSetting(SettingKeys.OptionForceConsole, value.ToString());
		}
	}

	partial void OnSelectedConnectionChanged(ConnectionItem? value)
	{
		if (value != null)
		{
			Host = value.Host;
			Port = value.Port;
			UserName = value.UserName;
			Password = _crypto.Decrypt(value.EncryptedPassword);
		}
	}
}
