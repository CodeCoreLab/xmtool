// ============================================================================
//  小马远程桌面 —— 全量应用源码（单文件版）
// ----------------------------------------------------------------------------
//  本文件由 39 个原始 .cs 机械合并而成（备份: ../backup_pre_singlefile_20261019/），
//  合并规则：每个源文件对应一个「块级 namespace」区段，块内重放原文件 usings，
//  语义与多文件形态完全一致；partial 类跨块声明合法。
//
//  ⚠ 不可并入本文件的必要外部文件（WPF 编译器强制）：
//    * App.xaml + Views/*.xaml —— 视图声明（x:Class/clr-namespace 指向本文件内命名空间）
//    * Styles/Colors.xaml、Controls.xaml —— 设计令牌
//    * Properties/AssemblyInfo.cs —— 程序集特性（只能在全局命名空间）
//    * libs/*.dll —— 第三方依赖（既有，未新增）
//    * tests/ —— 独立测试工程
//
//  区段顺序（依赖方向）：Models → Services → ViewModels → Views → Converters → App 组合根
// ============================================================================

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
}

// ════════════════════════════════════════════════════════════════════════
// 区段 04 / 39：RemoteDesktopPlus.Models/ConnectionItem.cs  →  namespace RemoteDesktopPlus.Models
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Models
{
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

	/// <summary>最后成功连接时间（Unix 秒，0 = 从未连接）。</summary>
	public long LastConnectedAt { get; set; }

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

	public const string MasterKeyVerifier = "master.key.verifier";

	public const string MasterKeySalt = "master.key.salt";

	public const string OptionResolution = "options.resolution";

	public const string OptionRedirectDrive = "options.redirect_drive";

	public const string OptionLocalDriveLetter = "options.local_drive_letter";

	public const string OptionForceConsole = "options.force_console";

	public const string OptionRedirectClipboard = "options.redirect_clipboard";
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

	private static void CleanupOldLogs()
	{
		try
		{
			DateTime cutoff = DateTime.Today.AddDays(-RetainDays);
			foreach (string file in Directory.EnumerateFiles(LogDirectory, "log-*.log"))
			{
				string name = Path.GetFileNameWithoutExtension(file); // log-YYYYMMDD
				if (name.Length == 12
					&& DateTime.TryParseExact(name.AsSpan(5, 8), "yyyyMMdd", null,
						System.Globalization.DateTimeStyles.None, out DateTime date)
					&& date < cutoff)
				{
					File.Delete(file);
				}
			}
		}
		catch
		{
			// 清理失败静默，下次再试。
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
		byte[] salt = Convert.FromBase64String(saltB64);
		byte[] key = DeriveKey(masterPassword, salt);
		try
		{
			if (AesGcmDecrypt(verifier, key) == "OK")
			{
				_masterKey = key;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	/// <summary>设置/修改主密码：全量重加密所有条目，事务内完成，失败回滚。</summary>
	public void SetMasterPassword(string newPassword, IEnumerable<ConnectionItem> existingItems)
	{
		var source = existingItems
			.Select(item => (Item: item, OriginalCipher: item.EncryptedPassword, Plain: Decrypt(item.EncryptedPassword)))
			.ToList();
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
	public void RemoveMasterPassword(IEnumerable<ConnectionItem> existingItems)
	{
		if (!HasMasterKey)
		{
			throw new InvalidOperationException("请先解锁。");
		}
		var source = existingItems
			.Select(item => (Item: item, OriginalCipher: item.EncryptedPassword, Plain: Decrypt(item.EncryptedPassword)))
			.ToList();
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
	public string Decrypt(string cipher)
	{
		if (string.IsNullOrEmpty(cipher))
		{
			return string.Empty;
		}
		try
		{
			if (cipher.StartsWith(PrefixMaster, StringComparison.Ordinal))
			{
				if (_masterKey == null)
				{
					return string.Empty;
				}
				return AesGcmDecrypt(cipher.Substring(PrefixMaster.Length), _masterKey);
			}
			if (cipher.StartsWith(PrefixDpapi, StringComparison.Ordinal))
			{
				return DpapiDecrypt(cipher.Substring(PrefixDpapi.Length));
			}
			return DpapiDecrypt(cipher);
		}
		catch
		{
			return string.Empty;
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

	private const int CRED_PERSIST_LOCAL_MACHINE = 2;

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
				Persist = CRED_PERSIST_LOCAL_MACHINE,
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
// (no usings)

/// <summary>
/// 与 RDP 客户端进程及其凭据存储交互的抽象层。
/// 存在的意义有二：一是把 mstsc.exe / 凭据写入隔离开，便于换后端；
/// 二是让测试能断言「密码只进安全通道，绝不进进程命令行」——
/// 见 <see cref="WindowsRdpBackend.StartClient"/> 只接受 .rdp 文件路径，参数面天然不含密码。
/// </summary>
public interface IRdpBackend
{
	/// <summary>把凭据写入系统凭据存储（等价于 cmdkey /generic，但不经命令行）。</summary>
	void StoreCredential(string target, string userName, string password);

	/// <summary>从系统凭据存储删除指定 target 的凭据。</summary>
	void DeleteCredential(string target);

	/// <summary>以给定的 .rdp 文件启动远程桌面客户端。</summary>
	void StartClient(string rdpFilePath);
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 11 / 39：RemoteDesktopPlus.Services/WindowsRdpBackend.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System.Diagnostics;

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

	public void StoreCredential(string target, string userName, string password)
	{
		CredentialStore.WriteGeneric(target, userName, password);
	}

	public void DeleteCredential(string target)
	{
		CredentialStore.DeleteGeneric(target);
	}

	public void StartClient(string rdpFilePath)
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = "mstsc.exe",
			UseShellExecute = true,
			ArgumentList = { rdpFilePath }
		});
	}
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
	public sealed record RdpOptions(string Host, int Port, string UserName, string Password, int? Width, int? Height, bool RedirectLocalDrive, string LocalDriveLetter, bool ForceConsole, bool RedirectClipboard = true);

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

	public static void Launch(RdpOptions opt)
	{
		string target = WindowsRdpBackend.BuildTarget(opt.Host);
		bool hasCredential = !string.IsNullOrEmpty(opt.UserName);
		if (hasCredential)
		{
			_backend.StoreCredential(target, opt.UserName, opt.Password);
		}

		string rdpPath = WriteRdpFile(opt);
		_backend.StartClient(rdpPath);

		// mstsc 读取 .rdp 与凭据是异步的，故延迟清理临时文件与一次性凭据。
		Task.Run(async delegate
		{
			try
			{
				await Task.Delay(5000).ConfigureAwait(continueOnCapturedContext: false);
				TryDelete(rdpPath);
				if (hasCredential)
				{
					TryDeleteCredential(opt.Host);
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
		sb.AppendLine($"full address:s:{opt.Host}:{opt.Port}");
		sb.AppendLine($"username:s:{opt.UserName}");
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

		if (opt.RedirectLocalDrive && !string.IsNullOrEmpty(opt.LocalDriveLetter))
		{
			sb.AppendLine($"drivestoredirect:s:{opt.LocalDriveLetter}:");
		}
		if (opt.ForceConsole)
		{
			sb.AppendLine("connect to console:i:1");
		}
		return sb.ToString();
	}

	/// <summary>生成并落盘 .rdp 文件（UTF-16LE），返回绝对路径。</summary>
	public static string WriteRdpFile(RdpOptions opt)
	{
		string dir = GetTempDirectory();
		Directory.CreateDirectory(dir);
		string safeHost = string.Join("_", opt.Host.Split(Path.GetInvalidFileNameChars()));
		string path = Path.Combine(dir, $"{safeHost}_{opt.Port}.rdp");
		File.WriteAllText(path, BuildRdpContent(opt), Encoding.Unicode);
		return path;
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

	/// <summary>打开主密码对话框（设置或解锁，由 <paramref name="purpose"/> 决定）。</summary>
	/// <returns>用户是否点了确定。</returns>
	bool ShowMasterPassword(MasterPasswordPurpose purpose);

	/// <summary>选择导出目标文件；用户取消返回 null。</summary>
	string? PickExportTarget(string suggestedFileName);

	/// <summary>选择导入来源文件；用户取消返回 null。</summary>
	string? PickImportSource();
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
		if (File.Exists(dbPath))
		{
			File.Copy(dbPath, backup, overwrite: true);
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		SqliteConnection.ClearAllPools();
		try
		{
			File.Copy(sourcePath, dbPath, overwrite: true);
			db.ReinitializeAfterImport();
			Log.Info("Backup import ok: " + sourcePath);
		}
		catch (Exception ex)
		{
			// 回滚失败绝不能静默：用户可能以为数据已还原，实际库文件状态未知。
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
				// 哨兵文件 + Error 日志：下次启动由 App 检测并向用户告警。
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

	public static void SetPort(int port)
	{
		if ((port < 1 || port > 65535) ? true : false)
		{
			throw new ArgumentOutOfRangeException("port", "端口范围必须在 1 到 65535 之间。");
		}
		int currentPort = GetCurrentPort();
		using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Terminal Server\\WinStations\\RDP-Tcp", writable: true) ?? throw new InvalidOperationException("无法打开 RDP-Tcp 注册表项，请确认以管理员身份运行。"))
		{
			registryKey.SetValue("PortNumber", port, RegistryValueKind.DWord);
		}
		RemoveOurFirewallRules(port);
		if (currentPort != port)
		{
			RemoveFirewallRuleForPort(currentPort);
		}
		AddFirewallRule(port);
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

	private static void AddFirewallRule(int port)
	{
		string text = $"{"RemoteDesktopPlus-RDP-"}{port}";
		RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text);
		RunNetsh("advfirewall", "firewall", "add", "rule", "name=" + text, "dir=in", "action=allow", "protocol=TCP", $"localport={port}");
	}

	private static void RemoveFirewallRuleForPort(int port)
	{
		string text = $"{"RemoteDesktopPlus-RDP-"}{port}";
		RunNetsh("advfirewall", "firewall", "delete", "rule", "name=" + text);
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

	private static void RunNetsh(params string[] args)
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
			process?.WaitForExit(5000);
		}
		catch
		{
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 18 / 39：RemoteDesktopPlus.Services/TrayController.cs  →  namespace RemoteDesktopPlus.Services
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Services
{
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

/// <summary>
/// 系统托盘控制器：最小化/关闭到托盘，左键单击恢复，右键菜单（打开/退出）。
/// 新增功能，原版无此能力。
/// </summary>
public class TrayController : IDisposable
{
	private readonly Window _main;
	private readonly NotifyIcon _icon;
	private bool _disposed;
	private bool _exitRequested;

	public TrayController(Window main)
	{
		_main = main;
		_icon = new NotifyIcon
		{
			Text = "小马远程桌面",
			Visible = true
		};
		try
		{
			using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico")).Stream;
			_icon.Icon = new Icon(stream);
		}
		catch
		{
			_icon.Icon = SystemIcons.Application;
		}

		var menu = new ContextMenuStrip();
		menu.Items.Add("打开", null, (_, _) => RestoreFromTray());
		menu.Items.Add("-");
		menu.Items.Add("退出", null, (_, _) => ExitApplication());
		_icon.ContextMenuStrip = menu;
		_icon.DoubleClick += (_, _) => RestoreFromTray();
		_icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) RestoreFromTray(); };

		_main.StateChanged += OnStateChanged;
		_main.Closing += OnClosing;
	}

	private void OnStateChanged(object sender, EventArgs e)
	{
		// 最小化 → 藏进托盘
		if (_main.WindowState == WindowState.Minimized)
		{
			_main.Hide();
		}
	}

	private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		if (!_exitRequested)
		{
			// 点关闭 → 隐藏到托盘而不是退出
			e.Cancel = true;
			_main.Hide();
		}
	}

	private void RestoreFromTray()
	{
		_main.Show();
		_main.WindowState = WindowState.Normal;
		_main.Activate();
		_main.Topmost = true;
		_main.Topmost = false;
	}

	private void ExitApplication()
	{
		_exitRequested = true;
		_icon.Visible = false;
		_main.Close();
		Application.Current.Shutdown();
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_main.StateChanged -= OnStateChanged;
		_main.Closing -= OnClosing;
		_icon.Visible = false;
		_icon.Dispose();
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 19 / 39：RemoteDesktopPlus.ViewModels/MainViewModel.cs  →  namespace RemoteDesktopPlus.ViewModels
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.ViewModels
{
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using GongSolutions.Wpf.DragDrop;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

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

	/// <summary>剪贴板重定向开关，默认开启（见 RdpOptions.RedirectClipboard 的说明）。</summary>
	[ObservableProperty]
	private bool redirectClipboard = true;

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

	/// <summary>在线扫描进行中标志：ScanOnlineCommand 的 CanExecute 依据，防重复触发。</summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor("ScanOnlineCommand")]
	private bool isScanning;

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

	private readonly IRdpLauncher _launcher;

	/// <summary>读选项期间抑制写回，避免「加载即保存」的自我覆盖。</summary>
	private bool _suppressOptionSave;

	public ObservableCollection<ConnectionItem> Connections { get; } = new ObservableCollection<ConnectionItem>();

	public IReadOnlyList<string> ResolutionOptions { get; } = new string[11] { "全屏", "1024*768", "1280*720", "1366*768", "1440*900", "1600*900", "1680*1050", "1920*1080", "1920*1200", "2560*1440", "3840*2160" };

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
	/// 唯一构造入口——组合根显式传入协作者，测试可注入临时库与假对话框。
	/// （原先只能靠静态单例，导致视图模型的行为无法在测试里单独观察。）
	/// <paramref name="launcher"/> 为 RDP 启动抽象（P1-3 实例化）；
	/// 默认参数保留旧调用形态，生产由组合根传入 <see cref="WindowsRdpLauncher.Instance"/>。
	/// </summary>
	public MainViewModel(DatabaseService db, CryptoService crypto, IDialogService dialogs, IRdpLauncher? launcher = null)
	{
		_db = db ?? throw new ArgumentNullException(nameof(db));
		_crypto = crypto ?? throw new ArgumentNullException(nameof(crypto));
		_dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
		_launcher = launcher ?? WindowsRdpLauncher.Instance;

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
			// 无历史设置时保持字段默认 true（首次运行也默认开启剪贴板）
			if (bool.TryParse(_db.GetSetting(SettingKeys.OptionRedirectClipboard), out var result3))
			{
				RedirectClipboard = result3;
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

	partial void OnRedirectClipboardChanged(bool value)
	{
		if (!_suppressOptionSave)
		{
			_db.SetSetting(SettingKeys.OptionRedirectClipboard, value.ToString());
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
}

// ════════════════════════════════════════════════════════════════════════
// 区段 20 / 39：RemoteDesktopPlus.ViewModels/MainViewModel.Connections.cs  →  namespace RemoteDesktopPlus.ViewModels
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.ViewModels
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

/// <summary>
/// 服务器列表职责：加载、增删改、拖拽排序、在线扫描。
/// 见 <see cref="MainViewModel"/> 的类注释中关于 partial 拆分的说明。
/// </summary>
public partial class MainViewModel
{
	public void ReloadConnections()
	{
		Connections.Clear();
		foreach (ConnectionItem item in _db.LoadAll())
		{
			Connections.Add(item);
		}
	}

	public void ConnectItem(ConnectionItem item)
	{
		SelectedConnection = item;
		var (width, height) = ParseResolution(Resolution);
		try
		{
			_launcher.Launch(new RdpLauncher.RdpOptions(item.Host, item.Port, item.UserName, _crypto.Decrypt(item.EncryptedPassword), width, height, RedirectLocalDrive, LocalDriveLetter, ForceConsole, RedirectClipboard));
		}
		catch (Exception ex)
		{
			// 兜底：启动器异常（凭据写入失败/临时目录不可写等）不得带崩 UI。
			Log.Error("发起连接失败: " + item.Host + ":" + item.Port, ex);
			_dialogs.ShowMessage("连接失败：" + ex.Message, "错误", DialogKind.Error);
			return;
		}
		// 连接即视为成功发起，回写最后连接时间（E2 扩展列）。
		if (item.Id > 0)
		{
			long now = DateTimeOffset.Now.ToUnixTimeSeconds();
			item.LastConnectedAt = now;
			_db.UpdateLastConnectedAt(item.Id, now);
		}
	}

	[RelayCommand]
	private void NewItem()
	{
		if (!_dialogs.EditConnection(null, out ConnectionFormResult result))
		{
			return;
		}
		ConnectionItem connectionItem = new ConnectionItem
		{
			Host = result.Host,
			Port = result.Port,
			UserName = result.UserName,
			Remark = result.Remark,
			EncryptedPassword = _crypto.Encrypt(result.Password),
			SortOrder = Connections.Count + 1
		};
		_db.Insert(connectionItem);
		connectionItem.Index = Connections.Count + 1;
		Connections.Add(connectionItem);
		SelectedConnection = connectionItem;
	}

	[RelayCommand(CanExecute = "HasSelected")]
	private void EditSelected()
	{
		if (SelectedConnection == null)
		{
			return;
		}
		if (!_dialogs.EditConnection(SelectedConnection, out ConnectionFormResult result))
		{
			return;
		}
		SelectedConnection.Host = result.Host;
		SelectedConnection.Port = result.Port;
		SelectedConnection.UserName = result.UserName;
		SelectedConnection.Remark = result.Remark;
		SelectedConnection.EncryptedPassword = _crypto.Encrypt(result.Password);
		_db.Update(SelectedConnection);
		// 编辑的是当前选中的连接，表单需要跟着刷新，否则界面显示的是旧值。
		Host = SelectedConnection.Host;
		Port = SelectedConnection.Port;
		UserName = SelectedConnection.UserName;
		Password = result.Password;
	}

	[RelayCommand(CanExecute = "HasSelected")]
	private void DeleteSelected()
	{
		if (SelectedConnection != null && _dialogs.Confirm($"确定删除 {SelectedConnection.Host}:{SelectedConnection.Port} 吗？", "删除确认"))
		{
			_db.Delete(SelectedConnection.Id);
			Connections.Remove(SelectedConnection);
			ReindexConnections();
		}
	}

	private bool HasSelected()
	{
		return SelectedConnection != null;
	}

	/// <summary>把列表顺序（Index）与持久化排序（SortOrder）重新压成 1..N。</summary>
	private void ReindexConnections()
	{
		for (int i = 0; i < Connections.Count; i++)
		{
			Connections[i].Index = i + 1;
			Connections[i].SortOrder = i + 1;
		}
	}

	[RelayCommand(CanExecute = "CanScan")]
	private async Task ScanOnlineAsync()
	{
		IsScanning = true;
		try
		{
			// 并发上限：Task.WhenAll 直发会在连接数大时形成瞬时 socket 风暴，
			// 用信号量把探活压到 20 并发以内；扫描期间命令禁用防叠加触发。
			using SemaphoreSlim gate = new SemaphoreSlim(20, 20);
			await Task.WhenAll(((IEnumerable<ConnectionItem>)Connections).Select((Func<ConnectionItem, Task>)async delegate(ConnectionItem item)
			{
				await gate.WaitAsync().ConfigureAwait(true);
				try
				{
					item.Status = OnlineStatus.Checking;
					item.Status = ((await OnlineProbe.IsOnlineAsync(item.Host, item.Port).ConfigureAwait(true)) ? OnlineStatus.Online : OnlineStatus.Offline);
				}
				finally
				{
					gate.Release();
				}
			}));
		}
		finally
		{
			IsScanning = false;
		}
	}

	private bool CanScan()
	{
		return !IsScanning;
	}

	public void DragOver(IDropInfo dropInfo)
	{
		if (dropInfo.Data is ConnectionItem && dropInfo.TargetItem is ConnectionItem)
		{
			dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
			dropInfo.Effects = DragDropEffects.Move;
		}
	}

	public void Drop(IDropInfo dropInfo)
	{
		if (dropInfo.Data is ConnectionItem item && dropInfo.TargetItem is ConnectionItem)
		{
			int num = Connections.IndexOf(item);
			int num2 = dropInfo.InsertIndex;
			// 往下拖时 InsertIndex 指向「插入到目标之前」的位置，
			// 源项本身也在列表里，先移除再插入等于少算一位，故补偿 1。
			if (num2 > num)
			{
				num2--;
			}
			if (num >= 0 && num2 >= 0 && num != num2)
			{
				Connections.Move(num, num2);
				ReindexConnections();
				_db.UpdateSortOrders(Connections);
			}
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 21 / 39：RemoteDesktopPlus.ViewModels/MainViewModel.Connect.cs  →  namespace RemoteDesktopPlus.ViewModels
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.ViewModels
{
using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

/// <summary>
/// 连接职责：表单校验、保存/更新到列表、发起远程桌面、清理残留凭据。
/// 见 <see cref="MainViewModel"/> 的类注释中关于 partial 拆分的说明。
/// </summary>
public partial class MainViewModel
{
	[RelayCommand(CanExecute = "CanConnect")]
	private void Connect()
	{
		if (string.IsNullOrWhiteSpace(Host))
		{
			_dialogs.ShowMessage("请填写主机 IP 地址。", "提示");
			return;
		}
		SaveOrUpdateForm();
		var (width, height) = ParseResolution(Resolution);
		_launcher.Launch(new RdpLauncher.RdpOptions(Host, Port, UserName, Password, width, height, RedirectLocalDrive, LocalDriveLetter, ForceConsole, RedirectClipboard));
	}

	private bool CanConnect()
	{
		return !string.IsNullOrWhiteSpace(Host);
	}

	[RelayCommand]
	private void SaveItem()
	{
		if (string.IsNullOrWhiteSpace(Host))
		{
			_dialogs.ShowMessage("请填写主机 IP 地址。", "提示");
		}
		else
		{
			SaveOrUpdateForm();
		}
	}

	/// <summary>
	/// 把当前表单写回列表：命中已有项（选中项，或 Host/Port/UserName 三者全同）则更新，否则新增。
	/// </summary>
	private void SaveOrUpdateForm()
	{
		string encryptedPassword = _crypto.Encrypt(Password);
		ConnectionItem connectionItem = SelectedConnection ?? Connections.FirstOrDefault((ConnectionItem c) => c.Host == Host && c.Port == Port && c.UserName == UserName);
		if (connectionItem == null)
		{
			ConnectionItem connectionItem2 = new ConnectionItem
			{
				Host = Host,
				Port = Port,
				UserName = UserName,
				EncryptedPassword = encryptedPassword,
				SortOrder = Connections.Count + 1
			};
			_db.Insert(connectionItem2);
			connectionItem2.Index = Connections.Count + 1;
			Connections.Add(connectionItem2);
			SelectedConnection = connectionItem2;
		}
		else
		{
			connectionItem.Host = Host;
			connectionItem.Port = Port;
			connectionItem.UserName = UserName;
			connectionItem.EncryptedPassword = encryptedPassword;
			_db.Update(connectionItem);
			SelectedConnection = connectionItem;
		}
	}

	[RelayCommand]
	private void RepairCredential()
	{
		if (string.IsNullOrWhiteSpace(Host))
		{
			_dialogs.ShowMessage("请先选择或输入主机。", "提示");
			return;
		}
		_launcher.ClearCredential(Host);
		_dialogs.ShowMessage("已清理 " + Host + " 的旧凭证，下次连接时会重新保存。", "凭证修复");
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 22 / 39：RemoteDesktopPlus.ViewModels/MainViewModel.ServerSettings.cs  →  namespace RemoteDesktopPlus.ViewModels
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.ViewModels
{
using System;
using CommunityToolkit.Mvvm.Input;
using RemoteDesktopPlus.Services;

/// <summary>
/// 服务器设置职责：本机远程桌面开关、RDP 端口读写与状态刷新。
/// 这些操作都会改注册表 / 防火墙，属于需要管理员权限的路径，故统一在此收口。
/// 见 <see cref="MainViewModel"/> 的类注释中关于 partial 拆分的说明。
/// </summary>
public partial class MainViewModel
{
	/// <summary>
	/// 状态轮询定时器。存在的理由：RDP 是否在监听会因外部因素变化
	/// （服务被停、端口被改、进程被挤占），而 ViewModel 只在构造与用户操作后刷新一次，
	/// 界面会长期停在启动瞬间的快照上，显示与事实不符。
	/// 用 DispatcherTimer 而非 System.Timers.Timer：后者的回调在后台线程，
	/// 直接写 ObservableProperty 会触发跨线程 UI 访问异常。
	/// </summary>
	private System.Windows.Threading.DispatcherTimer? _statusTimer;

	/// <summary>
	/// 启动状态轮询（幂等：重复调用不会叠加定时器）。
	/// 由视图在 Loaded 时调用——构造期还没有 Dispatcher，建不出有效定时器。
	/// </summary>
	public void StartStatusPolling(TimeSpan? interval = null)
	{
		if (_statusTimer != null)
		{
			return;
		}
		_statusTimer = new System.Windows.Threading.DispatcherTimer
		{
			Interval = interval ?? TimeSpan.FromSeconds(3)
		};
		_statusTimer.Tick += (_, _) => SafeRefreshServerStatus();
		_statusTimer.Start();
		SafeRefreshServerStatus();
	}

	/// <summary>停止轮询，供窗口关闭时调用（避免定时器持引用导致不释放）。</summary>
	public void StopStatusPolling()
	{
		if (_statusTimer == null)
		{
			return;
		}
		_statusTimer.Stop();
		_statusTimer = null;
	}

	/// <summary>
	/// 轮询专用刷新：三条探测中任何一条抛异常（注册表无权限、服务查询失败等）
	/// 都不能让定时器回调炸掉界面，故整体兜住并降级为「保持上次值」。
	/// </summary>
	private void SafeRefreshServerStatus()
	{
		try
		{
			RefreshServerStatus();
		}
		catch (Exception ex)
		{
			Log.Warn("刷新远程桌面状态失败", ex);
		}
	}

	public void RefreshServerStatus()
	{
		IsRemoteEnabled = RdpServerService.IsRemoteEnabled();
		CurrentRdpPort = RdpServerService.GetCurrentPort();
		IsPortListening = RdpServerService.IsPortListening(CurrentRdpPort);
	}

	[RelayCommand]
	private void ToggleRemote()
	{
		try
		{
			RdpServerService.SetRemoteEnabled(!IsRemoteEnabled);
			RefreshServerStatus();
			_dialogs.ShowMessage(IsRemoteEnabled ? "远程桌面已开启。" : "远程桌面已关闭。", "提示");
		}
		catch (Exception ex)
		{
			_dialogs.ShowMessage("操作失败：" + ex.Message, "错误", DialogKind.Error);
		}
	}

	[RelayCommand]
	private void ChangePort()
	{
		int result;
		bool flag = !int.TryParse(NewPort, out result);
		if (!flag)
		{
			flag = result < 1 || result > 65535;
		}
		if (flag)
		{
			_dialogs.ShowMessage("请输入有效端口（1-65535）。", "提示");
			return;
		}
		try
		{
			RdpServerService.SetPort(result);
			RefreshServerStatus();
			_dialogs.ShowMessage($"端口已修改为 {result}，重启后生效。", "提示");
		}
		catch (Exception ex)
		{
			_dialogs.ShowMessage("修改失败：" + ex.Message, "错误", DialogKind.Error);
		}
	}

	[RelayCommand]
	private void RestorePort()
	{
		try
		{
			RdpServerService.SetPort(3389);
			RefreshServerStatus();
			_dialogs.ShowMessage("端口已还原为默认 3389，重启后生效。", "提示");
		}
		catch (Exception ex)
		{
			_dialogs.ShowMessage("还原失败：" + ex.Message, "错误", DialogKind.Error);
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 23 / 39：RemoteDesktopPlus.ViewModels/MainViewModel.SecuritySettings.cs  →  namespace RemoteDesktopPlus.ViewModels
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.ViewModels
{
using System;
using CommunityToolkit.Mvvm.Input;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

/// <summary>
/// 加密设置职责：启动密码、主密码，以及数据库的导入导出。
/// 这三个动作都会改变「密文形态」（切 DPAPI ↔ 主密码派生），因此放在一起。
/// 见 <see cref="MainViewModel"/> 的类注释中关于 partial 拆分的说明。
/// </summary>
public partial class MainViewModel
{
	[RelayCommand]
	private void SetLaunchPassword()
	{
		_dialogs.SetLaunchPassword();
	}

	[RelayCommand]
	private void SetMasterPassword()
	{
		if (_dialogs.ShowMasterPassword(MasterPasswordPurpose.Set))
		{
			HasMasterPassword = _crypto.IsMasterPasswordConfigured();
			ReloadConnections();
		}
	}

	[RelayCommand]
	private void ExportData()
	{
		string fileName = _dialogs.PickExportTarget($"RemoteDesktopPlus-{DateTime.Now:yyyyMMdd-HHmmss}.db");
		if (fileName == null)
		{
			return;
		}
		try
		{
			BackupService.Export(fileName);
			_dialogs.ShowMessage("导出成功。", "提示");
		}
		catch (Exception ex)
		{
			_dialogs.ShowMessage("导出失败：" + ex.Message, "错误", DialogKind.Error);
		}
	}

	[RelayCommand]
	private void ImportData()
	{
		string fileName = _dialogs.PickImportSource();
		if (fileName == null
			|| !_dialogs.Confirm("导入会覆盖当前数据库（旧文件已自动备份为 data.db.bak）。\n如果原数据使用主密码加密，导入后请设置或解锁同一主密码。\n\n是否继续？", "确认导入", DialogKind.Warning))
		{
			return;
		}
		try
		{
			BackupService.Import(fileName);
			HasMasterPassword = _crypto.IsMasterPasswordConfigured();
			if (HasMasterPassword)
			{
				// 导入的库可能换了主密码，必须立刻要求解锁，否则读出来全是空串。
				_dialogs.ShowMasterPassword(MasterPasswordPurpose.Unlock);
			}
			ReloadConnections();
			_dialogs.ShowMessage("导入成功。", "提示");
		}
		catch (Exception ex)
		{
			_dialogs.ShowMessage("导入失败：" + ex.Message, "错误", DialogKind.Error);
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 24 / 39：RemoteDesktopPlus.Views/AboutWindow.cs  →  namespace RemoteDesktopPlus.Views
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Views
{
using System.Windows;

/// <summary>
/// 关于窗口。视图结构见 Views/AboutWindow.xaml。
/// </summary>
public partial class AboutWindow : Window
{
	public AboutWindow()
	{
		InitializeComponent();
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 25 / 39：RemoteDesktopPlus.Views/LoginWindow.cs  →  namespace RemoteDesktopPlus.Views
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Views
{
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RemoteDesktopPlus.Services;

/// <summary>
/// 启动密码解锁窗口。视图结构见 Views/LoginWindow.xaml。
/// 依赖由组合根（WpfDialogService）注入，不再直连静态单例（P1-1 测试盲区修复）。
/// </summary>
public partial class LoginWindow : Window
{
	private const int MaxFailures = 5;

	private readonly DatabaseService _db;
	private readonly CryptoService _crypto;

	private int _failures;

	public LoginWindow(DatabaseService? db = null, CryptoService? crypto = null)
	{
		// 默认参数保留旧调用形态兼容；组合根注入后可测试。
		_db = db ?? DatabaseService.Instance;
		_crypto = crypto ?? CryptoService.Instance;
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
		if (e.Key == Key.Enter)
		{
			TryLogin();
		}
	}

	private void TryLogin()
	{
		string hashB = _db.GetSetting(SettingKeys.LaunchPasswordHash) ?? string.Empty;
		string saltB = _db.GetSetting(SettingKeys.LaunchPasswordSalt) ?? string.Empty;
		if (CryptoService.VerifyLaunchPassword(PwdBox.Password, hashB, saltB))
		{
			base.DialogResult = true;
			Close();
			return;
		}
		_failures++;
		PwdBox.Clear();
		if (_failures >= MaxFailures)
		{
			MessageBox.Show("密码错误次数过多，程序将退出。", "提示", MessageBoxButton.OK, MessageBoxImage.Hand);
			base.DialogResult = false;
			Close();
		}
		else
		{
			MessageBox.Show($"密码错误，剩余 {MaxFailures - _failures} 次尝试。", "提示", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 26 / 39：RemoteDesktopPlus.Views/SetLaunchPasswordWindow.cs  →  namespace RemoteDesktopPlus.Views
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Views
{
using System.Windows;
using System.Windows.Controls;
using RemoteDesktopPlus.Services;

/// <summary>
/// 设置/修改启动密码的窗口。视图结构见 Views/SetLaunchPasswordWindow.xaml。
/// 依赖由组合根注入，不再直连静态单例（P1-1）。
/// </summary>
public partial class SetLaunchPasswordWindow : Window
{
	private readonly DatabaseService _db;
	private readonly CryptoService _crypto;

	public SetLaunchPasswordWindow(DatabaseService? db = null, CryptoService? crypto = null)
	{
		_db = db ?? DatabaseService.Instance;
		_crypto = crypto ?? CryptoService.Instance;
		InitializeComponent();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		base.DialogResult = false;
		Close();
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		DatabaseService db = _db;
		if (db.HasLaunchPassword())
		{
			string hashB = db.GetSetting(SettingKeys.LaunchPasswordHash) ?? string.Empty;
			string saltB = db.GetSetting(SettingKeys.LaunchPasswordSalt) ?? string.Empty;
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
				db.DeleteSetting(SettingKeys.LaunchPasswordHash);
				db.DeleteSetting(SettingKeys.LaunchPasswordSalt);
				MessageBox.Show("启动密码已移除。", "提示");
				base.DialogResult = true;
				Close();
			}
			return;
		}
		if (NewPwd.Password != ConfirmPwd.Password)
		{
			MessageBox.Show("两次输入的新密码不一致。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		string hash = CryptoService.HashLaunchPassword(NewPwd.Password, out string salt);
		db.SetSetting(SettingKeys.LaunchPasswordHash, hash);
		db.SetSetting(SettingKeys.LaunchPasswordSalt, salt);
		MessageBox.Show("启动密码已设置。", "提示");
		base.DialogResult = true;
		Close();
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 27 / 39：RemoteDesktopPlus.Views/MasterPasswordWindow.cs  →  namespace RemoteDesktopPlus.Views
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Views
{
using System.Windows;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

/// <summary>
/// 设置 / 修改 / 解锁主密码的窗口。视图结构见 Views/MasterPasswordWindow.xaml。
/// 依赖由组合根注入，不再直连静态单例（P1-1）。
/// </summary>
public partial class MasterPasswordWindow : Window
{
	private readonly DatabaseService _db;
	private readonly CryptoService _crypto;

	public string TitleText { get; }

	public Visibility ShowOldPassword { get; }

	public MasterPasswordPurpose Purpose { get; }

	public MasterPasswordWindow(MasterPasswordPurpose purpose, DatabaseService? db = null, CryptoService? crypto = null)
	{
		_db = db ?? DatabaseService.Instance;
		_crypto = crypto ?? CryptoService.Instance;
		InitializeComponent();
		Purpose = purpose;
		bool configured = _crypto.IsMasterPasswordConfigured();
		// 解锁场景必然要填原主密码；设置场景仅在「已有主密码、需先验原密码」时才需要。
		// 一次算清，避免在下方分支里再改一次只读属性。
		ShowOldPassword = purpose == MasterPasswordPurpose.Unlock || configured
			? Visibility.Visible
			: Visibility.Collapsed;
		TitleText = purpose switch
		{
			MasterPasswordPurpose.Set => configured ? "修改主密码" : "设置主密码",
			MasterPasswordPurpose.Unlock => "请输入主密码以解锁",
			_ => "主密码",
		};
		base.DataContext = this;
		if (purpose == MasterPasswordPurpose.Unlock)
		{
			// 解锁场景只需要输入原主密码，隐藏新密码相关控件。
			NewPwd.Visibility = Visibility.Collapsed;
			ConfirmPwd.Visibility = Visibility.Collapsed;
			OldPwd.Visibility = Visibility.Visible;
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
			if (_crypto.Unlock(OldPwd.Password))
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
		if (_crypto.IsMasterPasswordConfigured() && !_crypto.HasMasterKey && !_crypto.Unlock(OldPwd.Password))
		{
			MessageBox.Show("原主密码不正确。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		var existingItems = _db.LoadAll();
		if (string.IsNullOrEmpty(NewPwd.Password))
		{
			if (MessageBox.Show("新主密码为空，将移除主密码并改回 DPAPI。是否继续？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				_crypto.RemoveMasterPassword(existingItems);
				MessageBox.Show("主密码已移除。", "提示");
				base.DialogResult = true;
				Close();
			}
			return;
		}
		if (NewPwd.Password != ConfirmPwd.Password)
		{
			MessageBox.Show("两次输入不一致。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		_crypto.SetMasterPassword(NewPwd.Password, existingItems);
		MessageBox.Show("主密码已保存。", "提示");
		base.DialogResult = true;
		Close();
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 28 / 39：RemoteDesktopPlus.Views/EditConnectionWindow.cs  →  namespace RemoteDesktopPlus.Views
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Views
{
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

/// <summary>
/// 添加/编辑服务器的对话框。视图结构见 Views/EditConnectionWindow.xaml。
/// </summary>
public partial class EditConnectionWindow : Window, INotifyPropertyChanged
{
	private string _plainPassword = string.Empty;

	private bool _showPlainPassword;

	private bool _suppressPwdChanged;

	private readonly CryptoService _crypto;

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

	public EditConnectionWindow(ConnectionItem? source = null, CryptoService? crypto = null)
	{
		_crypto = crypto ?? CryptoService.Instance;
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
			string plain = _crypto.Decrypt(source.EncryptedPassword);
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
}

// ════════════════════════════════════════════════════════════════════════
// 区段 29 / 39：RemoteDesktopPlus.Views/MainWindow.cs  →  namespace RemoteDesktopPlus.Views
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Views
{
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.ViewModels;

/// <summary>
/// 主窗口。视图结构见 Views/MainWindow.xaml。
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
	private bool _showPlainPasswordOnMain;

	public bool ShowPlainPasswordOnMain
	{
		get
		{
			return _showPlainPasswordOnMain;
		}
		set
		{
			if (_showPlainPasswordOnMain == value)
			{
				return;
			}
			_showPlainPasswordOnMain = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public MainWindow(MainViewModel viewModel)
	{
		InitializeComponent();
		base.DataContext = viewModel;
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is not MainViewModel mainViewModel)
		{
			return;
		}
		mainViewModel.PropertyChanged += delegate(object? _, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(MainViewModel.Password) && PwdBox.Password != mainViewModel.Password)
			{
				PwdBox.Password = mainViewModel.Password;
			}
		};
		// 启动 RDP 状态轮询：窗口已显示，Dispatcher 可用
		mainViewModel.StartStatusPolling();
		base.Closed += (_, _) => mainViewModel.StopStatusPolling();
	}

	private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (sender is PasswordBox passwordBox && base.DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.Password = passwordBox.Password;
		}
	}

	private void MinimizeButton_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void AboutButton_Click(object sender, RoutedEventArgs e)
	{
		AboutWindow aboutWindow = new AboutWindow
		{
			Owner = this
		};
		aboutWindow.ShowDialog();
	}

	private void ConnList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is not DataGrid)
		{
			return;
		}
		DataGridRow? row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
		if (row?.Item is ConnectionItem item && base.DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.ConnectItem(item);
		}
	}

	private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
	{
		while (d != null)
		{
			if (d is T t)
			{
				return t;
			}
			d = VisualTreeHelper.GetParent(d);
		}
		return null;
	}

	private void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 30 / 39：RemoteDesktopPlus.Views/WpfDialogService.cs  →  namespace RemoteDesktopPlus.Views
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Views
{
using System.Windows;
using Microsoft.Win32;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

/// <summary>
/// <see cref="IDialogService"/> 的 WPF 实现——整个程序里唯一一处把「视图模型的需求」翻译成
/// 「具体窗口」的地方。ViewModel 只依赖接口，不含任何 Window 类型。
/// 持有服务依赖并下传给各对话框窗口（P1-1：对话框不再直连静态单例，可测试）。
/// </summary>
public sealed class WpfDialogService : IDialogService
{
	private readonly DatabaseService _db;
	private readonly CryptoService _crypto;

	public WpfDialogService(DatabaseService? db = null, CryptoService? crypto = null)
	{
		_db = db ?? DatabaseService.Instance;
		_crypto = crypto ?? CryptoService.Instance;
	}

	public void ShowMessage(string message, string title, DialogKind kind = DialogKind.Information)
	{
		Window? owner = Owner();
		if (owner != null)
		{
			MessageBox.Show(owner, message, title, MessageBoxButton.OK, ToImage(kind));
		}
		else
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, ToImage(kind));
		}
	}

	public bool Confirm(string message, string title, DialogKind kind = DialogKind.Warning)
	{
		Window? owner = OwnerOrNull();
		MessageBoxResult result = owner == null
			? MessageBox.Show(message, title, MessageBoxButton.YesNo, ToImage(kind))
			: MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, ToImage(kind));
		return result == MessageBoxResult.Yes;
	}

	public bool EditConnection(ConnectionItem? source, out ConnectionFormResult result)
	{
		EditConnectionWindow window = source == null
			? new EditConnectionWindow(null, _crypto)
			: new EditConnectionWindow(source, _crypto);
		window.Owner = OwnerOrNull();
		bool ok = window.ShowDialog().GetValueOrDefault();
		if (ok)
		{
			result = window.Result;
		}
		else
		{
			// 独立实例而非 null：调用方在任何分支都能安全读取字段。
			result = new ConnectionFormResult();
		}
		return ok;
	}

	public bool SetLaunchPassword()
	{
		SetLaunchPasswordWindow window = new SetLaunchPasswordWindow(_db, _crypto);
		window.Owner = OwnerOrNull();
		return window.ShowDialog().GetValueOrDefault();
	}

	public bool ShowMasterPassword(MasterPasswordPurpose purpose)
	{
		MasterPasswordWindow window = new MasterPasswordWindow(purpose, _db, _crypto);
		window.Owner = OwnerOrNull();
		return window.ShowDialog().GetValueOrDefault();
	}

	public string? PickExportTarget(string suggestedFileName)
	{
		SaveFileDialog dialog = new SaveFileDialog
		{
			Filter = "数据备份 (*.db)|*.db|所有文件 (*.*)|*.*",
			FileName = suggestedFileName
		};
		return dialog.ShowDialog().GetValueOrDefault() ? dialog.FileName : null;
	}

	public string? PickImportSource()
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Filter = "数据备份 (*.db)|*.db|所有文件 (*.*)|*.*"
		};
		return dialog.ShowDialog().GetValueOrDefault() ? dialog.FileName : null;
	}

	private static MessageBoxImage ToImage(DialogKind kind)
	{
		return kind switch
		{
			DialogKind.Error => MessageBoxImage.Hand,
			DialogKind.Warning => MessageBoxImage.Exclamation,
			_ => MessageBoxImage.Asterisk,
		};
	}

	private static Window? OwnerOrNull()
	{
		return Application.Current?.MainWindow;
	}

	/// <summary>
	/// 无主窗口时返回 null（例如启动阶段）。MessageBox.Show 接受 null owner，
	/// 等价于无 owner 重载；不再 new 临时 Window（P3-a：从不 Show 也从不关闭的无意义分配）。
	/// </summary>
	private static Window? Owner()
	{
		return Application.Current?.MainWindow;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 31 / 39：RemoteDesktopPlus.Converters/BoolToBrushConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

public sealed class BoolToBrushConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return new SolidColorBrush((value is int num && num != 0) ? Color.FromRgb(26, 174, 57) : Color.FromRgb(163, 158, 152));
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 32 / 39：RemoteDesktopPlus.Converters/BoolToEyeIconConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

public sealed class BoolToEyeIconConverter : IValueConverter
{
	public static readonly BoolToEyeIconConverter Instance = new BoolToEyeIconConverter();

	private static readonly Geometry EyeOpen = Geometry.Parse("M12,4.5C7,4.5 2.7,7.6 1,12 2.7,16.4 7,19.5 12,19.5 17,19.5 21.3,16.4 23,12 21.3,7.6 17,4.5 12,4.5z M12,17A5,5 0 0 1 7,12 5,5 0 0 1 12,7 5,5 0 0 1 17,12 5,5 0 0 1 12,17z M12,9A3,3 0 0 0 9,12 3,3 0 0 0 12,15 3,3 0 0 0 15,12 3,3 0 0 0 12,9z");

	private static readonly Geometry EyeClosed = Geometry.Parse("M2,5.27L3.28,4 20,20.72 18.73,22 15.65,18.92C14.5,19.3 13.28,19.5 12,19.5 7,19.5 2.73,16.39 1,12 1.69,10.24 2.79,8.69 4.19,7.46L2,5.27z M12,9A3,3 0 0 1 15,12 3,3 0 0 1 14.83,13.12L11.88,10.18C12.25,10.06 12.62,10 13,10z M12,4.5C17,4.5 21.27,7.61 23,12 22.18,14.08 20.79,15.88 19,17.19L17.58,15.76C18.94,14.82 20.06,13.54 20.82,12 19.17,8.64 15.76,6.5 12,6.5 10.91,6.5 9.84,6.68 8.84,7L7.3,5.47C8.74,4.85 10.32,4.5 12,4.5z M3.18,12C4.83,15.36 8.24,17.5 12,17.5 12.69,17.5 13.37,17.43 14,17.29L11.72,15A3,3 0 0 1 9,12.28L5.6,8.87C4.61,9.72 3.78,10.78 3.18,12z");

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (!(value is bool) || !(bool)value)
		{
			return EyeClosed;
		}
		return EyeOpen;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 33 / 39：RemoteDesktopPlus.Converters/BoolToOnOffConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows.Data;

public sealed class BoolToOnOffConverter : IValueConverter
{
	public static readonly BoolToOnOffConverter Instance = new BoolToOnOffConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (!(value is bool) || !(bool)value)
		{
			return "关闭";
		}
		return "开启";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 34 / 39：RemoteDesktopPlus.Converters/BoolToToggleTextConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows.Data;

public sealed class BoolToToggleTextConverter : IValueConverter
{
	public static readonly BoolToToggleTextConverter Instance = new BoolToToggleTextConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (!(value is bool) || !(bool)value)
		{
			return "开启远程";
		}
		return "关闭远程";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 35 / 39：RemoteDesktopPlus.Converters/BoolToVisibilityConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

public sealed class BoolToVisibilityConverter : IValueConverter
{
	public static readonly BoolToVisibilityConverter Instance = new BoolToVisibilityConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return (!(value is bool) || !(bool)value) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is Visibility visibility && visibility == Visibility.Visible;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 36 / 39：RemoteDesktopPlus.Converters/InverseBoolToVisibilityConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
	public static readonly InverseBoolToVisibilityConverter Instance = new InverseBoolToVisibilityConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is Visibility visibility && visibility == Visibility.Collapsed;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 37 / 39：RemoteDesktopPlus.Converters/StatusToBrushConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RemoteDesktopPlus.Models;

public sealed class StatusToBrushConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is OnlineStatus)
		{
			switch ((OnlineStatus)value)
			{
			case OnlineStatus.Online:
				return new SolidColorBrush(Color.FromRgb(26, 174, 57));
			case OnlineStatus.Offline:
				return new SolidColorBrush(Color.FromRgb(224, 49, 49));
			case OnlineStatus.Checking:
				return new SolidColorBrush(Color.FromRgb(221, 91, 0));
			}
		}
		return new SolidColorBrush(Color.FromRgb(163, 158, 152));
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 38 / 39：RemoteDesktopPlus.Converters/StatusToTextConverter.cs  →  namespace RemoteDesktopPlus.Converters
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus.Converters
{
using System;
using System.Globalization;
using System.Windows.Data;
using RemoteDesktopPlus.Models;

public sealed class StatusToTextConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is OnlineStatus)
		{
			switch ((OnlineStatus)value)
			{
			case OnlineStatus.Online:
				return "在线";
			case OnlineStatus.Offline:
				return "离线";
			case OnlineStatus.Checking:
				return "检测中";
			}
		}
		return "未知";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
}

// ════════════════════════════════════════════════════════════════════════
// 区段 39 / 39：RemoteDesktopPlus/App.cs  →  namespace RemoteDesktopPlus
// ════════════════════════════════════════════════════════════════════════
namespace RemoteDesktopPlus
{
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;
using RemoteDesktopPlus.Views;

/// <summary>
/// 应用入口。视图结构见 App.xaml；<c>InitializeComponent</c> 与 <c>Main</c> 由 XAML 编译器生成
/// （见 obj\**\App.g.cs），故此处不再手写。
/// </summary>
public partial class App : Application
{
	private const string MutexName = "Local\\RemoteDesktopPlusPlus.SingleInstance.Mutex";

	private const string PipeName = "RemoteDesktopPlusPlus.SingleInstance.Activate";

	private Mutex? _instanceMutex;

	private CancellationTokenSource? _pipeCts;

	private TrayController? _tray;

	protected override void OnStartup(StartupEventArgs e)
	{
		SQLitePCL.Batteries.Init();
		// 单实例：第二个实例通过命名管道唤醒已运行的实例后自行退出。
		_instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
		if (!createdNew)
		{
			try
			{
				using NamedPipeClientStream pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
				pipe.Connect(800);
			}
			catch (Exception ex)
			{
				Log.Debug("单实例唤醒失败（不致命）: " + ex.Message);
			}
			try
			{
				_instanceMutex.Dispose();
			}
			catch
			{
			}
			_instanceMutex = null;
			Shutdown();
			return;
		}

		base.OnStartup(e);
		Log.Info("应用启动，基线组合根装配开始");
		RdpLauncher.CleanupTempFiles();
		DatabaseService db = DatabaseService.Instance;
		CryptoService crypto = CryptoService.Instance;
		try
		{
			db.Initialize();
		}
		catch (Exception ex)
		{
			// 数据库损坏/磁盘只读/权限异常时给出可理解的兜底，而不是裸崩溃。
			Log.Error("数据库初始化失败: " + db.DbPath, ex);
			MessageBox.Show(
				"数据文件初始化失败，程序无法继续启动。\n\n位置: " + db.DbPath
				+ "\n\n异常信息: " + ex.Message
				+ "\n\n建议：检查磁盘可写权限；若数据文件损坏，可将其移走后重新启动（或用 data.db.bak 恢复）。",
				"启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
			Shutdown();
			return;
		}
		// 组合根：整个程序只有这里把服务实例与视图实现装配到一起。
		WpfDialogService dialogs = new WpfDialogService();

		if (db.HasLaunchPassword() && !new LoginWindow(db, crypto).ShowDialog().GetValueOrDefault())
		{
			Shutdown();
			return;
		}
		if (crypto.IsMasterPasswordConfigured() && !crypto.HasMasterKey)
		{
			dialogs.ShowMasterPassword(MasterPasswordPurpose.Unlock);
		}

		// 上次导入回滚失败的哨兵文件：必须让用户知道数据可能不一致。
		string sentinel = Path.Combine(AppContext.BaseDirectory, "data.db.import-failed");
		if (File.Exists(sentinel))
		{
			string detail = File.ReadAllText(sentinel);
			Log.Error("检测到上次导入回滚失败的哨兵文件: " + sentinel);
			MessageBox.Show("上次数据导入失败且自动恢复未完成，数据文件可能处于不一致状态。\n\n" + detail
				+ "\n建议：不要继续覆盖现有数据，检查 " + db.DbPath + ".bak 备份是否存在。",
				"数据导入异常", MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		MainViewModel viewModel = new MainViewModel(db, crypto, dialogs, WindowsRdpLauncher.Instance);
		MainWindow mainWindow = new MainWindow(viewModel);
		base.MainWindow = mainWindow;
		mainWindow.Closed += delegate
		{
			Shutdown();
		};
		mainWindow.Show();
		_tray = new TrayController(mainWindow);
		StartActivationListener(mainWindow);
	}

	private void StartActivationListener(Window main)
	{
		_pipeCts = new CancellationTokenSource();
		CancellationToken token = _pipeCts.Token;
		Task.Run(async delegate
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					using NamedPipeServerStream server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
					await server.WaitForConnectionAsync(token).ConfigureAwait(continueOnCapturedContext: false);
					Dispatcher.Invoke(delegate
					{
						BringToFront(main);
					});
				}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				Log.Debug("命名管道监听异常，300ms 后重试: " + ex.Message);
				try
				{
					await Task.Delay(300, token).ConfigureAwait(continueOnCapturedContext: false);
				}
				catch
				{
					break;
				}
			}
			}
		}, token);
	}

	private static void BringToFront(Window window)
	{
		if (window.WindowState == WindowState.Minimized)
		{
			window.WindowState = WindowState.Normal;
		}
		if (!window.IsVisible)
		{
			window.Show();
		}
		window.Activate();
		window.Topmost = true;
		window.Topmost = false;
		window.Focus();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		try
		{
			_tray?.Dispose();
		}
		catch
		{
		}
		try
		{
			_pipeCts?.Cancel();
			_pipeCts?.Dispose();
		}
		catch
		{
		}
		try
		{
			_instanceMutex?.ReleaseMutex();
		}
		catch (ApplicationException)
		{
		}
		catch
		{
		}
		try
		{
			_instanceMutex?.Dispose();
		}
		catch
		{
		}
		base.OnExit(e);
	}
}
}
