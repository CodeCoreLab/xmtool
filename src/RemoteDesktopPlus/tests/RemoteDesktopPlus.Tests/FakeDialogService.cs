using System;
using System.Collections.Generic;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Tests;

/// <summary>
/// <see cref="IDialogService"/> 的测试替身。
/// <para>
/// 它的存在本身就是「VM→Views 已切断」的证明：本类不引用任何 WPF 窗口/控件类型，
/// 却能让 <see cref="RemoteDesktopPlus.ViewModels.MainViewModel"/> 完成全部交互流程。
/// 所有应答由测试预先排定，因此不再依赖人去点按钮。
/// </para>
/// </summary>
public sealed class FakeDialogService : IDialogService
{
	/// <summary>按顺序弹出的提示（message, title, kind）。</summary>
	public List<(string Message, string Title, DialogKind Kind)> Messages { get; } = new List<(string, string, DialogKind)>();

	/// <summary>每次 Confirm 的应答队列；用完后统一回落到 <see cref="DefaultConfirm"/>。</summary>
	public Queue<bool> ConfirmAnswers { get; } = new Queue<bool>();

	public bool DefaultConfirm { get; set; }

	/// <summary>EditConnection 的应答队列；每项 = (是否确定, 表单内容)。</summary>
	public Queue<(bool Ok, ConnectionFormResult? Result)> EditConnectionAnswers { get; } = new Queue<(bool, ConnectionFormResult?)>();

	public Queue<bool> MasterPasswordAnswers { get; } = new Queue<bool>();

	public Queue<bool> SetLaunchPasswordAnswers { get; } = new Queue<bool>();

	public Queue<string?> ExportTargets { get; } = new Queue<string?>();
	public Queue<string?> ImportSources { get; } = new Queue<string?>();
	public Queue<string?> RdpExportTargets { get; } = new Queue<string?>();

	/// <summary>记录每次 MasterPassword 弹窗的用途，用于断言「导入后要求解锁」。</summary>
	public List<MasterPasswordPurpose> MasterPasswordPurposes { get; } = new List<MasterPasswordPurpose>();

	public void ShowMessage(string message, string title, DialogKind kind = DialogKind.Information)
	{
		Messages.Add((message, title, kind));
	}

	public bool Confirm(string message, string title, DialogKind kind = DialogKind.Warning)
	{
		Messages.Add((message, title, kind));
		return ConfirmAnswers.Count > 0 ? ConfirmAnswers.Dequeue() : DefaultConfirm;
	}

	public bool EditConnection(ConnectionItem? source, out ConnectionFormResult result)
	{
		if (EditConnectionAnswers.Count == 0)
		{
			result = new ConnectionFormResult();
			return false;
		}
		(bool ok, ConnectionFormResult? payload) = EditConnectionAnswers.Dequeue();
		result = payload ?? new ConnectionFormResult();
		return ok;
	}

	public bool SetLaunchPassword()
	{
		return SetLaunchPasswordAnswers.Count > 0 && SetLaunchPasswordAnswers.Dequeue();
	}

	public bool ShowMasterPassword(MasterPasswordPurpose purpose)
	{
		MasterPasswordPurposes.Add(purpose);
		return MasterPasswordAnswers.Count > 0 && MasterPasswordAnswers.Dequeue();
	}

	public string? PickExportTarget(string suggestedFileName)
	{
		return ExportTargets.Count > 0 ? ExportTargets.Dequeue() : null;
	}

	public string? PickImportSource()
	{
		return ImportSources.Count > 0 ? ImportSources.Dequeue() : null;
	}

	public string? PickRdpExportTarget(string suggestedFileName)
	{
		return RdpExportTargets.Count > 0 ? RdpExportTargets.Dequeue() : null;
	}

	/// <summary>
	/// 记录每次分组设置面板被打开时收到的分组名，并按预先排队的值决定是否「保存」。
	/// 真实面板的控件行为不在单测范围内（那是 WPF 交互），
	/// 单测要覆盖的是「VM 是否把正确的库与参数交给了面板」。
	/// </summary>
	public List<string> GroupOptionsOpened { get; } = new List<string>();

	public Queue<bool> GroupOptionsAnswers { get; } = new Queue<bool>();

	public bool EditGroupOptions(string groupName, GroupOptionStore store, IReadOnlyList<string> resolutionOptions, IReadOnlyList<string> driveLetterOptions)
	{
		GroupOptionsOpened.Add(groupName);
		return GroupOptionsAnswers.Count > 0 && GroupOptionsAnswers.Dequeue();
	}

	/// <summary>便捷构造：一份已填好的新建/编辑表单内容。</summary>
	public static ConnectionFormResult Form(string host, int port = 3389, string user = "Administrator", string password = "pw", string remark = "")
	{
		return new ConnectionFormResult
		{
			Host = host,
			Port = port,
			UserName = user,
			Password = password,
			Remark = remark
		};
	}
}
