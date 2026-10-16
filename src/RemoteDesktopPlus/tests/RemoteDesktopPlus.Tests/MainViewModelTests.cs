using System;
using System.Collections.Generic;
using System.Linq;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;
using Xunit;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// 视图模型行为测试。
/// <para>
/// 这一组用例是「VM→Views 已切断」最直接的证据：本文件不引用任何 WPF 窗口类型，
/// 却能驱动 <see cref="MainViewModel"/> 走完新建 / 编辑 / 删除 / 拖拽排序 / 导出导入全流程。
/// 重构前做不到——VM 内部直接 <c>new EditConnectionWindow()</c>，没有窗口就没有交互。
/// </para>
/// <para>
/// 每例都用临时库 + <see cref="FakeDialogService"/>，绝不触碰真实 data.db。
/// </para>
/// </summary>
public class MainViewModelTests
{
	private static (MainViewModel Vm, DatabaseService Db, CryptoService Crypto, FakeDialogService Dialogs) Create(TempDb tmp)
	{
		DatabaseService db = tmp.NewService();
		CryptoService crypto = new CryptoService(db);
		FakeDialogService dialogs = new FakeDialogService();
		MainViewModel vm = new MainViewModel(db, crypto, dialogs);
		return (vm, db, crypto, dialogs);
	}

	// —— 构造与选项加载 ——

