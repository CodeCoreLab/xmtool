using System;
using System.Collections.Generic;
using System.Linq;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 记录被发起的连接，用于断言「连接成功一次即自动保存」的行为。
/// </summary>
internal sealed class RecordingLauncher : IRdpLauncher
{
	public List<RdpLauncher.RdpOptions> Launched { get; } = new List<RdpLauncher.RdpOptions>();

	/// <summary>模拟启动器抛异常（凭据写入失败 / 临时目录不可写等）。</summary>
	public Exception? ThrowOnLaunch { get; set; }

	public void Launch(RdpLauncher.RdpOptions opt)
	{
		if (ThrowOnLaunch != null)
		{
			throw ThrowOnLaunch;
		}
		Launched.Add(opt);
	}

	public void ClearCredential(string host)
	{
	}
}

/// <summary>
/// 「连一次就自动保存」的验收：既要把新主机落库，也不能把选中行或同主机他账号覆盖掉。
/// </summary>
public class AutoSaveOnConnectTests
{
	private static (MainViewModel Vm, DatabaseService Db, FakeDialogService Dialogs, RecordingLauncher Launcher) Create(TempDb tmp)
	{
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		FakeDialogService dialogs = new FakeDialogService();
		RecordingLauncher launcher = new RecordingLauncher();
		MainViewModel vm = new MainViewModel(db, crypto, dialogs, launcher);
		return (vm, db, dialogs, launcher);
	}

	private static ConnectionItem Seed(DatabaseService db, MainViewModel vm, string host, string user)
	{
		ConnectionItem item = new ConnectionItem
		{
			Host = host,
			Port = 3389,
			UserName = user,
			EncryptedPassword = string.Empty,
			SortOrder = vm.Connections.Count + 1
		};
		db.Insert(item);
		vm.ReloadConnections();
		return vm.Connections.First(c => c.Host == host && c.UserName == user);
	}

	[Fact]
	public void Connect_NewHost_IsPersistedAutomatically()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _, launcher) = Create(tmp);
		vm.Host = "10.20.30.40";
		vm.UserName = "ops";
		vm.Password = "pw";

		vm.ConnectCommand.Execute(null);

		Assert.Single(launcher.Launched);
		Assert.Single(vm.Connections);
		// 落库才是真保存：重开一个新的服务实例（等价重启）还能看到这条。
		Assert.Single(tmp.NewService().LoadAll());
		Assert.Equal("10.20.30.40", vm.Connections[0].Host);
	}

	[Fact]
	public void Connect_ExistingHost_SameUser_UpdatesInsteadOfDuplicating()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _, _) = Create(tmp);
		Seed(db, vm, "10.1.1.1", "Administrator");

		vm.Host = "10.1.1.1";
		vm.UserName = "Administrator";
		vm.Password = "rotated";
		vm.ConnectCommand.Execute(null);

		Assert.Single(vm.Connections);
		Assert.Single(tmp.NewService().LoadAll());
	}

	[Fact]
	public void Connect_DifferentUserOnSameHost_CreatesSecondRow()
	{
		// 同一台机器两个账号是真实场景；只按 Host/Port 匹配会让后者覆盖前者。
		using TempDb tmp = new TempDb();
		var (vm, db, _, _) = Create(tmp);
		Seed(db, vm, "10.1.1.1", "Administrator");

		vm.SelectedConnection = null;
		vm.Host = "10.1.1.1";
		vm.UserName = "root";
		vm.ConnectCommand.Execute(null);

		Assert.Equal(2, vm.Connections.Count);
		Assert.Equal(2, tmp.NewService().LoadAll().Count);
	}

	[Fact]
	public void Connect_WithAnotherRowSelected_DoesNotOverwriteThatRow()
	{
		// 选中 A 行时不改表单、直接连 B 号主机：自动保存必须新增 B，
		// 不能把 A 行改成 B 的地址（这会静默毁掉一条记录）。
		using TempDb tmp = new TempDb();
		var (vm, db, _, _) = Create(tmp);
		ConnectionItem a = Seed(db, vm, "10.2.2.2", "Administrator");
		vm.SelectedConnection = a;
		// 选中会把表单回填成 A，模拟用户随后手改成 B 号主机
		vm.Host = "10.3.3.3";

		vm.ConnectCommand.Execute(null);

		Assert.Equal(2, vm.Connections.Count);
		Assert.Equal("10.2.2.2", vm.Connections.First(c => c.Id == a.Id).Host);
		Assert.Contains(vm.Connections, c => c.Host == "10.3.3.3");
		Assert.Equal(2, tmp.NewService().LoadAll().Count);
	}

	[Fact]
	public void Connect_LauncherFailure_DoesNotPersistUnreachableRecord()
	{
		// 本地失败（临时目录不可写等）不该把一条连不上的记录静默存进列表。
		using TempDb tmp = new TempDb();
		var (vm, db, dialogs, launcher) = Create(tmp);
		launcher.ThrowOnLaunch = new InvalidOperationException("temp dir not writable");
		vm.Host = "10.9.9.9";
		vm.UserName = "ops";

		vm.ConnectCommand.Execute(null);

		Assert.Empty(vm.Connections);
		Assert.Empty(tmp.NewService().LoadAll());
		Assert.Contains(dialogs.Messages, m => m.Message.Contains("连接失败"));
	}

	[Fact]
	public void Connect_DefaultsToAllDrivesBothDirections()
	{
		// 需求：盘符不再让用户选，默认双向全盘符（含连接后新插的 U 盘）。
		using TempDb tmp = new TempDb();
		var (vm, _, _, launcher) = Create(tmp);
		vm.Host = "10.5.5.5";

		vm.ConnectCommand.Execute(null);

		RdpLauncher.RdpOptions opt = Assert.Single(launcher.Launched);
		// v1.2.0：共享通道内置恒开，RdpOptions 不再承载开关，由 BuildRdpContent 恒写。
		Assert.NotNull(opt);
	}
}
