using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// v1.0.3 三项增强的行为测试：
/// ① 最近连接排序（含与拖拽排序互斥）② 分组配置继承 ③ 导出单条连接为 .rdp。
/// <para>
/// 与 <see cref="MainViewModelTests"/> 同样使用临时库 + FakeDialogService，不触碰真实 data.db。
/// </para>
/// </summary>
public class ConnectionEnhancementsTests
{
	private static (MainViewModel Vm, DatabaseService Db, FakeDialogService Dialogs) Create(TempDb tmp)
	{
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		FakeDialogService dialogs = new FakeDialogService();
		MainViewModel vm = new MainViewModel(db, crypto, dialogs);
		return (vm, db, dialogs);
	}

	/// <summary>插入一条连接并返回它（走真实落库路径，不是内存假数据）。</summary>
	private static ConnectionItem Seed(DatabaseService db, MainViewModel vm, string host, long lastConnectedAt = 0, string group = "")
	{
		ConnectionItem item = new ConnectionItem
		{
			Host = host,
			Port = 3389,
			UserName = "Administrator",
			GroupName = group,
			EncryptedPassword = string.Empty,
			LastConnectedAt = lastConnectedAt,
			SortOrder = vm.Connections.Count + 1
		};
		db.Insert(item);
		vm.ReloadConnections();
		return vm.Connections.First(c => c.Host == host);
	}

	// ───────────────────────── ① 最近连接排序 ─────────────────────────

	[Fact]
	public void SortByRecent_OrdersByLastConnectedAtDescending()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		Seed(db, vm, "old", lastConnectedAt: 1000);
		Seed(db, vm, "newest", lastConnectedAt: 3000);
		Seed(db, vm, "middle", lastConnectedAt: 2000);

		vm.SortByRecent = true;

