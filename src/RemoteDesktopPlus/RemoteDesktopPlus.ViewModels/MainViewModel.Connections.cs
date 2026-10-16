using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.ViewModels;

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
		RdpLauncher.Launch(new RdpLauncher.RdpOptions(item.Host, item.Port, item.UserName, _crypto.Decrypt(item.EncryptedPassword), width, height, RedirectLocalDrive, LocalDriveLetter, ForceConsole));
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

	[RelayCommand]
	private async Task ScanOnlineAsync()
	{
		await Task.WhenAll(((IEnumerable<ConnectionItem>)Connections).Select((Func<ConnectionItem, Task>)async delegate(ConnectionItem item)
		{
			item.Status = OnlineStatus.Checking;
			item.Status = ((await OnlineProbe.IsOnlineAsync(item.Host, item.Port)) ? OnlineStatus.Online : OnlineStatus.Offline);
		}));
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
