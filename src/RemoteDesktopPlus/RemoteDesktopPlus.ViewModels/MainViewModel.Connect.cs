using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.ViewModels;

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
		RdpLauncher.Launch(new RdpLauncher.RdpOptions(Host, Port, UserName, Password, width, height, RedirectLocalDrive, LocalDriveLetter, ForceConsole));
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
		RdpLauncher.ClearCredential(Host);
		_dialogs.ShowMessage("已清理 " + Host + " 的旧凭证，下次连接时会重新保存。", "凭证修复");
	}
}
