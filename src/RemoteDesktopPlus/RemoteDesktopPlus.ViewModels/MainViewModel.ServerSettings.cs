using System;
using CommunityToolkit.Mvvm.Input;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.ViewModels;

/// <summary>
/// 服务器设置职责：本机远程桌面开关、RDP 端口读写与状态刷新。
/// 这些操作都会改注册表 / 防火墙，属于需要管理员权限的路径，故统一在此收口。
/// 见 <see cref="MainViewModel"/> 的类注释中关于 partial 拆分的说明。
/// </summary>
public partial class MainViewModel
{
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