		Assert.Equal(new[] { "newest", "middle", "old" }, vm.Connections.Select(c => c.Host).ToArray());
	}

	[Fact]
	public void SortByRecent_False_KeepsManualSortOrder()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		Seed(db, vm, "a", lastConnectedAt: 1000);
		Seed(db, vm, "b", lastConnectedAt: 9000);

		vm.SortByRecent = false;

		// 手工顺序（插入序）优先，不受最近连接影响
		Assert.Equal(new[] { "a", "b" }, vm.Connections.Select(c => c.Host).ToArray());
	}

	[Fact]
	public void SortByRecent_PersistsAcrossReload()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		Seed(db, vm, "x", lastConnectedAt: 100);
		Seed(db, vm, "y", lastConnectedAt: 200);

		vm.SortByRecent = true;
		// 新建一个 VM 模拟重启，读同一份库
		MainViewModel restarted = new MainViewModel(db, new CryptoService(db), new FakeDialogService());

		Assert.True(restarted.SortByRecent);
		Assert.Equal("y", restarted.Connections.First().Host);
	}

	[Fact]
	public void CanReorderManually_IsInverseOfSortByRecent()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _) = Create(tmp);

		vm.SortByRecent = false;
		Assert.True(vm.CanReorderManually);

		vm.SortByRecent = true;
		Assert.False(vm.CanReorderManually);
	}

	[Fact]
	public void Drop_IsIgnoredWhenSortingByRecent()
	{
		// 自动排序下拖拽必须无效：否则会写入 SortOrder 却看不到效果，属于误导。
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		Seed(db, vm, "first", lastConnectedAt: 100);
		Seed(db, vm, "second", lastConnectedAt: 200);
		vm.SortByRecent = true;
		string[] before = vm.Connections.Select(c => c.Host).ToArray();

		StubDropInfo drop = new StubDropInfo(vm.Connections[0], vm.Connections[1], 1);
		vm.Drop(drop);

		Assert.Equal(before, vm.Connections.Select(c => c.Host).ToArray());
	}

	[Fact]
	public void LastConnectedText_FormatsRelativeTime()
	{
		ConnectionItem never = new ConnectionItem { LastConnectedAt = 0 };
		Assert.Equal("从未", never.LastConnectedText);

		ConnectionItem justNow = new ConnectionItem
		{
			LastConnectedAt = DateTimeOffset.Now.AddSeconds(-5).ToUnixTimeSeconds()
		};
		Assert.Equal("刚刚", justNow.LastConnectedText);

		ConnectionItem twoHoursAgo = new ConnectionItem
		{
			LastConnectedAt = DateTimeOffset.Now.AddHours(-2).ToUnixTimeSeconds()
		};
		Assert.Equal("2 小时前", twoHoursAgo.LastConnectedText);
	}

	// ───────────────────────── ② 分组配置继承 ─────────────────────────

	[Fact]
	public void GroupOptions_MissingFields_InheritGlobal()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		vm.Resolution = "1920*1080";
		vm.RedirectClipboard = true;
		vm.RedirectLocalDrive = true;
		vm.LocalDriveLetter = MainViewModel.AllDrivesDisplayName;

		ConnectionItem item = Seed(db, vm, "10.0.0.1", group: "客户A");
		// 组内没有任何覆盖 → 全部继承全局
		RdpLauncher.RdpOptions opt = vm.BuildEffectiveOptions(item);

		Assert.Equal(1920, opt.Width);
		Assert.Equal(1080, opt.Height);
		Assert.True(opt.RedirectClipboard);
		Assert.True(opt.RedirectLocalDrive);
	}

	[Fact]
	public void GroupOptions_OverrideWinsOverGlobal()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		vm.Resolution = "1920*1080";
		vm.RedirectClipboard = true;
		vm.RedirectLocalDrive = true;
		vm.LocalDriveLetter = MainViewModel.AllDrivesDisplayName;

		ConnectionItem item = Seed(db, vm, "10.0.0.2", group: "只读环境");

		// 仅覆盖剪贴板，其余继承
		GroupOptionStore store = new GroupOptionStore(db);
		store.Set("只读环境", new GroupOptions { RedirectClipboard = false });

		RdpLauncher.RdpOptions opt = vm.BuildEffectiveOptions(item);

		Assert.False(opt.RedirectClipboard);   // 组覆盖生效
		Assert.Equal(1920, opt.Width);         // 分辨率仍继承全局
	}

	[Fact]
	public void GroupOptions_AreCaseInsensitive()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		var store = new GroupOptionStore(db);
		store.Set("TEST", new GroupOptions { RedirectClipboard = false });

		// "test" 与 "TEST" 必须命中同一条配置，否则会出现「改了 A 组但 B 组没变」
		Assert.NotNull(store.Get("test"));
		Assert.False(store.Get("test")!.RedirectClipboard);
	}

	[Fact]
	public void GroupOptions_AllInherited_RemovesEntry()
	{
		using TempDb tmp = new TempDb();
		var (_, db, _) = Create(tmp);
		var store = new GroupOptionStore(db);
		store.Set("g", new GroupOptions { RedirectClipboard = false });
		Assert.NotEmpty(store.GroupNames());

		// 清空所有字段应移除条目，避免留下空壳让人误以为配过
		store.Set("g", new GroupOptions());
		Assert.Empty(store.GroupNames());
	}

	[Fact]
	public void GroupOptions_CorruptedJson_DegradesGracefully()
	{
		// 配置损坏不能让程序起不来：应丢弃坏配置并当空处理。
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		db.SetSetting(SettingKeys.OptionGroupOverrides, "{ 这不是合法 JSON ");

		var store = new GroupOptionStore(db);

		Assert.Empty(store.GroupNames());
	}

	[Fact]
	public void GroupFilterOptions_IncludesAllGroupsPlusExistingNames()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _) = Create(tmp);
		Seed(db, vm, "h1", group: "组一");
		Seed(db, vm, "h2", group: "组二");
		Seed(db, vm, "h3", group: "");

		Assert.Equal(MainViewModel.AllGroupsDisplayName, vm.GroupFilterOptions[0]);
		Assert.Contains("组一", vm.GroupFilterOptions);
		Assert.Contains("组二", vm.GroupFilterOptions);
		// 空分组不该出现在筛选里（它由「全部分组」覆盖）
		Assert.DoesNotContain("", vm.GroupFilterOptions);
	}

	// ───────────────────────── ③ 导出 .rdp ─────────────────────────

	[Fact]
	public void ExportRdp_WritesUtf16FileWithExpectedKeys()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, dialogs) = Create(tmp);
		ConnectionItem item = Seed(db, vm, "10.9.9.9");
		vm.SelectedConnection = item;

		string target = Path.Combine(Path.GetTempPath(), $"rdp-export-{Guid.NewGuid():N}.rdp");
		dialogs.RdpExportTargets.Enqueue(target);
		try
		{
			vm.ExportSelectedRdpCommand.Execute(null);

			Assert.True(File.Exists(target));
			byte[] head = new byte[2];
			using (FileStream fs = File.OpenRead(target))
			{
				fs.Read(head, 0, 2);
			}
			// UTF-16LE BOM，与 mstsc 自身写法一致
			Assert.Equal(new byte[] { 0xFF, 0xFE }, head);

			string content = File.ReadAllText(target, System.Text.Encoding.Unicode);
			Assert.Contains("full address:s:10.9.9.9:3389", content);
		}
		finally
		{
			if (File.Exists(target))
			{
				File.Delete(target);
			}
		}
	}

	[Fact]
	public void ExportRdp_NeverContainsPassword()
	{
		// 安全不变量：导出的文件可随意分享，绝不能带密码。
		using TempDb tmp = new TempDb();
		var (vm, db, dialogs) = Create(tmp);
		ConnectionItem item = Seed(db, vm, "10.8.8.8");
		item.EncryptedPassword = new CryptoService(db).Encrypt("PLAINTEXT-SECRET-MARKER");
		vm.SelectedConnection = item;

		string target = Path.Combine(Path.GetTempPath(), $"rdp-export-{Guid.NewGuid():N}.rdp");
		dialogs.RdpExportTargets.Enqueue(target);
		try
		{
			vm.ExportSelectedRdpCommand.Execute(null);

			string content = File.ReadAllText(target, System.Text.Encoding.Unicode);
			Assert.DoesNotContain("PLAINTEXT-SECRET-MARKER", content);
			Assert.DoesNotContain("password 51", content);
		}
		finally
		{
			if (File.Exists(target))
			{
				File.Delete(target);
			}
		}
	}

	[Fact]
	public void ExportRdp_UsesGroupOverrides()
	{
		// 导出应反映「这台机器实际会怎么连」，故必须应用分组覆盖。
		using TempDb tmp = new TempDb();
		var (vm, db, dialogs) = Create(tmp);
		ConnectionItem item = Seed(db, vm, "10.7.7.7", group: "无剪贴板组");
		new GroupOptionStore(db).Set("无剪贴板组", new GroupOptions { RedirectClipboard = false });
		vm.SelectedConnection = item;

		string target = Path.Combine(Path.GetTempPath(), $"rdp-export-{Guid.NewGuid():N}.rdp");
		dialogs.RdpExportTargets.Enqueue(target);
		try
		{
			vm.ExportSelectedRdpCommand.Execute(null);

			string content = File.ReadAllText(target, System.Text.Encoding.Unicode);
			Assert.Contains("redirectclipboard:i:0", content);
		}
		finally
		{
			if (File.Exists(target))
			{
				File.Delete(target);
			}
		}
	}

	[Fact]
	public void ExportRdp_CancelledDialog_WritesNothing()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, dialogs) = Create(tmp);
		vm.SelectedConnection = Seed(db, vm, "10.6.6.6");
		// 队列为空 = 用户取消
		vm.ExportSelectedRdpCommand.Execute(null);

		Assert.DoesNotContain(dialogs.Messages, m => m.Message.Contains("已导出"));
	}
}
