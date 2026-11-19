// ========================================================================
//  M1 平移切片（机械生成，勿手改）—— 源: ../../RemoteDesktopPlus.All.cs
//  范围: 行 18-3333 = S01-S17 区段（Models + Services，不含 S18 TrayController）
//  再生成: python Server/Shared/generate.py（锚点变更时同步本文件）
// ========================================================================

// ════════════════════════════════════════════════════════════════════════
// 区段 01 / 39：RemoteDesktopPlus.Models/OnlineStatus.cs  →  namespace RemoteDesktopPlus.Models
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Models
{
// (no usings)

public enum OnlineStatus
{
	Unknown,
	Online,
	Offline,
	Checking
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 02 / 39：RemoteDesktopPlus.Models/MasterPasswordPurpose.cs  →  namespace RemoteDesktopPlus.Models
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Models
{
// (no usings)

/// <summary>
/// 主密码对话框的用途。
/// <para>
/// 该枚举原先定义在 <c>RemoteDesktopPlus.Views</c> 命名空间，导致 ViewModel 必须引用 Views
/// 才能指定用途。这里移到 Models（无任何依赖的领域层），使 ViewModel / Services 只需依赖 Models。
/// </para>
/// </summary>
public enum MasterPasswordPurpose
{
	/// <summary>设置或修改主密码。</summary>
	Set,

	/// <summary>输入已有主密码以解锁。</summary>
	Unlock
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 03 / 39：RemoteDesktopPlus.Models/ConnectionFormResult.cs  →  namespace RemoteDesktopPlus.Models
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Models
{
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// 「添加/编辑服务器」对话框的数据载体。
/// <para>
/// 它刻意只承载字段 + 变更通知，不含任何 WPF 控件类型；因此从 Views 移入 Models 后，
/// ViewModel 可以只用这个 DTO 与对话框交互，而不必知道窗口本身的存在。
/// </para>
/// </summary>
public sealed class ConnectionFormResult : INotifyPropertyChanged
{
	/// <summary>主机（IP / 域名）最大长度。</summary>
	public const int MaxHostLength = 255;

	/// <summary>登录账号最大长度。</summary>
	public const int MaxUserNameLength = 128;

	/// <summary>密码最大长度。</summary>
	public const int MaxPasswordLength = 1024;

	/// <summary>备注最大长度。</summary>
	public const int MaxRemarkLength = 2000;

	/// <summary>分组名最大长度。</summary>
	public const int MaxGroupNameLength = 64;

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

	/// <summary>
	/// 分组名（空 = 未分组）。用于按组筛选，以及按组覆盖连接配置
	/// （分辨率/盘符/剪贴板），见 <see cref="Services.GroupOptionStore"/>。
	/// </summary>
	public string GroupName
	{
		get => _groupName;
		set
		{
			if (_groupName == value)
			{
				return;
			}
			_groupName = value;
			Notify();
		}
	}

	private string _groupName = string.Empty;

	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// 校验字段长度是否越界。返回 null 表示通过；否则返回第一条错误信息。
	/// 上限的作用：防止异常长输入撑爆 SQLite 记录、并把不可信内容带进 .rdp / 凭据存储。
	/// </summary>
	public string? ValidateLengths()
	{
		if ((Host ?? string.Empty).Length > MaxHostLength)
		{
			return "IP/域名长度不能超过 " + MaxHostLength + " 个字符。";
		}
		if ((UserName ?? string.Empty).Length > MaxUserNameLength)
		{
			return "登录账号长度不能超过 " + MaxUserNameLength + " 个字符。";
		}
		if ((Password ?? string.Empty).Length > MaxPasswordLength)
		{
			return "登录密码长度不能超过 " + MaxPasswordLength + " 个字符。";
		}
		if ((Remark ?? string.Empty).Length > MaxRemarkLength)
		{
			return "备注长度不能超过 " + MaxRemarkLength + " 个字符。";
		}
		if ((GroupName ?? string.Empty).Length > MaxGroupNameLength)
		{
			return "分组名长度不能超过 " + MaxGroupNameLength + " 个字符。";
		}
		return null;
	}

	private void Notify([CallerMemberName] string? name = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 04 / 39：RemoteDesktopPlus.Models/ConnectionItem.cs  →  namespace RemoteDesktopPlus.Models
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Models
{
using System;
using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 服务器列表中的一条连接记录。
/// <para>
/// 属性由 CommunityToolkit.Mvvm 的 <c>[ObservableProperty]</c> 源生成器生成
/// （见 analyzers\CommunityToolkit.Mvvm.SourceGenerators.dll）。
/// 反编译版本里那些带 <c>[GeneratedCode]</c> 特性的手写属性是生成器产物，
/// 若保留它们会与生成器冲突（CS0102 重复定义），故一律删除，只留字段声明。
/// </para>
/// </summary>
public partial class ConnectionItem : ObservableObject
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

	/// <summary>分组/标签（v2 schema 扩展列），用于后续按组筛选与显示。</summary>
	[ObservableProperty]
	private string groupName = string.Empty;

	/// <summary>
	/// 最后成功连接时间（Unix 秒，0 = 从未连接）。
	/// 用 [ObservableProperty] 而非普通属性：连接成功回写后要立刻让
	/// <see cref="LastConnectedText"/> 与排序一起刷新，否则界面停在旧值。
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(LastConnectedText))]
	private long lastConnectedAt;

	/// <summary>
	/// 「最近连接」列的显示文本。由 <see cref="LastConnectedAt"/> 换算成相对时间，
	/// 比原始时间戳更易读（列表里没人愿意心算 Unix 秒）。
	/// </summary>
	public string LastConnectedText
	{
		get
		{
			if (LastConnectedAt <= 0)
			{
				return "从未";
			}
			DateTimeOffset then = DateTimeOffset.FromUnixTimeSeconds(LastConnectedAt).ToLocalTime();
			TimeSpan span = DateTimeOffset.Now - then;
			if (span.TotalSeconds < 60)
			{
				return "刚刚";
			}
			if (span.TotalMinutes < 60)
			{
				return $"{(int)span.TotalMinutes} 分钟前";
			}
			if (span.TotalHours < 24)
			{
				return $"{(int)span.TotalHours} 小时前";
			}
			if (span.TotalDays < 30)
			{
				return $"{(int)span.TotalDays} 天前";
			}
			return then.ToString("yyyy-MM-dd");
		}
	}

	/// <summary>协议类型（v2 schema 扩展列），当前仅 rdp；为协议扩展（E5）预留。</summary>
	public string Protocol { get; set; } = "rdp";

	public long Id { get; set; }

	public string EncryptedPassword { get; set; } = string.Empty;

	public int SortOrder { get; set; }
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 05 / 39：RemoteDesktopPlus.Services/SettingKeys.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
// (no usings)

public static class SettingKeys
{
	public const string LaunchPasswordHash = "launch.password.hash";

	public const string LaunchPasswordSalt = "launch.password.salt";

	/// <summary>启动密码连续错误次数（持久化，防止重启绕过计数）。</summary>
	public const string LaunchPasswordFailCount = "launch.password.fail_count";

	/// <summary>启动密码锁定截止时间（Unix 秒，UTC）。</summary>
	public const string LaunchPasswordLockedUntil = "launch.password.locked_until";

	public const string MasterKeyVerifier = "master.key.verifier";

	public const string MasterKeySalt = "master.key.salt";

	public const string OptionResolution = "options.resolution";

	public const string OptionRedirectDrive = "options.redirect_drive";

	public const string OptionLocalDriveLetter = "options.local_drive_letter";

	public const string OptionForceConsole = "options.force_console";

	public const string OptionRedirectClipboard = "options.redirect_clipboard";

	/// <summary>
	/// 列表排序方式：`manual`（拖拽顺序，默认）或 `recent`（最近连接优先）。
	/// 两者互斥 —— 按最近连接排时拖拽改的 SortOrder 不参与显示，
	/// 否则用户会看到「拖了但没动」。
	/// </summary>
	public const string OptionSortMode = "options.sort_mode";

	/// <summary>分组级配置覆盖表（JSON），见 <see cref="GroupOptionStore"/>。</summary>
	public const string OptionGroupOverrides = "options.group_overrides";

	// ── 以下为 v2.1.0 移植小马线 v1.2.0 增量功能的新增键 ──

	/// <summary>开机自启开关（计划任务实现，见 <see cref="AutoStartService"/>）。</summary>
	public const string OptionAutoStart = "options.auto_start";

	/// <summary>点关闭按钮时隐藏到托盘（true=隐藏，false=真退出）。默认 true。</summary>
	public const string OptionCloseToTray = "options.close_to_tray";

	/// <summary>最小化时隐藏到托盘。默认 true。</summary>
	public const string OptionMinimizeToTray = "options.minimize_to_tray";

	/// <summary>按 ESC 隐藏到托盘。默认 false。</summary>
	public const string OptionEscToTray = "options.esc_to_tray";

	/// <summary>主窗口置顶。默认 false。</summary>
	public const string OptionTopmost = "options.topmost";
}
}

// ════════════════════════════════════════════════════════════════════════
// 追加区段：RemoteDesktopPlus.Services/AppVersion.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Reflection;

/// <summary>
/// 应用版本的唯一来源：读 <c>Properties/AssemblyInfo.cs</c> 的
/// <c>AssemblyInformationalVersion</c>。设置窗口页脚与关于窗口都从这里取，
/// 不在 XAML 里硬编码 —— 否则两处版本号必然漂移。
/// </summary>
public static class AppVersion
{
	/// <summary>形如 <c>2.0.1</c>；读不到时返回 <c>0.0.0</c> 而不是抛异常。</summary>
	public static string Current { get; } = Resolve();

	private static string Resolve()
	{
		try
		{
			string? informational = Assembly.GetEntryAssembly()
				?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
				?.InformationalVersion;
			if (!string.IsNullOrWhiteSpace(informational))
			{
				// 去掉 SourceLink 追加的 "+<commit>" 后缀，只留语义化版本。
				int plus = informational.IndexOf('+');
				return plus > 0 ? informational.Substring(0, plus) : informational;
			}
			// 回落：用 AssemblyVersion。刻意不读 FileVersionInfo(asm.Location) ——
			// 单文件发布下 Assembly.Location 为空（IL3000），那条路径在真实产物里永远走不通。
			Version? asmVersion = Assembly.GetEntryAssembly()?.GetName().Version;
			return asmVersion == null ? "0.0.0" : $"{asmVersion.Major}.{asmVersion.Minor}.{asmVersion.Build}";
		}
		catch
		{
			return "0.0.0";
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 06 / 39：RemoteDesktopPlus.Services/Log.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.IO;
using System.Text;

/// <summary>
/// 轻量文件日志：落 <c>%LOCALAPPDATA%\RemoteDesktopPlus\logs\log-YYYYMMDD.log</c>。
/// 进程内全局互斥串行写，崩溃/导入回滚失败等路径也能留下痕迹。
/// Level: Debug=0 Info=1 Warn=2 Error=3；默认记录 Info 及以上。
/// </summary>
public static class Log
{
	private const int RetainDays = 30;

	private static readonly object _gate = new object();

	/// <summary>CleanupOldLogs 重入标记：避免日志写入与清理相互递归。</summary>
	private static bool _inCleanup;

	/// <summary>日志目录。独立属性便于测试清理与用户定位。</summary>
	public static string LogDirectory { get; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"RemoteDesktopPlus", "logs");

	public static void Debug(string message) => Write("DBG", message, null);

	public static void Info(string message) => Write("INF", message, null);

	public static void Warn(string message, Exception? ex = null) => Write("WRN", message, ex);

	public static void Error(string message, Exception? ex = null) => Write("ERR", message, ex);

	private static void Write(string level, string message, Exception? ex)
	{
		try
		{
			lock (_gate)
			{
				Directory.CreateDirectory(LogDirectory);
				CleanupOldLogs();
				string file = Path.Combine(LogDirectory, $"log-{DateTime.Now:yyyyMMdd}.log");
				var sb = new StringBuilder();
				sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
					.Append(' ').Append(level).Append(' ').AppendLine(message);
				if (ex != null)
				{
					sb.AppendLine(ex.ToString());
				}
				File.AppendAllText(file, sb.ToString(), Encoding.UTF8);
			}
		}
		catch
		{
			// 日志自身失败绝不影响主流程。
		}
	}

	/// <summary>
	/// 清理超过保留期的旧日志。逐文件独立处理：单个文件出问题不能中断整轮清理。
	/// </summary>
	private static void CleanupOldLogs()
	{
		// 重入保护：Log.Write 内部会调用本方法，失败路径又要写日志，
		// 不加保护会形成 Write → CleanupOldLogs → Write 的无限递归。
		if (_inCleanup)
		{
			return;
		}
		_inCleanup = true;
		try
		{
			DateTime cutoff = DateTime.Today.AddDays(-RetainDays);
			foreach (string file in Directory.EnumerateFiles(LogDirectory, "log-*.log"))
			{
				try
				{
					string name = Path.GetFileNameWithoutExtension(file); // log-YYYYMMDD
					// 文件名形如 "log-20261106"：前缀 "log-" 占 4 字符，
					// 日期从下标 4 开始、占 8 字符。原实现写的是 AsSpan(5, 8)，
					// 而 5 + 8 = 13 > 长度 12 → AsSpan 抛 ArgumentOutOfRangeException，
					// 异常又发生在 && 左侧（TryParseExact 尚未执行），
					// 直接中断整个 foreach，导致所有旧日志都清不掉。
					if (name.Length == 12
						&& DateTime.TryParseExact(name.AsSpan(4, 8), "yyyyMMdd", null,
							System.Globalization.DateTimeStyles.None, out DateTime date)
						&& date < cutoff)
					{
						File.Delete(file);
					}
				}
				catch (Exception ex)
				{
					// 单个文件失败只跳过该文件，不影响其余文件。
					// 记到日志时不能走 Log.Write（会再次触发本方法），
					// 因此直接忽略异常详情，保证清理逻辑可继续。
					Log.Warn("清理单个旧日志失败: " + file + " (" + ex.Message + ")");
				}
			}
		}
		catch (Exception ex)
		{
			// 目录枚举本身失败（如目录不可访问）：下次再试。
			Log.Warn("清理旧日志失败: " + ex.Message);
		}
		finally
		{
			_inCleanup = false;
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 07 / 39：RemoteDesktopPlus.Services/DatabaseService.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using RemoteDesktopPlus.Models;

public sealed class DatabaseService
{
	private readonly struct ConnectionScope : IDisposable
	{
		private readonly bool _owned;

		public SqliteConnection Connection { get; }

		public SqliteTransaction? Transaction { get; }

		public ConnectionScope(SqliteConnection conn, SqliteTransaction? tx, bool owned)
		{
			Connection = conn;
			Transaction = tx;
			_owned = owned;
		}

		public SqliteCommand CreateCommand(string sql)
		{
			SqliteCommand sqliteCommand = new SqliteCommand(sql, Connection);
			if (Transaction != null)
			{
				sqliteCommand.Transaction = Transaction;
			}
			return sqliteCommand;
		}

		public void Dispose()
		{
			if (_owned)
			{
				Connection.Dispose();
			}
		}
	}

	private static readonly Lazy<DatabaseService> _instance = new Lazy<DatabaseService>(() =>
		new DatabaseService(Path.Combine(AppContext.BaseDirectory, "data.db")));

	private readonly string _connectionString;

	[ThreadStatic]
	private static SqliteConnection? _ambientConnection;

	[ThreadStatic]
	private static SqliteTransaction? _ambientTransaction;

	public static DatabaseService Instance => _instance.Value;

	public string DbPath { get; }

	/// <summary>连接串（VACUUM INTO 等引擎内操作需要独立连接时使用）。</summary>
	public string BuildConnectionString() => _connectionString;

	/// <summary>
	/// 以指定库文件创建实例。生产走 <see cref="Instance"/>（AppContext.BaseDirectory\data.db）；
	/// 单元测试用本构造指向临时文件，避免污染真实库。
	/// </summary>
	public DatabaseService(string dbPath)
	{
		DbPath = dbPath;
		_connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = DbPath,
			Mode = SqliteOpenMode.ReadWriteCreate
		}.ToString();
	}

	public void Initialize()
	{
		using ConnectionScope connectionScope = AcquireConnection();
		// WAL + busy_timeout：后台 3 秒轮询、批量在线检测、用户编辑保存、导入导出
		// 会并发读写同一文件。默认的 rollback journal 模式在此场景下直接抛
		// "database is locked"，且该异常会沿 Dispatcher 冒泡导致 GUI 闪退。
		using (SqliteCommand pragma = connectionScope.CreateCommand(
			"PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000; PRAGMA synchronous=NORMAL;"))
		{
			pragma.ExecuteNonQuery();
		}
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("CREATE TABLE IF NOT EXISTS Connections (\r\n    Id              INTEGER PRIMARY KEY AUTOINCREMENT,\r\n    Host            TEXT NOT NULL,\r\n    Port            INTEGER NOT NULL DEFAULT 3389,\r\n    UserName        TEXT NOT NULL DEFAULT '',\r\n    Password        TEXT NOT NULL DEFAULT '',\r\n    Remark          TEXT NOT NULL DEFAULT '',\r\n    SortOrder       INTEGER NOT NULL DEFAULT 0\r\n);\r\n\r\nCREATE TABLE IF NOT EXISTS Settings (\r\n    Key             TEXT PRIMARY KEY,\r\n    Value           TEXT NOT NULL DEFAULT ''\r\n);");
		sqliteCommand.ExecuteNonQuery();
		ApplyMigrations(connectionScope);
	}

	/// <summary>
	/// Schema 版本迁移管线（E1 扩展点）：新迁移 = 向数组追加一个元组，永不修改历史项。
	/// 每个迁移在独立事务内执行并写入 schema.version，失败即中断，避免半迁移状态。
	/// v1 基线 = 上方 CREATE TABLE 所建结构；v2 起均为 ALTER TABLE 追加式扩展。
	/// </summary>
	private static readonly (string Version, string Sql)[] Migrations = {
		("2", "ALTER TABLE Connections ADD COLUMN GroupName TEXT NOT NULL DEFAULT '';\r\n" +
		      "ALTER TABLE Connections ADD COLUMN LastConnectedAt INTEGER NOT NULL DEFAULT 0;\r\n" +
		      "ALTER TABLE Connections ADD COLUMN Protocol TEXT NOT NULL DEFAULT 'rdp';"),
	};

	private void ApplyMigrations(ConnectionScope scope)
	{
		string? raw = null;
		using (SqliteCommand read = scope.CreateCommand("SELECT Value FROM Settings WHERE Key = 'schema.version';"))
		{
			raw = read.ExecuteScalar() as string;
		}
		int current = int.TryParse(raw, out int parsed) ? parsed : 1;
		foreach ((string version, string sql) in Migrations)
		{
			if (!int.TryParse(version, out int target) || target <= current)
			{
				continue;
			}
			// 幂等防护：版本标记缺失但列已存在（如 v1 库被高版本打开过）时跳过重复 ALTER。
			if (ColumnExists(scope, "Connections", "GroupName") && ColumnExists(scope, "Connections", "LastConnectedAt") && ColumnExists(scope, "Connections", "Protocol"))
			{
				RunInTransaction(delegate
				{
					SetSetting("schema.version", version);
				});
				continue;
			}
			RunInTransaction(delegate
			{
				// RunInTransaction 内 AcquireConnection 会复用环境连接，
				// 故迁移 SQL 必须在该事务的连接上执行。
				using ConnectionScope txScope = AcquireConnection();
				using (SqliteCommand apply = txScope.CreateCommand(sql))
				{
					apply.ExecuteNonQuery();
				}
				SetSetting("schema.version", version);
			});
		}
	}

	private static bool ColumnExists(ConnectionScope scope, string table, string column)
	{
		using SqliteCommand command = scope.CreateCommand(
			"SELECT COUNT(*) FROM pragma_table_info($t) WHERE name = $c;");
		command.Parameters.AddWithValue("$t", table);
		command.Parameters.AddWithValue("$c", column);
		return Convert.ToInt64(command.ExecuteScalar() ?? 0L) > 0;
	}

	private ConnectionScope AcquireConnection()
	{
		if (_ambientConnection != null)
		{
			return new ConnectionScope(_ambientConnection, _ambientTransaction, owned: false);
		}
		SqliteConnection sqliteConnection = new SqliteConnection(_connectionString);
		sqliteConnection.Open();
		return new ConnectionScope(sqliteConnection, null, owned: true);
	}

	public List<ConnectionItem> LoadAll()
	{
		List<ConnectionItem> list = new List<ConnectionItem>();
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("SELECT Id, Host, Port, UserName, Password, Remark, SortOrder, GroupName, LastConnectedAt, Protocol FROM Connections ORDER BY SortOrder ASC, Id ASC;");
		using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
		int num = 0;
		while (sqliteDataReader.Read())
		{
			ConnectionItem obj = new ConnectionItem
			{
				Id = sqliteDataReader.GetInt64(0),
				Host = sqliteDataReader.GetString(1),
				Port = sqliteDataReader.GetInt32(2),
				UserName = sqliteDataReader.GetString(3),
				EncryptedPassword = sqliteDataReader.GetString(4),
				Remark = sqliteDataReader.GetString(5),
				SortOrder = sqliteDataReader.GetInt32(6),
				GroupName = sqliteDataReader.GetString(7),
				LastConnectedAt = sqliteDataReader.GetInt64(8),
				Protocol = sqliteDataReader.GetString(9)
			};
			num = (obj.Index = num + 1);
			list.Add(obj);
		}
		return list;
	}

	public void Insert(ConnectionItem item)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("INSERT INTO Connections (Host, Port, UserName, Password, Remark, SortOrder, GroupName, LastConnectedAt, Protocol)\r\nVALUES ($host, $port, $user, $pwd, $remark, $sort, $group, $last, $proto);\r\nSELECT last_insert_rowid();");
		sqliteCommand.Parameters.AddWithValue("$host", item.Host);
		sqliteCommand.Parameters.AddWithValue("$port", item.Port);
		sqliteCommand.Parameters.AddWithValue("$user", item.UserName);
		sqliteCommand.Parameters.AddWithValue("$pwd", item.EncryptedPassword ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$remark", item.Remark ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$sort", item.SortOrder);
		sqliteCommand.Parameters.AddWithValue("$group", item.GroupName ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$last", item.LastConnectedAt);
		sqliteCommand.Parameters.AddWithValue("$proto", string.IsNullOrEmpty(item.Protocol) ? "rdp" : item.Protocol);
		item.Id = (long)(sqliteCommand.ExecuteScalar() ?? ((object)0L));
	}

	public void Update(ConnectionItem item)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("UPDATE Connections\r\n   SET Host = $host, Port = $port, UserName = $user,\r\n       Password = $pwd, Remark = $remark, SortOrder = $sort,\r\n       GroupName = $group, LastConnectedAt = $last, Protocol = $proto\r\n WHERE Id = $id;");
		sqliteCommand.Parameters.AddWithValue("$host", item.Host);
		sqliteCommand.Parameters.AddWithValue("$port", item.Port);
		sqliteCommand.Parameters.AddWithValue("$user", item.UserName);
		sqliteCommand.Parameters.AddWithValue("$pwd", item.EncryptedPassword ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$remark", item.Remark ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$sort", item.SortOrder);
		sqliteCommand.Parameters.AddWithValue("$group", item.GroupName ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$last", item.LastConnectedAt);
		sqliteCommand.Parameters.AddWithValue("$proto", string.IsNullOrEmpty(item.Protocol) ? "rdp" : item.Protocol);
		sqliteCommand.Parameters.AddWithValue("$id", item.Id);
		sqliteCommand.ExecuteNonQuery();
	}

	/// <summary>
	/// 仅更新 Password 列（P2-5 优化）：主密码全量重加密时避免重写其余 6 列，
	/// 显著缩小事务体积与 journal 写放大。
	/// </summary>
	public void UpdatePasswordColumn(long id, string encryptedPassword)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("UPDATE Connections SET Password = $pwd WHERE Id = $id;");
		sqliteCommand.Parameters.AddWithValue("$pwd", encryptedPassword ?? string.Empty);
		sqliteCommand.Parameters.AddWithValue("$id", id);
		sqliteCommand.ExecuteNonQuery();
	}

	/// <summary>连接成功后回写最后连接时间（Unix 秒）。</summary>
	public void UpdateLastConnectedAt(long id, long unixSeconds)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("UPDATE Connections SET LastConnectedAt = $ts WHERE Id = $id;");
		sqliteCommand.Parameters.AddWithValue("$ts", unixSeconds);
		sqliteCommand.Parameters.AddWithValue("$id", id);
		sqliteCommand.ExecuteNonQuery();
	}

	public void Delete(long id)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("DELETE FROM Connections WHERE Id = $id;");
		sqliteCommand.Parameters.AddWithValue("$id", id);
		sqliteCommand.ExecuteNonQuery();
	}

	public void UpdateSortOrders(IEnumerable<ConnectionItem> items)
	{
		IEnumerable<ConnectionItem> items2 = items;
		RunInTransaction(delegate
		{
			using ConnectionScope connectionScope = AcquireConnection();
			using SqliteCommand sqliteCommand = connectionScope.CreateCommand("UPDATE Connections SET SortOrder = $sort WHERE Id = $id;");
			SqliteParameter sqliteParameter = sqliteCommand.Parameters.Add("$sort", SqliteType.Integer);
			SqliteParameter sqliteParameter2 = sqliteCommand.Parameters.Add("$id", SqliteType.Integer);
			foreach (ConnectionItem item in items2)
			{
				sqliteParameter.Value = item.SortOrder;
				sqliteParameter2.Value = item.Id;
				sqliteCommand.ExecuteNonQuery();
			}
		});
	}

	public string? GetSetting(string key)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("SELECT Value FROM Settings WHERE Key = $k;");
		sqliteCommand.Parameters.AddWithValue("$k", key);
		return sqliteCommand.ExecuteScalar() as string;
	}

	public void SetSetting(string key, string value)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("INSERT INTO Settings (Key, Value) VALUES ($k, $v)\r\nON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;");
		sqliteCommand.Parameters.AddWithValue("$k", key);
		sqliteCommand.Parameters.AddWithValue("$v", value);
		sqliteCommand.ExecuteNonQuery();
	}

	public void DeleteSetting(string key)
	{
		using ConnectionScope connectionScope = AcquireConnection();
		using SqliteCommand sqliteCommand = connectionScope.CreateCommand("DELETE FROM Settings WHERE Key = $k;");
		sqliteCommand.Parameters.AddWithValue("$k", key);
		sqliteCommand.ExecuteNonQuery();
	}

	public void RunInTransaction(Action action)
	{
		if (_ambientConnection != null)
		{
			action();
			return;
		}
		SqliteConnection sqliteConnection = new SqliteConnection(_connectionString);
		sqliteConnection.Open();
		using SqliteTransaction sqliteTransaction = sqliteConnection.BeginTransaction();
		_ambientConnection = sqliteConnection;
		_ambientTransaction = sqliteTransaction;
		try
		{
			action();
			sqliteTransaction.Commit();
		}
		catch
		{
			try
			{
				sqliteTransaction.Rollback();
			}
			catch
			{
			}
			throw;
		}
		finally
		{
			_ambientConnection = null;
			_ambientTransaction = null;
			sqliteConnection.Dispose();
		}
	}

	public bool HasLaunchPassword()
	{
		if (!string.IsNullOrEmpty(GetSetting(SettingKeys.LaunchPasswordHash)))
		{
			return !string.IsNullOrEmpty(GetSetting(SettingKeys.LaunchPasswordSalt));
		}
		return false;
	}

	public void ReinitializeAfterImport()
	{
		Initialize();
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 追加区段：RemoteDesktopPlus.Services/GroupOptionStore.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// 分组级连接配置覆盖。
/// <para>
/// 解决的问题：原先分辨率/盘符/剪贴板是**全局一份**，同时连「客户 A」（要 1920*1080、只共享 C 盘）
/// 和「内网测试」（要全屏、共享全部盘）时必须反复改全局配置，互相干扰。
/// </para>
/// <para>
/// 设计：只存**与全局不同的差异项**（null 表示继承全局），避免把全局值复制一份导致
/// 「改了全局但组内还是旧值」这类隐蔽不一致。
/// </para>
/// </summary>
public sealed class GroupOptions
{
	/// <summary>为空表示继承全局设置。</summary>
	public string? Resolution { get; set; }

	/// <summary>为空表示继承全局设置。</summary>
	public bool? RedirectClipboard { get; set; }

	/// <summary>为空表示继承全局设置。</summary>
	public bool? RedirectLocalDrive { get; set; }

	/// <summary>为空表示继承全局；`*` 表示全部盘符。</summary>
	public string? LocalDriveLetter { get; set; }
}

/// <summary>
/// 分组配置的读写门面：以 JSON 存在 Settings 表的一个键里。
/// 用单个 JSON 键而非新建表 —— 配置量小、读写整块、无需迁移。
/// <para>
/// 刻意**不做内存缓存**：配置是单个小 JSON，每次读库开销可忽略；
/// 而缓存会引入「另一个实例改了配置、本实例还读旧值」的跨实例不一致
/// （实测导致分组覆盖不生效，且刷新时机难以判定）。
/// </para>
/// </summary>
public sealed class GroupOptionStore
{
	private readonly DatabaseService _db;

	public GroupOptionStore(DatabaseService db)
	{
		_db = db ?? throw new ArgumentNullException(nameof(db));
	}

	/// <summary>读取指定分组的覆盖配置；无配置返回 null（全继承全局）。</summary>
	public GroupOptions? Get(string? groupName)
	{
		if (string.IsNullOrWhiteSpace(groupName))
		{
			return null;
		}
		Dictionary<string, GroupOptions> all = Load();
		return all.TryGetValue(groupName.Trim(), out GroupOptions? opt) ? opt : null;
	}

	/// <summary>
	/// 写入/清除指定分组的覆盖。全部字段为空即视为「无覆盖」并移除该组条目，
	/// 避免留下一个空壳条目让人误以为配过。
	/// </summary>
	public void Set(string? groupName, GroupOptions? options)
	{
		if (string.IsNullOrWhiteSpace(groupName))
		{
			return;
		}
		string key = groupName.Trim();
		Dictionary<string, GroupOptions> all = Load();
		if (options == null || IsAllInherited(options))
		{
			if (all.Remove(key))
			{
				Save(all);
			}
			return;
		}
		all[key] = options;
		Save(all);
	}

	/// <summary>列出所有已配置覆盖的分组名（用于 UI 提示）。</summary>
	public IReadOnlyList<string> GroupNames()
	{
		return new List<string>(Load().Keys);
	}

	private static bool IsAllInherited(GroupOptions o)
	{
		return o.Resolution == null
			&& o.RedirectClipboard == null
			&& o.RedirectLocalDrive == null
			&& o.LocalDriveLetter == null;
	}

	private Dictionary<string, GroupOptions> Load()
	{
		string raw = _db.GetSetting(SettingKeys.OptionGroupOverrides);
		if (string.IsNullOrWhiteSpace(raw))
		{
			return new Dictionary<string, GroupOptions>(StringComparer.OrdinalIgnoreCase);
		}
		try
		{
			Dictionary<string, GroupOptions>? parsed = JsonSerializer
				.Deserialize<Dictionary<string, GroupOptions>>(raw);
			if (parsed == null)
			{
				return new Dictionary<string, GroupOptions>(StringComparer.OrdinalIgnoreCase);
			}
			// 反序列化出来的字典比较器默认是 Ordinal，统一改成忽略大小写，
			// 避免「TEST」与「test」被当成两个组。
			return new Dictionary<string, GroupOptions>(parsed, StringComparer.OrdinalIgnoreCase);
		}
		catch (JsonException ex)
		{
			// 配置损坏不该让整个程序起不来：丢弃坏配置并按空处理，同时记日志便于排查。
			Log.Warn("分组配置 JSON 解析失败，按空配置处理: " + ex.Message);
			return new Dictionary<string, GroupOptions>(StringComparer.OrdinalIgnoreCase);
		}
	}

	private void Save(Dictionary<string, GroupOptions> all)
	{
		_db.SetSetting(SettingKeys.OptionGroupOverrides, JsonSerializer.Serialize(all));
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 08 / 39：RemoteDesktopPlus.Services/CryptoService.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RemoteDesktopPlus.Models;

/// <summary>
/// 凭据加解密与主密码/启动密码管理。
/// 密文前缀语义：<c>D1$</c> = DPAPI(CurrentUser)，<c>M1$</c> = 主密码派生的 AES-256-GCM。
/// </summary>
public sealed class CryptoService
{
	private static readonly Lazy<CryptoService> _instance =
		new Lazy<CryptoService>(() => new CryptoService(DatabaseService.Instance));

	private const string PrefixDpapi = "D1$";

	private const string PrefixMaster = "M1$";

	private const int Pbkdf2Iter = 200000;

	private const int KeyBytes = 32;

	private const int SaltBytes = 16;

	private const int NonceBytes = 12;

	private const int TagBytes = 16;

	private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("RemoteDesktopPlus.Salt.v1");

	private readonly DatabaseService _db;

	private byte[]? _masterKey;

	/// <summary>生产环境默认实例（绑定 <see cref="DatabaseService.Instance"/>）。</summary>
	public static CryptoService Instance => _instance.Value;

	public bool HasMasterKey => _masterKey != null;

	/// <summary>以指定的存储创建实例，便于单元测试注入临时库。</summary>
	public CryptoService(DatabaseService db)
	{
		_db = db ?? throw new ArgumentNullException(nameof(db));
	}

	public bool IsMasterPasswordConfigured()
	{
		return !string.IsNullOrEmpty(_db.GetSetting(SettingKeys.MasterKeyVerifier));
	}

	public bool Unlock(string masterPassword)
	{
		string saltB64 = _db.GetSetting(SettingKeys.MasterKeySalt);
		string verifier = _db.GetSetting(SettingKeys.MasterKeyVerifier);
		if (string.IsNullOrEmpty(saltB64) || string.IsNullOrEmpty(verifier))
		{
			return false;
		}
		try
		{
			// Base64 解码必须在 try 内：salt 被写坏时 Convert.FromBase64String
			// 抛 FormatException，放在 try 外会一路冒泡到调用方
			// （MasterPasswordWindow 的按钮事件没有 catch）→ WPF 未处理异常 → 应用崩溃。
			byte[] salt = Convert.FromBase64String(saltB64);
			byte[] key = DeriveKey(masterPassword, salt);
			if (AesGcmDecrypt(verifier, key) == "OK")
			{
				_masterKey = key;
				return true;
			}
		}
		catch (FormatException ex)
		{
			// 本地数据已损坏（salt 不是合法 Base64），不是用户密码输错。
			Log.Warn("主密码 salt 解析失败，本地数据可能已损坏", ex);
			return false;
		}
		catch (Exception ex)
		{
			Log.Warn("主密码解锁失败", ex);
			return false;
		}
		return false;
	}

	/// <summary>设置/修改主密码：全量重加密所有条目，事务内完成，失败回滚。</summary>
	/// <remarks>
	/// 任一条目解密失败即整批中止并抛异常：继续执行会用空串覆盖该条目的真实密码，
	/// 造成不可逆的数据丢失。
	/// </remarks>
	public void SetMasterPassword(string newPassword, IEnumerable<ConnectionItem> existingItems)
	{
		var source = CollectDecryptableItems(existingItems);
		byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
		byte[] key = DeriveKey(newPassword, salt);
		string verifier = AesGcmEncrypt("OK", key);
		string saltB64 = Convert.ToBase64String(salt);
		var newCiphers = source
			.Select(s => (s.Item, s.OriginalCipher, NewCipher: PrefixMaster + AesGcmEncrypt(s.Plain, key)))
			.ToList();

		byte[]? previousKey = _masterKey;
		try
		{
			_db.RunInTransaction(delegate
			{
				_db.SetSetting(SettingKeys.MasterKeySalt, saltB64);
				_db.SetSetting(SettingKeys.MasterKeyVerifier, verifier);
				foreach (var entry in newCiphers)
				{
					entry.Item.EncryptedPassword = entry.NewCipher;
					// 仅写 Password 列：全字段 UPDATE 会把 Host/Port 等不变列一并重写（写放大）。
					_db.UpdatePasswordColumn(entry.Item.Id, entry.NewCipher);
				}
			});
			_masterKey = key;
		}
		catch
		{
			foreach (var entry in newCiphers)
			{
				entry.Item.EncryptedPassword = entry.OriginalCipher;
			}
			_masterKey = previousKey;
			throw;
		}
	}

	/// <summary>移除主密码：回退为 DPAPI 形态，事务内完成，失败回滚。</summary>
	/// <remarks>
	/// 与 <see cref="SetMasterPassword"/> 同理：任一条目解密失败即整批中止。
	/// </remarks>
	public void RemoveMasterPassword(IEnumerable<ConnectionItem> existingItems)
	{
		if (!HasMasterKey)
		{
			throw new InvalidOperationException("请先解锁。");
		}
		var source = CollectDecryptableItems(existingItems);
		var newCiphers = source
			.Select(s => (s.Item, s.OriginalCipher, NewCipher: PrefixDpapi + DpapiEncrypt(s.Plain)))
			.ToList();

		byte[]? previousKey = _masterKey;
		try
		{
			_db.RunInTransaction(delegate
			{
				_db.DeleteSetting(SettingKeys.MasterKeySalt);
				_db.DeleteSetting(SettingKeys.MasterKeyVerifier);
				foreach (var entry in newCiphers)
				{
					entry.Item.EncryptedPassword = entry.NewCipher;
					// 与 SetMasterPassword 一致：仅写 Password 列，避免写放大。
					_db.UpdatePasswordColumn(entry.Item.Id, entry.NewCipher);
				}
			});
			_masterKey = null;
		}
		catch
		{
			foreach (var entry in newCiphers)
			{
				entry.Item.EncryptedPassword = entry.OriginalCipher;
			}
			_masterKey = previousKey;
			throw;
		}
	}

	public string Encrypt(string plain)
	{
		if (string.IsNullOrEmpty(plain))
		{
			return string.Empty;
		}
		if (_masterKey != null)
		{
			return PrefixMaster + AesGcmEncrypt(plain, _masterKey);
		}
		return PrefixDpapi + DpapiEncrypt(plain);
	}

	/// <summary>解密失败一律返回空串，不抛出（调用方无需处理损坏密文）。</summary>
	/// <remarks>
	/// 注意：返回空串有歧义——「密码本就为空」与「解密失败」无法区分。
	/// 需要区分二者的场景（主密码批量重加密等会写回数据的路径）必须改用
	/// <see cref="TryDecrypt"/>，否则会用空串覆盖真实密码，造成不可逆的数据丢失。
	/// </remarks>
	public string Decrypt(string cipher)
	{
		return TryDecrypt(cipher, out string plain) ? plain : string.Empty;
	}

	/// <summary>
	/// 收集可解密条目：任一条目解密失败即抛异常中止整批操作。
	/// 批量重加密路径必须先过这一关，否则失败条目会被空串覆盖，数据不可逆丢失。
	/// </summary>
	public List<(ConnectionItem Item, string OriginalCipher, string Plain)> CollectDecryptableItems(IEnumerable<ConnectionItem> existingItems)
	{
		var result = new List<(ConnectionItem, string, string)>();
		var failed = new List<string>();
		foreach (var item in existingItems)
		{
			string cipher = item.EncryptedPassword ?? string.Empty;
			if (TryDecrypt(cipher, out string plain))
			{
				result.Add((item, cipher, plain));
			}
			else
			{
				failed.Add(string.IsNullOrEmpty(item.Host) ? "(无主机名)" : item.Host);
			}
		}
		if (failed.Count > 0)
		{
			throw new InvalidOperationException(
				"有 " + failed.Count + " 条记录无法解密，操作已取消（未做任何修改）：" +
				string.Join("、", failed.Take(5)) +
				(failed.Count > 5 ? " 等" : string.Empty) +
				"。常见原因：主密码已变更、DPAPI 凭据因换机/换用户失效。");
		}
		return result;
	}

	/// <summary>
	/// 可失败语义的解密：成功返回 true 并输出明文，失败返回 false。
	/// 用于「解不开就整批中止」的数据写回路径，避免静默用空串覆盖密文。
	/// </summary>
	public bool TryDecrypt(string cipher, out string plain)
	{
		plain = string.Empty;
		if (string.IsNullOrEmpty(cipher))
		{
			// 空密文视为「本就没有密码」，不是失败。
			return true;
		}
		try
		{
			if (cipher.StartsWith(PrefixMaster, StringComparison.Ordinal))
			{
				if (_masterKey == null)
				{
					// 主密码形态但未解锁：无法解密，判失败。
					return false;
				}
				plain = AesGcmDecrypt(cipher.Substring(PrefixMaster.Length), _masterKey);
				return true;
			}
			if (cipher.StartsWith(PrefixDpapi, StringComparison.Ordinal))
			{
				plain = DpapiDecrypt(cipher.Substring(PrefixDpapi.Length));
				return true;
			}
			plain = DpapiDecrypt(cipher);
			return true;
		}
		catch
		{
			plain = string.Empty;
			return false;
		}
	}

	public static string HashLaunchPassword(string password, out string saltB64)
	{
		byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
		saltB64 = Convert.ToBase64String(salt);
		return Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iter, HashAlgorithmName.SHA256, KeyBytes));
	}

	/// <summary>定时安全比较，防时序侧信道。</summary>
	public static bool VerifyLaunchPassword(string password, string hashB64, string saltB64)
	{
		try
		{
			byte[] salt = Convert.FromBase64String(saltB64);
			byte[] expected = Convert.FromBase64String(hashB64);
			byte[] actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iter, HashAlgorithmName.SHA256, KeyBytes);
			return CryptographicOperations.FixedTimeEquals(actual, expected);
		}
		catch
		{
			return false;
		}
	}

	private static byte[] DeriveKey(string password, byte[] salt)
	{
		return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Pbkdf2Iter, HashAlgorithmName.SHA256, KeyBytes);
	}

	private static string AesGcmEncrypt(string plain, byte[] key)
	{
		byte[] nonce = RandomNumberGenerator.GetBytes(NonceBytes);
		byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
		byte[] cipher = new byte[plainBytes.Length];
		byte[] tag = new byte[TagBytes];
		using (AesGcm aes = new AesGcm(key, TagBytes))
		{
			aes.Encrypt(nonce, plainBytes, cipher, tag);
		}
		// 装机格式：nonce | tag | cipher，再整体 Base64
		byte[] packed = new byte[nonce.Length + tag.Length + cipher.Length];
		Buffer.BlockCopy(nonce, 0, packed, 0, nonce.Length);
		Buffer.BlockCopy(tag, 0, packed, nonce.Length, tag.Length);
		Buffer.BlockCopy(cipher, 0, packed, nonce.Length + tag.Length, cipher.Length);
		return Convert.ToBase64String(packed);
	}

	private static string AesGcmDecrypt(string cipherB64, byte[] key)
	{
		byte[] packed = Convert.FromBase64String(cipherB64);
		if (packed.Length < NonceBytes + TagBytes)
		{
			throw new CryptographicException("密文长度不足");
		}
		byte[] nonce = new byte[NonceBytes];
		byte[] tag = new byte[TagBytes];
		byte[] cipher = new byte[packed.Length - NonceBytes - TagBytes];
		Buffer.BlockCopy(packed, 0, nonce, 0, NonceBytes);
		Buffer.BlockCopy(packed, NonceBytes, tag, 0, TagBytes);
		Buffer.BlockCopy(packed, NonceBytes + TagBytes, cipher, 0, cipher.Length);
		byte[] plain = new byte[cipher.Length];
		using (AesGcm aes = new AesGcm(key, TagBytes))
		{
			aes.Decrypt(nonce, cipher, tag, plain);
		}
		return Encoding.UTF8.GetString(plain);
	}

	private static string DpapiEncrypt(string plain)
	{
		return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), DpapiEntropy, DataProtectionScope.CurrentUser));
	}

	private static string DpapiDecrypt(string b64)
	{
		byte[] bytes = ProtectedData.Unprotect(Convert.FromBase64String(b64), DpapiEntropy, DataProtectionScope.CurrentUser);
		return Encoding.UTF8.GetString(bytes);
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 09 / 39：RemoteDesktopPlus.Services/CredentialStore.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Windows 凭据管理器（Credential Manager）的编程式读写。
/// 用途：替代 <c>cmdkey.exe /pass:&lt;明文&gt;</c>——后者会把密码暴露在进程命令行上，
/// 任何进程都能通过 Win32_Process.CommandLine / 任务管理器看到。
/// 本实现等价于 cmdkey 的内部行为：CRED_TYPE_GENERIC + TargetName=TERMSRV/&lt;host&gt;，
/// CredentialBlob 为该密码的 UTF-16LE 字节。
/// </summary>
public static class CredentialStore
{
	private const int CRED_TYPE_GENERIC = 1;

	/// <summary>凭据仅在当前登录会话内有效，注销即失效（不落盘为长期凭据）。</summary>
	private const int CRED_PERSIST_SESSION = 1;

	private const int ERROR_NOT_FOUND = 1168;

	[StructLayout(LayoutKind.Sequential)]
	private struct FILETIME
	{
		public uint dwLowDateTime;

		public uint dwHighDateTime;
	}

	/// <remarks>
	/// 字段顺序与 wincred.h 的 CREDENTIAL 一致；LastWritten 用 FILETIME（4 字节对齐）而非 long，
	/// 否则 x64 下后续字段会整体偏移 4 字节，导致 CredWrite 失败。
	/// </remarks>
	[StructLayout(LayoutKind.Sequential)]
	private struct CREDENTIAL
	{
		public int Flags;

		public int Type;

		public IntPtr TargetName;

		public IntPtr Comment;

		public FILETIME LastWritten;

		public int CredentialBlobSize;

		public IntPtr CredentialBlob;

		public int Persist;

		public int AttributeCount;

		public IntPtr Attributes;

		public IntPtr TargetAlias;

		public IntPtr UserName;
	}

	[DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredWrite(ref CREDENTIAL credential, int flags);

	[DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CredDelete(string target, int type, int flags);

	/// <summary>写入通用凭据。失败返回 false，不抛异常。</summary>
	public static bool WriteGeneric(string target, string userName, string password)
	{
		if (string.IsNullOrEmpty(target))
		{
			return false;
		}
		IntPtr targetPtr = IntPtr.Zero;
		IntPtr userPtr = IntPtr.Zero;
		IntPtr blobPtr = IntPtr.Zero;
		int blobSize = 0;
		try
		{
			byte[] blob = System.Text.Encoding.Unicode.GetBytes(password ?? string.Empty);
			blobSize = blob.Length;
			targetPtr = Marshal.StringToHGlobalUni(target);
			userPtr = Marshal.StringToHGlobalUni(userName ?? string.Empty);
			blobPtr = Marshal.AllocHGlobal(blob.Length);
			Marshal.Copy(blob, 0, blobPtr, blob.Length);

			CREDENTIAL credential = new CREDENTIAL
			{
				Flags = 0,
				Type = CRED_TYPE_GENERIC,
				TargetName = targetPtr,
				Comment = IntPtr.Zero,
				CredentialBlobSize = blob.Length,
				CredentialBlob = blobPtr,
				Persist = CRED_PERSIST_SESSION,
				AttributeCount = 0,
				Attributes = IntPtr.Zero,
				TargetAlias = IntPtr.Zero,
				UserName = userPtr
			};
			return CredWrite(ref credential, 0);
		}
		catch
		{
			return false;
		}
		finally
		{
			if (blobPtr != IntPtr.Zero)
			{
				// 先清零再释放，避免密码残留在未回收堆块里。
				for (int i = 0; i < blobSize; i++)
				{
					Marshal.WriteByte(blobPtr, i, 0);
				}
				Marshal.FreeHGlobal(blobPtr);
			}
			if (userPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(userPtr);
			}
			if (targetPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(targetPtr);
			}
		}
	}

	/// <summary>删除通用凭据。已不存在时视为成功（与 cmdkey /delete 幂等语义一致）。</summary>
	public static bool DeleteGeneric(string target)
	{
		if (string.IsNullOrEmpty(target))
		{
			return false;
		}
		try
		{
			if (CredDelete(target, CRED_TYPE_GENERIC, 0))
			{
				return true;
			}
			return Marshal.GetLastWin32Error() == ERROR_NOT_FOUND;
		}
		catch
		{
			return false;
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 10 / 39：RemoteDesktopPlus.Services/IRdpBackend.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System.Diagnostics;

/// <summary>
/// 与 RDP 客户端进程及其凭据存储交互的抽象层。
/// 存在的意义有二：一是把 mstsc.exe / 凭据写入隔离开，便于换后端；
/// 二是让测试能断言「密码只进安全通道，绝不进进程命令行」——
/// 见 <see cref="WindowsRdpBackend.StartClient"/> 只接受 .rdp 文件路径，参数面天然不含密码。
/// </summary>
public interface IRdpBackend
{
	/// <summary>把凭据写入系统凭据存储（等价于 cmdkey /generic，但不经命令行）。</summary>
	bool StoreCredential(string target, string userName, string password);

	/// <summary>从系统凭据存储删除指定 target 的凭据。</summary>
	void DeleteCredential(string target);

	/// <summary>以给定的 .rdp 文件启动远程桌面客户端，返回进程句柄供窗口管理跟踪（启动失败返回 null）。</summary>
	Process? StartClient(string rdpFilePath);
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 11 / 39：RemoteDesktopPlus.Services/WindowsRdpBackend.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Windows 平台的 RDP 后端：凭据走 <see cref="CredentialStore"/>（CredWriteW），
/// 客户端走 mstsc.exe。
/// 关键安全性质：本类所有进程启动的参数面都**不含密码**——
/// 密码只经 CredentialStore 写入系统凭据存储。
/// </summary>
public sealed class WindowsRdpBackend : IRdpBackend
{
	public static WindowsRdpBackend Instance { get; } = new WindowsRdpBackend();

	/// <summary>cmdkey 的 target 命名约定：TERMSRV/&lt;host&gt;，mstsc 只认这个前缀。</summary>
	public static string BuildTarget(string host)
	{
		return "TERMSRV/" + host;
	}

	public bool StoreCredential(string target, string userName, string password)
	{
		return CredentialStore.WriteGeneric(target, userName, password);
	}

	public void DeleteCredential(string target)
	{
		CredentialStore.DeleteGeneric(target);
	}

	public Process? StartClient(string rdpFilePath)
	{
		return Process.Start(new ProcessStartInfo
		{
			FileName = "mstsc.exe",
			UseShellExecute = true,
			ArgumentList = { rdpFilePath }
		});
	}
}

/// <summary>
/// mstsc 窗口管理器：连接建立后把窗口在所在显示器工作区居中、把外框尺寸钉死为
/// 「.rdp 桌面分辨率 + 非客户区」（拖动只平移、宽高比与尺寸恒定，杜绝被自动缩放）、
/// 并支持在远程窗口上右键粘贴（吞掉右键并注入 Shift+Insert）。
/// <para>
/// 只操作窗口层，不碰 .rdp 内容——v1.1.0~v1.2.1 改 .rdp 视频参数导致画面模糊整体回退的
/// 教训：窗口行为与画面参数必须分层隔离。
/// </para>
/// <para>
/// 生命周期：<see cref="Track"/> 由 UI 线程调用（Connect → Launch → Track）；钩子必须装在
/// 带消息循环的线程上，装失败仅降级（不右键粘贴 / 靠轮询钉尺寸），不影响连接本身。
/// </para>
/// </summary>
public static class MstscWindowManager
{
	// ── 纯逻辑（单测覆盖，不含 Win32）──────────────────────────────────

	/// <summary>归一化 .rdp 桌面尺寸：宽高须同时为正，否则视为全屏（不锁尺寸不居中）。</summary>
	public static (int? Width, int? Height) NormalizeDesktopSize(int? width, int? height)
	{
		bool ok = width.HasValue && width.Value > 0 && height.HasValue && height.Value > 0;
		return ok ? (width, height) : (null, null);
	}

	/// <summary>目标外框 = 桌面分辨率 + 非客户区（边框 + 标题栏）；帧为负时钳 0，结果至少 1px。</summary>
	public static (int Width, int Height) ComputeTargetOuterSize(int desktopWidth, int desktopHeight, int frameWidth, int frameHeight)
	{
		int w = desktopWidth + (frameWidth > 0 ? frameWidth : 0);
		int h = desktopHeight + (frameHeight > 0 ? frameHeight : 0);
		return (w < 1 ? 1 : w, h < 1 ? 1 : h);
	}

	/// <summary>
	/// 工作区内居中坐标。窗口比工作区大时钳到原点——纯居中会把标题栏顶出屏幕，
	/// 用户将永远拖不动窗口。
	/// </summary>
	public static (int X, int Y) ComputeCenteredPosition(int workX, int workY, int workWidth, int workHeight, int outerWidth, int outerHeight)
	{
		int x = workX + (workWidth - outerWidth) / 2;
		int y = workY + (workHeight - outerHeight) / 2;
		if (x < workX)
		{
			x = workX;
		}
		if (y < workY)
		{
			y = workY;
		}
		return (x, y);
	}

	// ── 会话跟踪 ─────────────────────────────────────────────────────

	private sealed class Session
	{
		public Process? Proc;

		public IntPtr Hwnd;

		public int? DesktopWidth;

		public int? DesktopHeight;

		public bool PasteEnabled;

		public bool Centered;

		/// <summary>发现主窗口的截止时刻（Environment.TickCount64）。</summary>
		public long DeadlineTicks;

		public Timer? Timer;
	}

	private const int FindWindowTimeoutMs = 30000;

	private const int FindPollIntervalMs = 200;

	/// <summary>兜底轮询间隔：实时修正由 WinEvent 钩子负责，轮询只兜钩子丢失。</summary>
	private const int PinPollIntervalMs = 500;

	private static readonly object Gate = new object();

	private static readonly List<Session> Sessions = new List<Session>();

	/// <summary>当前跟踪的会话数（单测与诊断用）。</summary>
	public static int ActiveSessionCount
	{
		get
		{
			lock (Gate)
			{
				return Sessions.Count;
			}
		}
	}

	// 钩子回调委托必须持静态引用：被 GC 后钩子仍指向已回收内存（经典 .NET 陷阱）。
	private static LowLevelMouseProc? _mouseProc;

	private static WinEventProc? _winEventProc;

	private static IntPtr _mouseHook;

	private static IntPtr _winEventHook;

	/// <summary>右键按下已被本钩子吞掉、等待配对抬起。</summary>
	private static bool _rightDownSwallowed;

	/// <summary>
	/// 跟踪一个已启动的 mstsc 进程。<c>client</c> 为 null（测试替身 / 启动失败）时无操作。
	/// desktopWidth/Height 来自 .rdp 分辨率；redirectClipboard 决定是否接管右键粘贴。
	/// </summary>
	public static void Track(Process? client, int? desktopWidth, int? desktopHeight, bool redirectClipboard)
	{
		if (client == null)
		{
			return;
		}
		var (w, h) = NormalizeDesktopSize(desktopWidth, desktopHeight);
		// 既不锁尺寸又不开右键粘贴 → 没有可管理的行为，不建会话、不装钩子。
		if (!w.HasValue && !redirectClipboard)
		{
			return;
		}
		try
		{
			Session s = new Session
			{
				Proc = client,
				DesktopWidth = w,
				DesktopHeight = h,
				PasteEnabled = redirectClipboard,
				DeadlineTicks = Environment.TickCount64 + FindWindowTimeoutMs
			};
			s.Timer = new Timer(_ => Tick(s), null, 0, FindPollIntervalMs);
			lock (Gate)
			{
				Sessions.Add(s);
			}
			if (redirectClipboard)
			{
				EnsureMouseHook();
			}
			if (w.HasValue)
			{
				EnsureWinEventHook();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("mstsc 窗口管理启动失败（不影响连接本身）", ex);
		}
	}

	private static void Tick(Session s)
	{
		try
		{
			if (s.Proc == null || s.Proc.HasExited)
			{
				DisposeSession(s);
				return;
			}
			if (s.Hwnd == IntPtr.Zero)
			{
				s.Proc.Refresh();
				IntPtr h = s.Proc.MainWindowHandle;
				if (h == IntPtr.Zero || !IsWindow(h))
				{
					if (Environment.TickCount64 > s.DeadlineTicks)
					{
						Log.Info("mstsc 主窗口未在时限内出现，停止窗口管理（连接本身不受影响）");
						DisposeSession(s);
					}
					return;
				}
				s.Hwnd = h;
			}
			if (!IsWindow(s.Hwnd))
			{
				DisposeSession(s);
				return;
			}
			// 最小化/最大化是用户的显式动作，让行；恢复后下一轮重新钉住。
			if (IsIconic(s.Hwnd) || IsZoomed(s.Hwnd))
			{
				return;
			}
			if (s.DesktopWidth.HasValue)
			{
				ApplySizeAndCenter(s);
			}
			// 首轮成功后放慢轮询：尺寸实时修正由 WinEvent 钩子负责。
			s.Timer?.Change(PinPollIntervalMs, PinPollIntervalMs);
		}
		catch (Exception ex)
		{
			Log.Warn("mstsc 窗口管理轮询异常", ex);
		}
	}

	/// <summary>
	/// 钉尺寸 + 首次居中。帧宽高每轮实测（而非首次记忆）：坏态窗口（如 783×593）与
	/// 好态窗口的帧一致，按当前帧算目标 → 设为目标 → 下一轮测得同样帧，自稳不来回写。
	/// </summary>
	private static void ApplySizeAndCenter(Session s)
	{
		if (s.Hwnd == IntPtr.Zero || !GetWindowRect(s.Hwnd, out RECT wr) || !GetClientRect(s.Hwnd, out RECT cr))
		{
			return;
		}
		int clientW = cr.Right - cr.Left;
		int clientH = cr.Bottom - cr.Top;
		if (clientW <= 0 || clientH <= 0)
		{
			return;
		}
		int frameW = wr.Right - wr.Left - clientW;
		int frameH = wr.Bottom - wr.Top - clientH;
		var (targetW, targetH) = ComputeTargetOuterSize(s.DesktopWidth!.Value, s.DesktopHeight!.Value, frameW, frameH);
		int curW = wr.Right - wr.Left;
		int curH = wr.Bottom - wr.Top;
		if (!s.Centered)
		{
			IntPtr mon = MonitorFromWindow(s.Hwnd, MONITOR_DEFAULTTONEAREST);
			MONITORINFO mi = default;
			mi.cbSize = Marshal.SizeOf<MONITORINFO>();
			if (GetMonitorInfo(mon, ref mi))
			{
				var (x, y) = ComputeCenteredPosition(mi.rcWork.Left, mi.rcWork.Top,
					mi.rcWork.Right - mi.rcWork.Left, mi.rcWork.Bottom - mi.rcWork.Top, targetW, targetH);
				SetWindowPos(s.Hwnd, IntPtr.Zero, x, y, targetW, targetH, SWP_NOZORDER | SWP_NOACTIVATE);
				s.Centered = true;
				return;
			}
			// 取不到显示器信息：跳过居中，只钉尺寸（下次轮询重试居中）。
		}
		if (curW != targetW || curH != targetH)
		{
			// 只钉尺寸不改坐标：保留用户拖到的位置，宽高比由「外框恒等于目标」保证。
			SetWindowPos(s.Hwnd, IntPtr.Zero, 0, 0, targetW, targetH, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
		}
	}

	private static Session? FindSession(IntPtr hwnd)
	{
		lock (Gate)
		{
			for (int i = 0; i < Sessions.Count; i++)
			{
				if (Sessions[i].Hwnd == hwnd)
				{
					return Sessions[i];
				}
			}
		}
		return null;
	}

	private static bool IsTrackedRoot(IntPtr root)
	{
		lock (Gate)
		{
			for (int i = 0; i < Sessions.Count; i++)
			{
				if (Sessions[i].PasteEnabled && Sessions[i].Hwnd == root)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void DisposeSession(Session s)
	{
		lock (Gate)
		{
			if (!Sessions.Remove(s))
			{
				return; // 并发 Tick 只清理一次
			}
		}
		try
		{
			s.Timer?.Dispose();
		}
		catch
		{
		}
		try
		{
			s.Proc?.Dispose(); // 只释放句柄引用，不结束 mstsc 进程
		}
		catch
		{
		}
		bool pasteAlive = false;
		bool sizeAlive = false;
		lock (Gate)
		{
			foreach (Session x in Sessions)
			{
				pasteAlive |= x.PasteEnabled;
				sizeAlive |= x.DesktopWidth.HasValue;
			}
		}
		if (!pasteAlive && _mouseHook != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_mouseHook);
			_mouseHook = IntPtr.Zero;
		}
		if (!sizeAlive && _winEventHook != IntPtr.Zero)
		{
			UnhookWinEvent(_winEventHook);
			_winEventHook = IntPtr.Zero;
		}
	}

	// ── 钩子安装（须在带消息循环的 UI 线程调用；失败仅降级） ──────────────

	private static void EnsureMouseHook()
	{
		if (_mouseHook != IntPtr.Zero)
		{
			return;
		}
		_mouseProc ??= MouseHookCallback;
		_mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, IntPtr.Zero, 0);
		if (_mouseHook == IntPtr.Zero)
		{
			Log.Warn("右键粘贴钩子安装失败，右键保持原生行为");
		}
	}

	private static void EnsureWinEventHook()
	{
		if (_winEventHook != IntPtr.Zero)
		{
			return;
		}
		_winEventProc ??= WinEventCallback;
		_winEventHook = SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE,
			IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
		if (_winEventHook == IntPtr.Zero)
		{
			Log.Warn("尺寸实时修正钩子安装失败，降级为 500ms 轮询");
		}
	}

	// ── WinEvent：窗口移动/缩放瞬间把尺寸钉回去（轮询的实时补充） ─────────

	private static void WinEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		// OBJID_WINDOW=0：只处理窗口自身的位置变化，忽略子对象。
		if (eventType != EVENT_OBJECT_LOCATIONCHANGE || hwnd == IntPtr.Zero || idObject != 0)
		{
			return;
		}
		try
		{
			Session? s = FindSession(hwnd);
			if (s == null || !s.DesktopWidth.HasValue || !s.Centered)
			{
				return;
			}
			if (IsIconic(hwnd) || IsZoomed(hwnd))
			{
				return;
			}
			// 我们自己的 SetWindowPos 也会触发本回调：尺寸已对时下一行判断直接返回，无递归。
			ApplySizeAndCenter(s);
		}
		catch
		{
			// 钩子回调异常绝不能外抛（会打断宿主消息循环）
		}
	}

	// ── 鼠标钩子：远程窗口上的右键 = 粘贴 ────────────────────────────────

	private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && (wParam == (IntPtr)WM_RBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONUP))
		{
			MSLLHOOKSTRUCT data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
			bool down = wParam == (IntPtr)WM_RBUTTONDOWN;
			if (down)
			{
				_rightDownSwallowed = false;
				// Shift+右键 = 逃生门：原样透传，保留远程原生右键语义（上下文菜单等）。
				if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
				{
					return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
				}
				IntPtr root = GetAncestor(WindowFromPoint(data.pt), GA_ROOT);
				if (IsTrackedRoot(root))
				{
					_rightDownSwallowed = true;
					SendPaste(root);
					return (IntPtr)1; // 吞掉按下：不让右键进远程，避免与粘贴叠加弹出菜单
				}
			}
			else if (_rightDownSwallowed)
			{
				_rightDownSwallowed = false;
				return (IntPtr)1; // 配对吞掉抬起，防止远程收到孤立的按钮抬起
			}
		}
		return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
	}

	/// <summary>
	/// 把「粘贴」变成一次 Shift+Insert 键入，交给目标应用自己的粘贴语义处理换行——
	/// 不逐行注入 Enter：多行文本的执行策略（bracketed paste / 多行确认提示 / 逐行执行）
	/// 由目标终端决定，本地代劳必然与各工具语义打架。
	/// 选 Shift+Insert 而非 Ctrl+V：conhost、Windows Terminal、PuTTY、标准编辑框全部原生
	/// 支持；Ctrl+V 在 PuTTY 默认不是粘贴键、老 conhost 还要求「启用 Ctrl 键快捷方式」。
	/// </summary>
	private static void SendPaste(IntPtr hwnd)
	{
		try
		{
			SetForegroundWindow(hwnd); // 键入目标须前台；用户本就点在该窗口，通常已满足
			keybd_event(VK_SHIFT, 0, 0, UIntPtr.Zero);
			Thread.Sleep(10);
			keybd_event(VK_INSERT, 0, 0, UIntPtr.Zero);
			Thread.Sleep(10);
			keybd_event(VK_INSERT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
			Thread.Sleep(10);
			keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
		}
		catch (Exception ex)
		{
			Log.Warn("右键粘贴键入失败", ex);
		}
	}

	// ── Win32 声明 ───────────────────────────────────────────────────

	private const int WH_MOUSE_LL = 14;

	private const int WM_RBUTTONDOWN = 0x0204;

	private const int WM_RBUTTONUP = 0x0205;

	private const int VK_SHIFT = 0x10;

	private const int VK_INSERT = 0x2D;

	private const uint KEYEVENTF_KEYUP = 0x0002;

	private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;

	private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

	private const uint SWP_NOMOVE = 0x0002;

	private const uint SWP_NOZORDER = 0x0004;

	private const uint SWP_NOACTIVATE = 0x0010;

	private const int GA_ROOT = 2;

	private const uint MONITOR_DEFAULTTONEAREST = 2;

	private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

	private delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int X;

		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MSLLHOOKSTRUCT
	{
		public POINT pt;

		public uint mouseData;

		public uint flags;

		public uint time;

		public IntPtr dwExtraInfo;
	}

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool IsZoomed(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

	[DllImport("user32.dll")]
	private static extern IntPtr WindowFromPoint(POINT point);

	[DllImport("user32.dll")]
	private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll")]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int vKey);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 12 / 39：RemoteDesktopPlus.Services/IRdpLauncher.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;

/// <summary>
/// RDP 启动抽象（P1-3 / E5 第一步：实例化）。
/// ViewModel 通过本接口发起连接，测试可注入替身；
/// 将来多协议扩展时按 Protocol 分派不同实现（第二步，见架构报告 E5）。
/// </summary>
public interface IRdpLauncher
{
	/// <summary>发起远程桌面连接（写凭据 → 生成 .rdp → 拉起客户端 → 延迟清理）。</summary>
	void Launch(RdpLauncher.RdpOptions opt);

	/// <summary>清理指定主机的系统凭据（凭证修复功能）。</summary>
	void ClearCredential(string host);
}

/// <summary>生产实现：委托给既有 <see cref="RdpLauncher"/> 静态逻辑，行为不变。</summary>
public sealed class WindowsRdpLauncher : IRdpLauncher
{
	public static readonly WindowsRdpLauncher Instance = new WindowsRdpLauncher();

	public void Launch(RdpLauncher.RdpOptions opt)
	{
		RdpLauncher.Launch(opt);
	}

	public void ClearCredential(string host)
	{
		RdpLauncher.ClearCredential(host);
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 13 / 39：RemoteDesktopPlus.Services/RdpLauncher.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 生成 .rdp 文件并拉起远程桌面客户端。
/// <para>
/// 安全要点（原实现缺陷已修）：密码**不再**经 <c>cmdkey.exe /pass:&lt;明文&gt;</c> 传入，
/// 而是通过 <see cref="IRdpBackend.StoreCredential"/> 写入系统凭据存储。
/// 原因是命令行参数对任何进程可见（Win32_Process.CommandLine / 任务管理器），
/// 而 CredWriteW 的 CredentialBlob 不进进程命令行。
/// </para>
/// </summary>
public static class RdpLauncher
{
	/// <param name="RedirectClipboard">
	/// 是否启用剪贴板重定向（默认 true）。
	/// 缺省为 true 是刻意的：本地与远程之间复制粘贴属于基本预期，
	/// 且 .rdp 中缺失 redirectclipboard 键时 mstsc 按「禁用」处理（不是按 UI 默认勾选）。
	/// </param>
	/// <param name="LocalDriveLetter">
	/// 要重定向的盘符。传 <see cref="AllDrivesToken"/> 表示全部驱动器（含连接后接入的）；
	/// 传单个字母（如 "D"）表示只重定向该盘。
	/// </param>
	public sealed record RdpOptions(string Host, int Port, string UserName, string Password, int? Width, int? Height, bool RedirectLocalDrive, string LocalDriveLetter, bool ForceConsole, bool RedirectClipboard = true,
		// —— v2.1.1 追加：设置页签（mstsc 原生项）的高级项。全部带默认值，
		//    既有调用点零改动。DisplayMode 两档互斥：预设档走 BuildRdpContent 的
		//    原有键组，自定义档按下方单项逐键写出（二者不同时生效）。 ——
		int DisplayMode = 0,
		bool UseMultiMon = false,
		int ColorDepth = 32,
		bool ShowConnectionBar = true,
		int AudioMode = 0,
		int KeyboardHook = 2,
		bool RedirectPrinters = true,
		bool DisableWallpaper = false,
		bool AllowFontSmoothing = true,
		bool AllowDesktopComposition = true,
		bool DisableFullWindowDrag = false,
		bool DisableMenuAnims = false,
		bool DisableThemes = false,
		bool BitmapCachePersist = true,
		bool AutoReconnect = true,
		int ConnectionType = 7,
		int AuthLevel = 2,
		bool UseWebAuth = false)
	{
		/// <summary>
		/// 「全部驱动器」的协议取值。Microsoft RDP 属性规范中，drivestoredirect 支持
		/// `*`（全部，含之后接入的）与 `DynamicDrives`（仅之后接入的）；此处用 `*`，
		/// 因为它同时覆盖已有盘与后插的 U 盘/移动硬盘。
		/// </summary>
		public const string AllDrivesToken = "*";

		/// <summary>
		/// 把盘符规范成 RDP 规范要求的 `D:\;` 形态。
		/// 规范来源：drivestoredirect 的官方示例为 `C:\;E:\;` —— 每个盘符带尾随反斜杠，
		/// 并以分号结尾。历史实现只写 `D:`，属于不完整语法（mstsc 可能宽松接受，
		/// 但不符合规范，且多盘符时无法拼接）。
		/// </summary>
		public static string NormalizeDriveSpec(string? letter)
		{
			if (string.IsNullOrWhiteSpace(letter))
			{
				return string.Empty;
			}
			string trimmed = letter.Trim();
			if (AllDrivesToken.Equals(trimmed, StringComparison.Ordinal))
			{
				return AllDrivesToken;
			}
			// 归一成单字母：接受 "D"、"D:"、"D:\" 三种输入
			char drive = char.ToUpperInvariant(trimmed[0]);
			if (drive < 'A' || drive > 'Z')
			{
				return string.Empty;
			}
			return $"{drive}:\\;";
		}
	}

	private static IRdpBackend _backend = WindowsRdpBackend.Instance;

	/// <summary>可替换后端（测试注入用）。生产默认走 Windows 实现。</summary>
	public static IRdpBackend Backend
	{
		get
		{
			return _backend;
		}
		set
		{
			_backend = value ?? throw new ArgumentNullException(nameof(value));
		}
	}

	/// <summary>
	/// A16：按主机统计**活跃连接引用数**。
	/// <para>
	/// 缺陷背景：延迟清理线程无条件删除 <c>TERMSRV/&lt;host&gt;</c> 凭据，
	/// 但同一主机的并发连接会共用同一个 target —— 先发的那条连接的清理
	/// 会把后发连接正在使用的凭据删掉，导致后者的 mstsc 弹密码框。
	/// </para>
	/// <para>
	/// 修复：Launch 时对主机 <c>+1</c>，延迟清理时对主机 <c>-1</c>；
	/// 只有引用数归零的那一轮才真正删除凭据，仍被引用的主机本轮跳过，
	/// 下一轮（引用归零时）再删。
	/// </para>
	/// </summary>
	private static readonly Dictionary<string, int> ActiveConnections = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly object ActiveConnectionsLock = new object();

	/// <summary>当前某主机的活跃连接引用数（测试与诊断可读）。</summary>
	public static int GetActiveConnectionCount(string host)
	{
		lock (ActiveConnectionsLock)
		{
			return ActiveConnections.TryGetValue(host, out int n) ? n : 0;
		}
	}

	/// <summary>登记一次对某主机的活跃连接引用（引用 +1）。</summary>
	public static void AcquireConnection(string host)
	{
		lock (ActiveConnectionsLock)
		{
			ActiveConnections.TryGetValue(host, out int n);
			ActiveConnections[host] = n + 1;
		}
	}

	/// <summary>
	/// 释放一次对某主机的活跃连接引用（引用 -1）。
	/// <para>返回值：该主机是否已无其它活跃连接引用（即凭据可以安全删除）。</para>
	/// </summary>
	public static bool ReleaseConnection(string host)
	{
		lock (ActiveConnectionsLock)
		{
			if (!ActiveConnections.TryGetValue(host, out int n))
			{
				return true;
			}
			n -= 1;
			if (n <= 0)
			{
				ActiveConnections.Remove(host);
				return true;
			}
			ActiveConnections[host] = n;
			return false;
		}
	}

	/// <summary>
	/// 尝试清理某主机的系统凭据。
	/// <para>
	/// 仅当该主机**已无活跃连接引用**时才删除；仍有引用则本轮跳过
	/// （由最后一条连接释放引用后的一轮删除）。
	/// </para>
	/// <para>返回值：true = 已删除；false = 仍被引用，本轮跳过。</para>
	/// </summary>
	public static bool TryCleanupCredentialIfUnused(string host)
	{
		if (GetActiveConnectionCount(host) > 0)
		{
			return false;
		}
		TryDeleteCredential(host);
		return true;
	}

	/// <summary>
	/// 延迟清理的等待时长（毫秒）。
	/// <para>
	/// 生产值 5000：mstsc 读取 .rdp 与凭据是异步的，需要给它一点时间。
	/// 暴露为可写是为了让单元测试不必真的等 5 秒。
	/// </para>
	/// </summary>
	public static int CleanupDelayMilliseconds { get; set; } = 5000;

	public static void Launch(RdpOptions opt)
	{
		string target = WindowsRdpBackend.BuildTarget(opt.Host);
		bool hasCredential = !string.IsNullOrEmpty(opt.UserName);
		if (hasCredential && !_backend.StoreCredential(target, opt.UserName, opt.Password))
		{
			// 凭据写入失败不应静默：mstsc 会退化为弹窗询问密码。
			Log.Error("RDP 凭据写入失败，mstsc 将改为交互式询问密码: " + target);
		}

		// A16：本条连接开始引用该主机的凭据。
		AcquireConnection(opt.Host);

		string rdpPath = WriteRdpFile(opt);
		Process? client = _backend.StartClient(rdpPath);

		// 窗口管理（纯窗口层，不碰 .rdp 内容）：连接后居中 + 外框尺寸钉死（拖动只平移）、
		// 右键粘贴跟随剪贴板重定向开关——重定向关闭时右键保持远程原生行为。
		MstscWindowManager.Track(client, opt.Width, opt.Height, opt.RedirectClipboard);

		// mstsc 读取 .rdp 与凭据是异步的，故延迟清理临时文件与一次性凭据。
		Task.Run(async delegate
		{
			try
			{
				await Task.Delay(CleanupDelayMilliseconds).ConfigureAwait(continueOnCapturedContext: false);
				TryDelete(rdpPath);
				// A16：本条连接对凭据的引用到此结束。
				ReleaseConnection(opt.Host);
				// 仅当该主机已无其它活跃连接引用时才删除凭据；
				// 否则本轮跳过（不删），由最后一条连接释放引用时的那一轮删除。
				if (!TryCleanupCredentialIfUnused(opt.Host))
				{
					Log.Info("RDP 凭据仍被同主机其他活跃连接引用，本轮不清理: " + opt.Host);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("RDP 延迟清理失败: " + opt.Host, ex);
			}
		});
	}

	public static void ClearCredential(string host)
	{
		TryDeleteCredential(host);
	}

	public static void CleanupTempFiles()
	{
		try
		{
			string dir = GetTempDirectory();
			if (!Directory.Exists(dir))
			{
				return;
			}
			foreach (string file in Directory.EnumerateFiles(dir, "*.rdp"))
			{
				TryDelete(file);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("启动清理临时 .rdp 文件失败", ex);
		}
	}

	/// <summary>临时 .rdp 所在目录：%TEMP%\RemoteDesktopPlus。</summary>
	public static string GetTempDirectory()
	{
		return Path.Combine(Path.GetTempPath(), "RemoteDesktopPlus");
	}

	/// <summary>
	/// 生成 .rdp 文件内容。公开以便单元测试直接断言各分支渲染结果，无需落盘。
	/// </summary>
	public static string BuildRdpContent(RdpOptions opt)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine($"full address:s:{SanitizeRdpValue(opt.Host)}:{opt.Port}");
		sb.AppendLine($"username:s:{SanitizeRdpValue(opt.UserName)}");
		sb.AppendLine("prompt for credentials:i:0");
		sb.AppendLine("authentication level:i:0");
		sb.AppendLine("enablecredsspsupport:i:1");

		bool hasSize = opt.Width.HasValue && opt.Width.GetValueOrDefault() > 0
			&& opt.Height.HasValue && opt.Height.GetValueOrDefault() > 0;
		if (hasSize)
		{
			sb.AppendLine("screen mode id:i:1");
			sb.AppendLine($"desktopwidth:i:{opt.Width.GetValueOrDefault()}");
			sb.AppendLine($"desktopheight:i:{opt.Height.GetValueOrDefault()}");
		}
		else
		{
			sb.AppendLine("screen mode id:i:2");
		}

		// 剪贴板重定向：mstsc 对此键**显式关闭优先**，键缺失时按禁用处理，
		// 故必须显式写出 i:1，不能依赖「未写就默认开」。
		sb.AppendLine($"redirectclipboard:i:{(opt.RedirectClipboard ? 1 : 0)}");

		// 驱动器重定向三种取值（Microsoft RDP 属性规范）：
		//   键缺失 / 值空 → 不重定向
		//   *            → 全部驱动器，**含连接之后接入的**（动态，比逐个枚举更彻底）
		//   C:\;E:\;     → 指定盘符（每个盘符带尾随反斜杠，分号分隔）
		// 注意 * 后面**不能**再跟冒号，`s:*:` 是非法语法，会导致整段重定向失效。
		if (opt.RedirectLocalDrive)
		{
			string driveSpec = RdpOptions.NormalizeDriveSpec(opt.LocalDriveLetter);
			if (!string.IsNullOrEmpty(driveSpec))
			{
				sb.AppendLine($"drivestoredirect:s:{driveSpec}");
			}
		}
		if (opt.ForceConsole)
		{
			sb.AppendLine("connect to console:i:1");
		}

		// —— v2.1.2：mstsc 原生键无条件写出（对齐原版）。默认值 = mstsc 默认值，
		//    改了设置才产生差异，不重蹈 v2.0.0「流畅优先档」画质模糊的覆辙。 ——
		sb.AppendLine($"use multimon:i:{(opt.UseMultiMon ? 1 : 0)}");
		sb.AppendLine($"session bpp:i:{opt.ColorDepth}");
		sb.AppendLine($"displayconnectionbar:i:{(opt.ShowConnectionBar ? 1 : 0)}");
		sb.AppendLine($"audiomode:i:{opt.AudioMode}");
		sb.AppendLine($"keyboardhook:i:{opt.KeyboardHook}");
		sb.AppendLine($"redirectprinters:i:{(opt.RedirectPrinters ? 1 : 0)}");
		sb.AppendLine($"connection type:i:{opt.ConnectionType}");
		sb.AppendLine($"disable wallpaper:i:{(opt.DisableWallpaper ? 1 : 0)}");
		sb.AppendLine($"allow font smoothing:i:{(opt.AllowFontSmoothing ? 1 : 0)}");
		sb.AppendLine($"allow desktop composition:i:{(opt.AllowDesktopComposition ? 1 : 0)}");
		sb.AppendLine($"disable full window drag:i:{(opt.DisableFullWindowDrag ? 1 : 0)}");
		sb.AppendLine($"disable menu anims:i:{(opt.DisableMenuAnims ? 1 : 0)}");
		sb.AppendLine($"disable themes:i:{(opt.DisableThemes ? 1 : 0)}");
		sb.AppendLine($"bitmapcachepersistenable:i:{(opt.BitmapCachePersist ? 1 : 0)}");
		sb.AppendLine($"autoreconnection enabled:i:{(opt.AutoReconnect ? 1 : 0)}");
		sb.AppendLine($"authentication level:i:{opt.AuthLevel}");
		sb.AppendLine($"enablecredsspsupport:i:{(opt.UseWebAuth ? 0 : 1)}");
		return sb.ToString();
	}

	/// <summary>
	/// 清洗写入 .rdp 的用户可控值。
	/// .rdp 是 CRLF 分隔的 <c>key:type:value</c> 文本格式，用户输入里的换行会让后续内容
	/// 被当成新的配置项解析——例如把主机名填成 <c>host\r\nadministrative mode:i:1</c>
	/// 就能在导出的文件里塞进任意 mstsc 设置。因此所有用户可控值必须剔除 CR/LF。
	/// </summary>
	public static string SanitizeRdpValue(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		// 剔除 CR/LF（切断配置项注入），并去掉其余控制字符（避免 mstsc 解析异常）。
		StringBuilder sb = new StringBuilder(value.Length);
		foreach (char c in value)
		{
			if (c == '\r' || c == '\n')
			{
				continue;
			}
			if (char.IsControl(c))
			{
				continue;
			}
			sb.Append(c);
		}
		return sb.ToString();
	}

	/// <summary>
	/// 生成并落盘 .rdp 文件（UTF-16LE），返回绝对路径。
	/// <para>
	/// A19：文件名必须含唯一成分。旧实现只用「主机名_端口」，
	/// 同一主机同一端口的并发连接会写到**同一个文件**上：
	/// 后发的覆盖先发的（内容不同 → 先发那条拿到的是后发的配置），
	/// 且先发那条的延迟清理会把后发的 .rdp 删掉 → 后发的 mstsc 读不到文件。
	/// </para>
	/// <para>
	/// 现在文件名形如 <c>host_port_&lt;pid&gt;_&lt;序号&gt;_&lt;随机&gt;.rdp</c>：
	/// PID 区分进程、序号区分同进程内的多次生成、随机后缀兜底跨进程同 PID 复用。
	/// 主机名+端口前缀保留（前缀仍可用于人工辨认是哪台机器）。
	/// </para>
	/// </summary>
	public static string WriteRdpFile(RdpOptions opt)
	{
		string dir = GetTempDirectory();
		Directory.CreateDirectory(dir);
		string safeHost = string.Join("_", opt.Host.Split(Path.GetInvalidFileNameChars()));
		string path = Path.Combine(dir, $"{safeHost}_{opt.Port}_{BuildUniqueSuffix()}");
		File.WriteAllText(path, BuildRdpContent(opt), Encoding.Unicode);
		return path;
	}

	/// <summary>进程内单调递增的临时文件序号（配合 PID + 随机后缀保证唯一）。</summary>
	private static int _tempFileSeq;

	/// <summary>构造 .rdp 临时文件的唯一后缀（PID + 序号 + 随机十六进制）。</summary>
	private static string BuildUniqueSuffix()
	{
		int seq = Interlocked.Increment(ref _tempFileSeq);
		string rand = Guid.NewGuid().ToString("N").Substring(0, 8);
		return $"{Environment.ProcessId}_{seq}_{rand}.rdp";
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static void TryDeleteCredential(string host)
	{
		try
		{
			_backend.DeleteCredential(WindowsRdpBackend.BuildTarget(host));
		}
		catch
		{
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 14 / 39：RemoteDesktopPlus.Services/OnlineProbe.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public static class OnlineProbe
{
	public static async Task<bool> IsOnlineAsync(string host, int port, int timeoutMs = 1500, CancellationToken token = default(CancellationToken))
	{
		try
		{
			using TcpClient client = new TcpClient();
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);
			cts.CancelAfter(timeoutMs);
			await client.ConnectAsync(host, port, cts.Token);
			return client.Connected;
		}
		catch
		{
			return false;
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 15 / 39：RemoteDesktopPlus.Services/IDialogService.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System.Collections.Generic;
using RemoteDesktopPlus.Models;

/// <summary>弹窗的语义分类，避免把 WPF 的 MessageBoxImage 类型泄漏进 ViewModel。</summary>
public enum DialogKind
{
	/// <summary>普通提示。</summary>
	Information,

	/// <summary>警告 / 需要用户特别留意的确认。</summary>
	Warning,

	/// <summary>操作失败。</summary>
	Error
}

/// <summary>
/// ViewModel 面向用户的交互出口。
/// <para>
/// 引入它的唯一目的：切断 <c>MainViewModel → RemoteDesktopPlus.Views</c> 的直接依赖。
/// 视图模型只表达「需要弹一个错误」「需要用户填一份连接表单」「需要用户选一个文件」，
/// 至于用哪个窗口、哪个控件实现，由 <c>WpfDialogService</c> 在视图层决定。
/// </para>
/// <para>
/// 该接口只出现 Models 里的 DTO 与自身枚举，因此 ViewModel 与测试替身都不需要引用 WPF 控件。
/// </para>
/// </summary>
public interface IDialogService
{
	/// <summary>提示 / 警告 / 错误提示框（仅一个确定按钮）。</summary>
	void ShowMessage(string message, string title, DialogKind kind = DialogKind.Information);

	/// <summary>是 / 否确认框，返回用户是否选择「是」。</summary>
	bool Confirm(string message, string title, DialogKind kind = DialogKind.Warning);

	/// <summary>
	/// 打开「添加 / 编辑服务器」对话框。
	/// </summary>
	/// <param name="source">要编辑的既有连接；为 null 表示新建。</param>
	/// <param name="result">用户确认后的表单内容；取消时无意义。</param>
	/// <returns>用户是否点了确定。</returns>
	bool EditConnection(ConnectionItem? source, out ConnectionFormResult result);

	/// <summary>打开「设置启动密码」对话框。</summary>
	/// <returns>用户是否点了确定。</returns>
	bool SetLaunchPassword();

	/// <summary>打开「设置」窗口（v2.1.0，常规/显示/本地资源/体验/高级页签）。</summary>
	void ShowSettings();

	/// <summary>打开主密码对话框（设置或解锁，由 <paramref name="purpose"/> 决定）。</summary>
	/// <returns>用户是否点了确定。</returns>
	bool ShowMasterPassword(MasterPasswordPurpose purpose);

	/// <summary>选择导出目标文件；用户取消返回 null。</summary>
	string? PickExportTarget(string suggestedFileName);

	/// <summary>
	/// 选择导出「单条连接」的目标文件（.rdp）；用户取消返回 null。
	/// 与 <see cref="PickExportTarget"/> 分开是因为两者扩展名与过滤器不同，
	/// 混用会让用户拿到一个后缀为 .db 的 rdp 文件。
	/// </summary>
	string? PickRdpExportTarget(string suggestedFileName);

	/// <summary>选择导入来源文件；用户取消返回 null。</summary>
	string? PickImportSource();

	/// <summary>
	/// 打开「分组设置」面板，为该分组配置连接参数覆盖。
	/// </summary>
	/// <param name="groupName">要配置的分组名（非空）。</param>
	/// <param name="store">分组配置存储，面板直接通过它读写。</param>
	/// <param name="resolutionOptions">分辨率候选。</param>
	/// <param name="driveLetterOptions">盘符候选。</param>
	/// <returns>用户是否点了保存。</returns>
	bool EditGroupOptions(string groupName, GroupOptionStore store, IReadOnlyList<string> resolutionOptions, IReadOnlyList<string> driveLetterOptions);
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 16 / 39：RemoteDesktopPlus.Services/BackupService.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.IO;
using Microsoft.Data.Sqlite;

/// <summary>
/// 备份格式抽象（E3 扩展点）：新格式 = 实现本接口并在 ViewModel 按扩展名分发。
/// 现有实现 <see cref="DbFileBackupFormat"/> 保持「整库 .db 文件」的既有语义。
/// </summary>
public interface IBackupFormat
{
	/// <summary>格式标识，如 "dbfile"；用于日志与未来多格式分发。</summary>
	string FormatId { get; }

	/// <summary>把数据库导出到目标文件。</summary>
	void Export(string targetPath, DatabaseService db);

	/// <summary>从来源文件导入数据库（覆盖式）。</summary>
	void Import(string sourcePath, DatabaseService db);
}

/// <summary>
/// 整库 .db 文件格式。
/// Export 改用 <c>VACUUM INTO</c>（P2-6）：在 SQLite 引擎内生成一致快照，
/// 消除「直接拷贝 .db 恰逢事务进行中 → 备份半新半旧」的损坏风险；
/// VACUUM 不可用（老引擎/文件锁）时回退到文件拷贝。
/// Import 前做 SQLite 文件头校验（前 16 字节 = "SQLite format 3\0"），
/// 拒绝非本程序库文件，防止误导入任意文件覆盖 data.db。
/// </summary>
public sealed class DbFileBackupFormat : IBackupFormat
{
	private static readonly byte[] SqliteMagic = "SQLite format 3\0"u8.ToArray();

	public string FormatId => "dbfile";

	public void Export(string targetPath, DatabaseService db)
	{
		try
		{
			using (SqliteConnection connection = new SqliteConnection(db.BuildConnectionString()))
			{
				connection.Open();
				using SqliteCommand command = connection.CreateCommand();
				command.CommandText = "VACUUM INTO $path;";
				command.Parameters.AddWithValue("$path", targetPath);
				command.ExecuteNonQuery();
			}
			Log.Info("Backup export ok (vacuum): " + targetPath);
		}
		catch (SqliteException)
		{
			// 回退：直接拷贝。仍好过完全无法导出；一致性风险见类注释。
			ExportByFileCopy(targetPath, db);
			Log.Warn("Backup export fell back to file copy: " + targetPath);
		}
	}

	private static void ExportByFileCopy(string targetPath, DatabaseService db)
	{
		using FileStream source = new FileStream(db.DbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using FileStream destination = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
		source.CopyTo(destination);
	}

	public void Import(string sourcePath, DatabaseService db)
	{
		if (!File.Exists(sourcePath))
		{
			throw new FileNotFoundException("导入的数据库文件不存在", sourcePath);
		}
		ValidateSqliteHeader(sourcePath);

		string dbPath = db.DbPath;
		string backup = dbPath + ".bak";
		// 导入不是「直接覆盖」：先把源文件复制到临时文件做完整性与表结构校验，
		// 通过后才原子替换正式库。直接覆盖时，若源文件是损坏的或结构不符，
		// 用户会连同原库一起丢失，且无法回滚。
		string staging = dbPath + ".import-staging";
		StagingFileGuard guard = new StagingFileGuard(staging, dbPath + ".new");
		bool replaced = false;
		try
		{
			// 阶段一：暂存 + 校验。此阶段任何失败都不动正式库，无需回滚。
			File.Copy(sourcePath, staging, overwrite: true);
			ValidateStagedDatabase(staging);

			// 阶段二：备份现库，然后原子替换。
			if (File.Exists(dbPath))
			{
				File.Copy(dbPath, backup, overwrite: true);
			}
			GC.Collect();
			GC.WaitForPendingFinalizers();
			SqliteConnection.ClearAllPools();
			AtomicReplace(staging, dbPath);
			replaced = true;
			db.ReinitializeAfterImport();
			Log.Info("Backup import ok: " + sourcePath);
		}
		catch (Exception ex)
		{
			if (!replaced)
			{
				// 正式库尚未被触碰：校验/复制阶段失败，直接向上抛即可，原数据完好。
				Log.Error("Backup import aborted before replacing the live database: " + sourcePath, ex);
				throw;
			}
			// 已替换后失败：回滚。回滚失败绝不能静默——用户可能以为数据已还原，
			// 实际库文件状态未知，故写哨兵文件供下次启动告警。
			try
			{
				SqliteConnection.ClearAllPools();
				if (File.Exists(backup))
				{
					File.Copy(backup, dbPath, overwrite: true);
					db.ReinitializeAfterImport();
				}
				Log.Error("Backup import failed and rollback attempted: " + sourcePath, ex);
			}
			catch (Exception rollbackEx)
			{
				Log.Error("Backup import rollback FAILED - database may be corrupted: " + dbPath, rollbackEx);
				try
				{
					File.WriteAllText(dbPath + ".import-failed",
						$"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n来源: {sourcePath}\n" +
						"导入失败且自动回滚未完成，数据文件可能处于不一致状态。\n" +
						$"回滚异常: {rollbackEx.Message}\n");
				}
				catch
				{
				}
			}
			throw;
		}
		finally
		{
			guard.Dispose();
		}
	}

	/// <summary>导入前的强校验：完整性 + 必需表结构。任一不满足即拒绝导入。</summary>
	private static void ValidateStagedDatabase(string path)
	{
		using SqliteConnection connection = new SqliteConnection("Data Source=" + path + ";Mode=ReadOnly;");
		connection.Open();
		using (SqliteCommand integrity = connection.CreateCommand())
		{
			integrity.CommandText = "PRAGMA integrity_check;";
			object? result = integrity.ExecuteScalar();
			string verdict = result?.ToString() ?? string.Empty;
			if (!string.Equals(verdict, "ok", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("导入文件数据库完整性检查未通过：" + verdict + "。已拒绝导入，原数据未受影响。");
			}
		}
		foreach (string table in new[] { "Connections", "Settings" })
		{
			using SqliteCommand cmd = connection.CreateCommand();
			cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$t;";
			cmd.Parameters.AddWithValue("$t", table);
			long count = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
			if (count == 0)
			{
				throw new InvalidDataException("导入文件缺少必需的数据表 " + table + "，不是本程序导出的备份。已拒绝导入，原数据未受影响。");
			}
		}
	}

	/// <summary>原子替换：先删旧文件再改名（同卷 rename 为原子操作）。</summary>
	private static void AtomicReplace(string stagingPath, string destinationPath)
	{
		string destDirectory = Path.GetDirectoryName(destinationPath) ?? AppContext.BaseDirectory;
		string destFileName = Path.GetFileName(destinationPath);
		string tempDestination = Path.Combine(destDirectory, destFileName + ".new");
		File.Copy(stagingPath, tempDestination, overwrite: true);
		// File.Replace 需要目标已存在；不存在时退化为 delete + move。
		if (File.Exists(destinationPath))
		{
			File.Replace(tempDestination, destinationPath, null, ignoreMetadataErrors: true);
		}
		else
		{
			File.Delete(destinationPath);
			File.Move(tempDestination, destinationPath);
		}
	}

	/// <summary>临时文件守卫：无论成功失败都删除暂存文件。</summary>
	private sealed class StagingFileGuard : IDisposable
	{
		private readonly string[] _paths;

		public StagingFileGuard(params string[] paths)
		{
			_paths = paths;
		}

		public void Dispose()
		{
			foreach (string path in _paths)
			{
				try
				{
					if (File.Exists(path))
					{
						File.Delete(path);
					}
				}
				catch
				{
					// 清理失败不应影响主流程。
				}
			}
		}
	}

	private static void ValidateSqliteHeader(string path)
	{
		using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		if (stream.Length < SqliteMagic.Length)
		{
			throw new InvalidDataException("文件过小，不是有效的 SQLite 数据库文件。");
		}
		byte[] header = new byte[SqliteMagic.Length];
		int read = stream.Read(header, 0, header.Length);
		if (read != header.Length || !header.AsSpan().SequenceEqual(SqliteMagic))
		{
			throw new InvalidDataException("所选文件不是 SQLite 数据库（缺少 SQLite 文件头），已拒绝导入。");
		}
	}
}

/// <summary>
/// 备份服务门面：保留原静态调用形态（BackupService.Export/Import），
/// 内部委托给默认的 <see cref="DbFileBackupFormat"/>。
/// </summary>
public static class BackupService
{
	public static IBackupFormat DefaultFormat { get; set; } = new DbFileBackupFormat();

	public static void Export(string targetPath) => DefaultFormat.Export(targetPath, DatabaseService.Instance);

	public static void Import(string sourcePath) => DefaultFormat.Import(sourcePath, DatabaseService.Instance);
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 17 / 39：RemoteDesktopPlus.Services/RdpServerService.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

public static class RdpServerService
{
	private const string RdpTcpKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp";

	private const string TerminalServerKey = "SYSTEM\\CurrentControlSet\\Control\\Terminal Server";

	public const int DefaultPort = 3389;

	private const string FirewallRulePrefix = "RemoteDesktopPlus-RDP-";

	public static int GetCurrentPort()
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp");
			object obj = registryKey?.GetValue("PortNumber");
			if (obj is int)
			{
				return (int)obj;
			}
		}
		catch
		{
		}
		return 3389;
	}

	public static bool IsRemoteEnabled()
	{
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server");
			if (registryKey?.GetValue("fDenyTSConnections") is int num)
			{
				return num == 0;
			}
		}
		catch
		{
		}
		return false;
	}

	public static void SetRemoteEnabled(bool enabled)
	{
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server", writable: true) ?? throw new InvalidOperationException("无法打开终端服务注册表项，请确认以管理员身份运行。");
		registryKey.SetValue("fDenyTSConnections", (!enabled) ? 1 : 0, RegistryValueKind.DWord);
	}

	/// <summary>
	/// 修改远程桌面端口，按「先加新规则 → 再改注册表 → 最后删旧规则」的顺序做事务式变更。
	/// 任一步失败即回滚到原状态，避免出现「注册表已改但防火墙没放行 → 连不上」的中间态。
	/// </summary>
	public static void SetPort(int port)
	{
		if (port < 1 || port > 65535)
		{
			throw new ArgumentOutOfRangeException("port", "端口范围必须在 1 到 65535 之间。");
		}
		int currentPort = GetCurrentPort();
		if (currentPort == port)
		{
			// 端口未变：仅确保规则存在，避免重复执行注册表写入。
			if (!AddFirewallRule(port))
			{
				throw new InvalidOperationException("防火墙规则创建失败，端口未做变更。请确认以管理员身份运行且 Windows 防火墙服务可用。");
			}
			RemoveOurFirewallRules(port);
			return;
		}

		// 步骤 1：先为新端口放行。若失败则不触碰注册表，原状态保持不变。
		if (!AddFirewallRule(port))
		{
			throw new InvalidOperationException("新端口 " + port + " 的防火墙规则创建失败，已放弃端口变更（仍为 " + currentPort + "）。请确认以管理员身份运行且 Windows 防火墙服务可用。");
		}

		// 步骤 2：改注册表。失败则回滚防火墙（删除刚加的新规则），旧规则不动。
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp", writable: true) ?? throw new InvalidOperationException("无法打开 RDP-Tcp 注册表项，请确认以管理员身份运行。");
			registryKey.SetValue("PortNumber", port, RegistryValueKind.DWord);
		}
		catch (Exception ex)
		{
			RemoveFirewallRuleForPort(port);
			throw new InvalidOperationException("写入端口注册表失败，已回滚防火墙规则，端口仍为 " + currentPort + "。原因：" + ex.Message, ex);
		}

		// 步骤 3：新端口已生效，最后清理其余旧规则（含旧端口）。
		RemoveOurFirewallRules(port);
		RemoveFirewallRuleForPort(currentPort);
	}

	public static bool IsPortListening(int port)
	{
		try
		{
			IPEndPoint[] activeTcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
			for (int i = 0; i < activeTcpListeners.Length; i++)
			{
				if (activeTcpListeners[i].Port == port)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	/// <summary>为指定端口重建入站放行规则。返回是否真正建成。</summary>
	private static bool AddFirewallRule(int port)
	{
		string text = FirewallRulePrefix + port;
		// 先删同名旧规则再新增，保证幂等。
		RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text);
		return RunNetsh("advfirewall", "firewall", "add", "rule", "name=" + text, "dir=in", "action=allow", "protocol=TCP", $"localport={port}");
	}

	/// <summary>删除指定端口的入站放行规则。返回是否删除成功（规则不存在也视为成功）。</summary>
	private static bool RemoveFirewallRuleForPort(int port)
	{
		string text = FirewallRulePrefix + port;
		return RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text);
	}

	private static void RemoveOurFirewallRules(int exceptPort)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				ArgumentList = 
				{
					"-NoProfile",
					"-Command",
					$"Get-NetFirewallRule -DisplayName '{"RemoteDesktopPlus-RDP-"}*' -ErrorAction SilentlyContinue | Where-Object {{ $_.DisplayName -ne '{"RemoteDesktopPlus-RDP-"}{exceptPort}' }} | " + "ForEach-Object { $_.DisplayName }"
				}
			});
			if (process == null)
			{
				return;
			}
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit(3000);
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (!string.IsNullOrEmpty(text2) && text2.StartsWith("RemoteDesktopPlus-RDP-", StringComparison.OrdinalIgnoreCase))
				{
					RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text2);
				}
			}
		}
		catch
		{
		}
	}

	/// <summary>
	/// 执行 netsh 并返回是否成功。旧实现吞掉退出码，导致「防火墙规则没加上」这类失败
	/// 完全无感；这里把退出码与 stderr 纳入判断，失败时记日志。
	/// </summary>
	private static bool RunNetsh(params string[] args)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo
		{
			FileName = "netsh.exe",
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};
		foreach (string item in args)
		{
			processStartInfo.ArgumentList.Add(item);
		}
		try
		{
			using Process process = Process.Start(processStartInfo);
			if (process == null)
			{
				return false;
			}
			string stdout = process.StandardOutput.ReadToEnd();
			string stderr = process.StandardError.ReadToEnd();
			if (!process.WaitForExit(5000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				Log.Error("netsh 执行超时: " + string.Join(" ", args));
				return false;
			}
			if (process.ExitCode != 0)
			{
				Log.Error("netsh 退出码 " + process.ExitCode + ": " + string.Join(" ", args)
					+ " | stdout=" + stdout.Trim() + " | stderr=" + stderr.Trim());
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("netsh 执行异常: " + string.Join(" ", args), ex);
			return false;
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 追加区段：RemoteDesktopPlus.Services/AutoStartService.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Diagnostics;
using System.IO;

/// <summary>
/// 开机自启服务：用**计划任务**而非 HKCU Run 键实现。
/// <para>
/// 为什么不用注册表 Run 键：本程序 app.manifest 声明 <c>requireAdministrator</c>，
/// UAC 对「提权进程写 Run 键 + 开机静默拉起提权进程」有专门拦截 —— 写键能成功，
/// 但登录时该项会被静默跳过（事件日志 Event ID  ForgottenRunKey 类问题），
/// 用户只会看到「明明开了自启却从不生效」。
/// </para>
/// <para>
/// 计划任务方案：<c>schtasks /create /sc onlogon /rl highest /f</c>，任务以
/// 「登录时 + 最高权限」运行，绕过 UAC 弹窗且能真正静默拉起提权进程。
/// 任务名固定，重复 create 用 /f 覆盖，天然幂等；删除用 /delete /f。
/// </para>
/// </summary>
public static class AutoStartService
{
	/// <summary>计划任务名。加产品前缀避免与系统任务撞名。</summary>
	public const string TaskName = "XiaomaRemoteDesktop_AutoStart";

	/// <summary>读取当前自启开关状态（仅查应用自己的持久化设置）。</summary>
	public static bool IsEnabled(DatabaseService db)
	{
		string raw = db.GetSetting(SettingKeys.OptionAutoStart);
		return bool.TryParse(raw, out bool v) && v;
	}

	/// <summary>
	/// 开关自启。写设置与建/删任务在同一事务语义下处理：任务操作失败时抛出，
	/// 由调用方提示用户；设置回写放在成功路径上，保证「设置值 = 实际状态」。
	/// </summary>
	public static void SetEnabled(DatabaseService db, bool enabled, string exePath)
	{
		if (enabled)
		{
			CreateTask(exePath);
		}
		else
		{
			DeleteTask();
		}
		db.SetSetting(SettingKeys.OptionAutoStart, enabled.ToString());
	}

	/// <summary>当前 exe 的完整路径（自启任务要指向它）。</summary>
	public static string GetExecutablePath()
	{
		return Environment.ProcessPath
			?? System.Reflection.Assembly.GetExecutingAssembly().Location;
	}

	private static void CreateTask(string exePath)
	{
		// --silent：登录拉起时直接进托盘，不弹主窗（见 App.OnStartup 对该参数的处理）。
		// /rl highest：与 manifest 的 requireAdministrator 对齐，避免 UAC 弹窗。
		// /f：已存在同名任务时覆盖，保证路径变更（升级/换目录）后任务始终指向新位置。
		RunSchtasks($" /create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --silent\" /sc onlogon /rl highest /f");
	}

	private static void DeleteTask()
	{
		// /f：任务不存在时不报错，幂等删除。
		RunSchtasks($" /delete /tn \"{TaskName}\" /f");
	}

	private static void RunSchtasks(string arguments)
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo
			{
				FileName = "schtasks.exe",
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			}) ?? throw new InvalidOperationException("无法启动 schtasks.exe");
			process.WaitForExit(10000);
			if (process.ExitCode != 0)
			{
				string stderr = process.StandardError.ReadToEnd();
				throw new InvalidOperationException("schtasks 退出码 " + process.ExitCode + "：" + stderr.Trim());
			}
		}
		catch (Exception ex) when (ex is not InvalidOperationException)
		{
			throw new InvalidOperationException("计划任务操作失败：" + ex.Message, ex);
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════