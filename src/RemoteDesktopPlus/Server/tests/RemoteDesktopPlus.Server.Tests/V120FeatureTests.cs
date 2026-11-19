// v2.1.0 新功能测试（移植小马线 V120FeatureTests，适配本线测试基建）：
// 批量连接、托盘/置顶设置持久化、ESC 开关、多选拖拽护栏。
// 覆盖范围说明：AutoStartService 的 schtasks 路径需要真实 OS，不在单测范围
// （测试禁止触碰 vm.AutoStart 写路径，避免测试机残留计划任务）。
using System;
using System.Collections.Generic;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.Tests;
using RemoteDesktopPlus.ViewModels;
using Xunit;

namespace RemoteDesktopPlus.Tests
{
	public class V120FeatureTests : IDisposable
	{
		private readonly TempDb _tmp = new TempDb();

		public void Dispose()
		{
			_tmp.Dispose();
		}

		private (MainViewModel Vm, DatabaseService Db, FakeDialogService Dialogs, StubLauncher Launcher) Create()
		{
			DatabaseService db = _tmp.NewService();
			CryptoService crypto = new CryptoService(db);
			FakeDialogService dialogs = new FakeDialogService();
			StubLauncher launcher = new StubLauncher();
			MainViewModel vm = new MainViewModel(db, crypto, dialogs, launcher);
			return (vm, db, dialogs, launcher);
		}

		/// <summary>本文件自包含的启动器替身：记录调用、可注入异常。</summary>
		private sealed class StubLauncher : IRdpLauncher
		{
			public List<RdpLauncher.RdpOptions> Launched { get; } = new List<RdpLauncher.RdpOptions>();

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

		[Fact]
		public void BatchConnect_LaunchesEveryItem_AndUpdatesLastConnected()
		{
			var (vm, db, dialogs, launcher) = Create();
			ConnectionItem a = new ConnectionItem { Host = "10.0.0.1", Port = 3389, UserName = "admin", EncryptedPassword = new CryptoService(db).Encrypt("p1") };
			ConnectionItem b = new ConnectionItem { Host = "10.0.0.2", Port = 3390, UserName = "admin", EncryptedPassword = new CryptoService(db).Encrypt("p2") };
			db.Insert(a);
			db.Insert(b);
			vm.ReloadConnections();

			vm.BatchConnect(new List<ConnectionItem> { vm.Connections[0], vm.Connections[1] });

			Assert.Equal(2, launcher.Launched.Count);
			Assert.Equal("10.0.0.1", launcher.Launched[0].Host);
			Assert.Equal("10.0.0.2", launcher.Launched[1].Host);
			// 连接即回写 LastConnectedAt（>0，与单条连接语义一致）。
			Assert.True(vm.Connections[0].LastConnectedAt > 0);
			Assert.True(vm.Connections[1].LastConnectedAt > 0);
		}

		[Fact]
		public void BatchConnect_EmptyList_IsNoOp()
		{
			var (vm, _, _, launcher) = Create();
			vm.BatchConnect(new List<ConnectionItem>());
			Assert.Empty(launcher.Launched);
		}

		[Fact]
		public void BatchConnect_LaunchFailure_DoesNotEscape()
		{
			var (vm, db, dialogs, launcher) = Create();
			ConnectionItem a = new ConnectionItem { Host = "10.0.0.1", Port = 3389, UserName = "admin", EncryptedPassword = new CryptoService(db).Encrypt("p1") };
			db.Insert(a);
			vm.ReloadConnections();
			// Launch 抛异常（模拟临时目录不可写）：ConnectItem 兜底提示，不逃逸出 BatchConnect。
			launcher.ThrowOnLaunch = new InvalidOperationException("boom");

			vm.BatchConnect(new List<ConnectionItem> { vm.Connections[0] });

			Assert.Single(dialogs.Messages);
			Assert.Empty(launcher.Launched);
		}

		[Fact]
		public void TrayBehaviorSettings_PersistAndReload()
		{
			var (vm, db, _, _) = Create();
			vm.CloseToTray = false;
			vm.MinimizeToTray = false;
			vm.EscToTray = true;

			// 新 VM（模拟重启）读回相同值。
			MainViewModel vm2 = new MainViewModel(db, new CryptoService(db), new FakeDialogService());
			Assert.False(vm2.CloseToTray);
			Assert.False(vm2.MinimizeToTray);
			Assert.True(vm2.EscToTray);
		}

		[Fact]
		public void Topmost_PersistsAndReloads()
		{
			var (vm, db, _, _) = Create();
			vm.Topmost = true;

			MainViewModel vm2 = new MainViewModel(db, new CryptoService(db), new FakeDialogService());
			Assert.True(vm2.Topmost);
		}

		[Fact]
		public void RequestEscHide_OnlyFiresWhenEnabled()
		{
			var (vm, _, _, _) = Create();
			int fired = 0;
			vm.EscHideRequested += (_, _) => fired++;
			vm.RequestEscHide();       // EscToTray 默认 false → 不触发
			Assert.Equal(0, fired);
			vm.EscToTray = true;
			vm.RequestEscHide();       // 开启后触发
			Assert.Equal(1, fired);
		}

		[Fact]
		public void CanReorderManually_BlockedWhenMultipleSelected()
		{
			var (vm, _, _, _) = Create();
			Assert.True(vm.CanReorderManually);   // 单选 + 手动排序 → 可拖
			vm.SelectedCount = 3;
			Assert.False(vm.CanReorderManually);  // 多选 → 拖拽护栏关闭
			vm.SelectedCount = 1;
			Assert.True(vm.CanReorderManually);
		}
	}
}
