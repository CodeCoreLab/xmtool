using System;
using CommunityToolkit.Mvvm.Input;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.ViewModels;

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
