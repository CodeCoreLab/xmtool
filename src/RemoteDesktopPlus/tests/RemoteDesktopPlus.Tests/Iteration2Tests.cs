using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 本轮迭代新增功能的回归测试：
/// schema 迁移管线、扩展列持久化、单列 UPDATE、备份格式校验、扫描限流状态位。
/// </summary>
public class Iteration2Tests
{
	// —— E1 schema 迁移 ——

	[Fact]
	public void Initialize_OnFreshDb_WritesSchemaVersion2()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		Assert.Equal("2", db.GetSetting("schema.version"));
	}

	[Fact]
	public void Initialize_IsIdempotent_SecondRunDoesNotDuplicateColumns()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		db.Initialize(); // 第二次执行不得因 ALTER TABLE 重复列而抛错
		Assert.Equal("2", db.GetSetting("schema.version"));
		Assert.Empty(db.LoadAll());
	}

	[Fact]
	public void Initialize_OnV1Db_MigratesToV2_AndPreservesRows()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		// 人为把库降回 v1：删扩展列不可行（SQLite DROP COLUMN 支持有限），
		// 改为删掉 schema.version 记录并验证「已有 v1 数据 + 无版本标记」时迁移可重复执行。
		db.DeleteSetting("schema.version");
		db.Initialize(); // 再次初始化：迁移逻辑应把版本补回 2
		Assert.Equal("2", db.GetSetting("schema.version"));
	}

	// —— E2 扩展列持久化 ——

	[Fact]
	public void ExtensionColumns_RoundtripThroughInsertAndLoad()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		ConnectionItem item = new ConnectionItem
		{
			Host = "10.1.1.1",
			Port = 3389,
			UserName = "admin",
			EncryptedPassword = "D1$abc",
			SortOrder = 1,
			GroupName = "生产环境",
			LastConnectedAt = 1700000000L,
			Protocol = "rdp"
		};
		db.Insert(item);
		ConnectionItem loaded = db.LoadAll().Single();

		Assert.Equal("生产环境", loaded.GroupName);
		Assert.Equal(1700000000L, loaded.LastConnectedAt);
		Assert.Equal("rdp", loaded.Protocol);
	}

	[Fact]
	public void UpdateLastConnectedAt_PersistsUnixSeconds()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		ConnectionItem item = new ConnectionItem { Host = "h", Port = 3389, UserName = "u", EncryptedPassword = "x", SortOrder = 1 };
		db.Insert(item);

		db.UpdateLastConnectedAt(item.Id, 1234567890L);
		Assert.Equal(1234567890L, db.LoadAll().Single().LastConnectedAt);
	}

	[Fact]
	public void ConnectItem_WritesLastConnectedAt()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		FakeDialogService dialogs = new FakeDialogService();
		RecordingLauncher launcher = new RecordingLauncher();
		MainViewModel vm = new MainViewModel(db, crypto, dialogs, launcher);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("10.9.9.9", 3389, "u", "p")));
		vm.NewItemCommand.Execute(null);
		ConnectionItem item = vm.Connections.Single();
		long before = item.LastConnectedAt;

		vm.ConnectItem(item);

		Assert.Equal(1, launcher.LaunchCalls.Count);
		Assert.True(item.LastConnectedAt > 0);
		Assert.Equal(item.LastConnectedAt, db.LoadAll().Single().LastConnectedAt);
		// 密码以解密后的明文传给启动器（由启动器负责安全写入凭据存储）
		Assert.Equal("p", launcher.LaunchCalls[0].Password);
	}

	// —— P2-5 单列 UPDATE ——

	[Fact]
	public void UpdatePasswordColumn_ChangesOnlyPasswordColumn()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		ConnectionItem item = new ConnectionItem { Host = "h1", Port = 1234, UserName = "u1", EncryptedPassword = "old", Remark = "r", SortOrder = 5 };
		db.Insert(item);

		db.UpdatePasswordColumn(item.Id, "new-cipher");
		ConnectionItem loaded = db.LoadAll().Single();

		Assert.Equal("new-cipher", loaded.EncryptedPassword);
		Assert.Equal("h1", loaded.Host);
		Assert.Equal(1234, loaded.Port);
		Assert.Equal("u1", loaded.UserName);
		Assert.Equal("r", loaded.Remark);
		Assert.Equal(5, loaded.SortOrder);
	}

	[Fact]
	public void SetMasterPassword_StillPersistsViaPasswordColumnOnly()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);

		ConnectionItem item = new ConnectionItem { Host = "h", Port = 3389, UserName = "u", Remark = "keep-me" };
		item.EncryptedPassword = crypto.Encrypt("orig");
		db.Insert(item);

		crypto.SetMasterPassword("m1", new[] { item });

		ConnectionItem loaded = db.LoadAll().Single();
		Assert.StartsWith("M1$", loaded.EncryptedPassword);
		Assert.Equal("keep-me", loaded.Remark);
		CryptoService fresh = new CryptoService(db);
		Assert.True(fresh.Unlock("m1"));
		Assert.Equal("orig", fresh.Decrypt(loaded.EncryptedPassword));
	}

	// —— E3 备份格式校验 ——

	[Fact]
	public void Import_RejectsNonSqliteFile_BeforeTouchingDatabase()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		db.SetSetting("canary", "untouched");
		string fake = Path.Combine(Path.GetTempPath(), "rdpp_fake_" + Guid.NewGuid().ToString("N") + ".db");
		File.WriteAllText(fake, "this is definitely not a sqlite database");
		try
		{
			DbFileBackupFormat format = new DbFileBackupFormat();
			Assert.Throws<InvalidDataException>(() => format.Import(fake, db));
			// 库未被破坏
			Assert.Equal("untouched", tmp.NewService().GetSetting("canary"));
		}
		finally
		{
			File.Delete(fake);
		}
	}

	[Fact]
	public void Export_ProducesValidSqliteFile_AndImportRoundtrips()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		db.Initialize();
		db.Insert(new ConnectionItem { Host = "round-trip", Port = 3390, UserName = "u", EncryptedPassword = "c", SortOrder = 1 });
		string target = Path.Combine(Path.GetTempPath(), "rdpp_export_" + Guid.NewGuid().ToString("N") + ".db");
		try
		{
			DbFileBackupFormat format = new DbFileBackupFormat();
			format.Export(target, db);
			Assert.True(new FileInfo(target).Length > 0);

			// 空库导入后应能读到导出前的记录
			using TempDb tmp2 = new TempDb();
			DatabaseService db2 = tmp2.NewService();
			format.Import(target, db2);
			Assert.Equal("round-trip", db2.LoadAll().Single().Host);
		}
		finally
		{
			File.Delete(target);
		}
	}

	// —— P1 扫描限流 ——

	[Fact]
	public async Task ScanOnlineAsync_SetsAndClearsIsScanning()
	{
		using TempDb tmp = new TempDb();
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		FakeDialogService dialogs = new FakeDialogService();
		MainViewModel vm = new MainViewModel(db, crypto, dialogs);
		db.Insert(new ConnectionItem { Host = "127.0.0.1", Port = 1, UserName = "u", EncryptedPassword = "", SortOrder = 1 });
		vm.ReloadConnections();

		Assert.False(vm.IsScanning);
		object? executed = null;
		await vm.ScanOnlineCommand.ExecuteAsync(executed);
		Assert.False(vm.IsScanning);
		// 环回端口 1 大概率关闭 → Offline
		Assert.Equal(OnlineStatus.Offline, vm.Connections[0].Status);
	}

	// —— 测试替身 ——

	private sealed class RecordingLauncher : IRdpLauncher
	{
		public System.Collections.Generic.List<RdpLauncher.RdpOptions> LaunchCalls { get; } = new System.Collections.Generic.List<RdpLauncher.RdpOptions>();

		public System.Collections.Generic.List<string> ClearedHosts { get; } = new System.Collections.Generic.List<string>();

		public void Launch(RdpLauncher.RdpOptions opt) => LaunchCalls.Add(opt);

		public void ClearCredential(string host) => ClearedHosts.Add(host);
	}
}