	[Fact]
	public void Constructor_StartsWithEmptyList_AndDefaultOptions()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, _) = Create(tmp);

		Assert.Empty(vm.Connections);
		Assert.Equal(3389, vm.Port);
		Assert.Equal("1920*1080", vm.Resolution);
		Assert.Equal(10, vm.ResolutionOptions.Count);
		Assert.True(vm.OptionsExpanded);
	}

	[Fact]
	public void Constructor_LoadsPersistedOptions_WithoutWritingThemBack()
	{
		using TempDb tmp = new TempDb();
		DatabaseService seed = tmp.NewService();
		seed.SetSetting(SettingKeys.OptionResolution, "2560*1440");
		seed.SetSetting(SettingKeys.OptionRedirectDrive, "True");
		seed.SetSetting(SettingKeys.OptionForceConsole, "True");
		seed.SetSetting(SettingKeys.OptionLocalDriveLetter, "D");

		FakeDialogService dialogs = new FakeDialogService();
		CryptoService crypto = new CryptoService(seed);
		MainViewModel vm = new MainViewModel(seed, crypto, dialogs);

		Assert.Equal("2560*1440", vm.Resolution);
		Assert.True(vm.RedirectLocalDrive);
		Assert.True(vm.ForceConsole);
		Assert.Equal("D", vm.LocalDriveLetter);

		// 加载期间必须抑制回写：否则会把「非法但已存的值」覆盖成默认值。
		DatabaseService verify = tmp.NewService();
		Assert.Equal("2560*1440", verify.GetSetting(SettingKeys.OptionResolution));
	}

	[Fact]
	public void Constructor_IgnoresPersistedResolution_OutsideTheAllowedList()
	{
		using TempDb tmp = new TempDb();
		DatabaseService seed = tmp.NewService();
		seed.SetSetting(SettingKeys.OptionResolution, "4096*2160");

		MainViewModel vm = new MainViewModel(seed, new CryptoService(seed), new FakeDialogService());

		Assert.Equal("1920*1080", vm.Resolution);
	}

	[Fact]
	public void ChangingResolution_PersistsToSettings()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, _) = Create(tmp);

		vm.Resolution = "1280*720";

		Assert.Equal("1280*720", tmp.NewService().GetSetting(SettingKeys.OptionResolution));
	}

	// —— 连接列表 CRUD ——

	[Fact]
	public void NewItem_AcceptedForm_AddsEncryptedRow_AndSelectsIt()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, crypto, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("10.0.0.9", 3390, "admin", "pw123", "备注")));

		vm.NewItemCommand.Execute(null);

		ConnectionItem item = Assert.Single(vm.Connections);
		Assert.Equal("10.0.0.9", item.Host);
		Assert.Equal(3390, item.Port);
		Assert.Equal("备注", item.Remark);
		Assert.Equal(1, item.Index);
		Assert.Equal(1, item.SortOrder);
		Assert.Same(item, vm.SelectedConnection);

		// 密码必须以密文入库，且能原样解回
		Assert.NotEqual("pw123", item.EncryptedPassword);
		Assert.Equal("pw123", crypto.Decrypt(item.EncryptedPassword));
		// 落库的密文应与内存对象一致，且同样只以密文形态存在
		ConnectionItem persisted = db.LoadAll().Single();
		Assert.Equal(item.EncryptedPassword, persisted.EncryptedPassword);
		Assert.NotEqual("pw123", persisted.EncryptedPassword);
	}

	[Fact]
	public void NewItem_Cancelled_ChangesNothing()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((false, null));

		vm.NewItemCommand.Execute(null);

		Assert.Empty(vm.Connections);
	}

	[Fact]
	public void EditSelected_UpdatesRowInPlace_AndRefreshesForm()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, crypto, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a.local", 3389, "u1", "p1")));
		vm.NewItemCommand.Execute(null);
		ConnectionItem item = vm.Connections[0];

		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("b.local", 3391, "u2", "p2")));
		vm.EditSelectedCommand.Execute(null);

		Assert.Single(vm.Connections);
		Assert.Equal("b.local", item.Host);
		Assert.Equal(3391, item.Port);
		Assert.Equal("u2", item.UserName);
		Assert.Equal("p2", crypto.Decrypt(item.EncryptedPassword));

		// 表单应同步为编辑后的值
		Assert.Equal("b.local", vm.Host);
		Assert.Equal(3391, vm.Port);
		Assert.Equal("u2", vm.UserName);
		Assert.Equal("p2", vm.Password);
	}

	[Fact]
	public void EditSelected_Cancelled_KeepsRowUntouched()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a.local")));
		vm.NewItemCommand.Execute(null);

		dialogs.EditConnectionAnswers.Enqueue((false, null));
		vm.EditSelectedCommand.Execute(null);

		Assert.Equal("a.local", vm.Connections[0].Host);
	}

	[Fact]
	public void EditSelected_WithoutSelection_IsDisabledAndNoOp()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);

		Assert.False(vm.EditSelectedCommand.CanExecute(null));
		vm.EditSelectedCommand.Execute(null);

		Assert.Empty(dialogs.EditConnectionAnswers);
		Assert.Empty(vm.Connections);
	}

	[Fact]
	public void DeleteSelected_Confirmed_RemovesRow_AndReindexes()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _, dialogs) = Create(tmp);
		foreach (string h in new[] { "a", "b", "c" })
		{
			dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form(h)));
			vm.NewItemCommand.Execute(null);
		}

		vm.SelectedConnection = vm.Connections[1];
		dialogs.ConfirmAnswers.Enqueue(true);
		vm.DeleteSelectedCommand.Execute(null);

		Assert.Equal(new[] { "a", "c" }, vm.Connections.Select(x => x.Host).ToArray());
		Assert.Equal(new[] { 1, 2 }, vm.Connections.Select(x => x.Index).ToArray());
		Assert.Equal(new[] { "a", "c" }, db.LoadAll().Select(x => x.Host).ToArray());
	}

	[Fact]
	public void DeleteSelected_Declined_KeepsRow()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a")));
		vm.NewItemCommand.Execute(null);

		vm.SelectedConnection = vm.Connections[0];
		dialogs.ConfirmAnswers.Enqueue(false);
		vm.DeleteSelectedCommand.Execute(null);

		Assert.Single(vm.Connections);
	}

	// —— 表单保存语义 ——

	[Fact]
	public void SaveItem_UpdatesSelectedRow_InsteadOfAddingADuplicate()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a", 3389, "u")));
		vm.NewItemCommand.Execute(null);

		vm.Host = "a";
		vm.Port = 3389;
		vm.UserName = "u";
		vm.Password = "rotated";
		vm.SaveItemCommand.Execute(null);

		Assert.Single(vm.Connections);
		Assert.Single(tmp.NewService().LoadAll());
	}

	[Fact]
	public void SaveItem_MatchesExistingRow_ByHostPortUser_WhenNothingSelected()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("dup", 3389, "u")));
		vm.NewItemCommand.Execute(null);
		vm.SelectedConnection = null;

		vm.Host = "dup";
		vm.Port = 3389;
		vm.UserName = "u";
		vm.SaveItemCommand.Execute(null);

		Assert.Single(vm.Connections);
	}

	[Fact]
	public void SaveItem_CreatesNewRow_WhenNoMatchAndNothingSelected()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("first")));
		vm.NewItemCommand.Execute(null);
		vm.SelectedConnection = null;

		vm.Host = "second";
		vm.Port = 3389;
		vm.UserName = "someone-else";
		vm.SaveItemCommand.Execute(null);

		Assert.Equal(2, vm.Connections.Count);
	}

	[Fact]
	public void SaveItem_EmptyHost_ShowsHint_AndDoesNotPersist()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		vm.Host = "   ";

		vm.SaveItemCommand.Execute(null);

		Assert.Empty(vm.Connections);
		Assert.Single(dialogs.Messages);
		Assert.Equal("请填写主机 IP 地址。", dialogs.Messages[0].Message);
	}

	// —— 选择连接回填表单 ——

	[Fact]
	public void SelectingConnection_BackfillsForm_WithDecryptedPassword()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a", 3390, "u", "secret")));
		vm.NewItemCommand.Execute(null);

		vm.Password = string.Empty;
		vm.SelectedConnection = null;
		vm.SelectedConnection = vm.Connections[0];

		Assert.Equal("a", vm.Host);
		Assert.Equal(3390, vm.Port);
		Assert.Equal("u", vm.UserName);
		Assert.Equal("secret", vm.Password);
	}

	// —— 拖拽排序 ——

	[Fact]
	public void Drop_MovingDownward_CompensatesInsertIndex()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _, dialogs) = Create(tmp);
		foreach (string h in new[] { "a", "b", "c" })
		{
			dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form(h)));
			vm.NewItemCommand.Execute(null);
		}

		// 把第 0 项 "a" 拖到索引 2（目标之前）→ 补偿后落到位置 1
		vm.Drop(new StubDropInfo(vm.Connections[0], vm.Connections[2], 2));

		Assert.Equal(new[] { "b", "a", "c" }, vm.Connections.Select(x => x.Host).ToArray());
		// 顺序必须落库，否则重启后白拖
		Assert.Equal(new[] { "b", "a", "c" }, db.LoadAll().Select(x => x.Host).ToArray());
		Assert.Equal(new[] { 1, 2, 3 }, vm.Connections.Select(x => x.SortOrder).ToArray());
	}

	[Fact]
	public void Drop_MovingUpward_DoesNotCompensate()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _, dialogs) = Create(tmp);
		foreach (string h in new[] { "a", "b", "c" })
		{
			dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form(h)));
			vm.NewItemCommand.Execute(null);
		}

		// 把第 2 项 "c" 拖到索引 0 → 不补偿
		vm.Drop(new StubDropInfo(vm.Connections[2], vm.Connections[0], 0));

		Assert.Equal(new[] { "c", "a", "b" }, vm.Connections.Select(x => x.Host).ToArray());
		Assert.Equal(new[] { "c", "a", "b" }, db.LoadAll().Select(x => x.Host).ToArray());
	}

	[Fact]
	public void Drop_OntoItself_IsNoOp_AndDoesNotRewriteSortOrders()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a")));
		vm.NewItemCommand.Execute(null);
		vm.Connections[0].SortOrder = 77;
		db.UpdateSortOrders(vm.Connections);

		vm.Drop(new StubDropInfo(vm.Connections[0], vm.Connections[0], 0));

		Assert.Single(vm.Connections);
		Assert.Equal(77, db.LoadAll()[0].SortOrder);
	}

	[Fact]
	public void Drop_NonConnectionPayload_IsIgnored()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a")));
		vm.NewItemCommand.Execute(null);

		vm.Drop(new StubDropInfo("not a connection", vm.Connections[0], 0));

		Assert.Single(vm.Connections);
		Assert.Equal("a", vm.Connections[0].Host);
	}

	// —— 服务器设置命令：失败路径必须被翻译成提示而不是崩溃 ——

	[Fact]
	public void ChangePort_NonNumeric_ShowsRangeHint_WithoutTouchingRegistry()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		vm.NewPort = "abc";

		vm.ChangePortCommand.Execute(null);

		Assert.Single(dialogs.Messages);
		Assert.Contains("1-65535", dialogs.Messages[0].Message);
	}

	[Theory]
	[InlineData("0")]
	[InlineData("65536")]
	[InlineData("-1")]
	public void ChangePort_OutOfRange_ShowsHint(string newPort)
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		vm.NewPort = newPort;

		vm.ChangePortCommand.Execute(null);

		Assert.Single(dialogs.Messages);
		Assert.Contains("1-65535", dialogs.Messages[0].Message);
	}

	[Fact]
	public void RepairCredential_EmptyHost_ShowsHint()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		vm.Host = "";

		vm.RepairCredentialCommand.Execute(null);

		Assert.Single(dialogs.Messages);
		Assert.Equal("请先选择或输入主机。", dialogs.Messages[0].Message);
	}

	// —— 加密设置：导入后必须要求解锁 ——

	[Fact]
	public void ImportData_UserCancelsFilePick_DoesNothing()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);

		vm.ImportDataCommand.Execute(null);

		Assert.Empty(dialogs.Messages);
	}

	[Fact]
	public void ImportData_DeclinedAtConfirmation_DoesNothing()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.ImportSources.Enqueue("whatever.db");
		dialogs.ConfirmAnswers.Enqueue(false);

		vm.ImportDataCommand.Execute(null);

		Assert.Empty(dialogs.ExportTargets);
	}

	[Fact]
	public void ExportData_NoTargetPicked_DoesNotReportSuccess()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.ExportTargets.Enqueue(null);

		vm.ExportDataCommand.Execute(null);

		Assert.Empty(dialogs.Messages);
	}

	[Fact]
	public void SetLaunchPasswordCommand_DelegatesToDialog()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.SetLaunchPasswordAnswers.Enqueue(true);

		vm.SetLaunchPasswordCommand.Execute(null);

		Assert.Empty(dialogs.SetLaunchPasswordAnswers);
	}

	[Fact]
	public void SetMasterPassword_RequestedPurposeIsSet()
	{
		using TempDb tmp = new TempDb();
		var (vm, _, _, dialogs) = Create(tmp);
		dialogs.MasterPasswordAnswers.Enqueue(false);

		vm.SetMasterPasswordCommand.Execute(null);

		Assert.Equal(new[] { MasterPasswordPurpose.Set }, dialogs.MasterPasswordPurposes);
	}

	[Fact]
	public void SetMasterPassword_WhenDialogCancels_DoesNotReloadConnections()
	{
		using TempDb tmp = new TempDb();
		var (vm, db, _, dialogs) = Create(tmp);
		dialogs.EditConnectionAnswers.Enqueue((true, FakeDialogService.Form("a")));
		vm.NewItemCommand.Execute(null);
		dialogs.MasterPasswordAnswers.Enqueue(false);

		vm.SetMasterPasswordCommand.Execute(null);

		Assert.Single(vm.Connections);
	}
}
