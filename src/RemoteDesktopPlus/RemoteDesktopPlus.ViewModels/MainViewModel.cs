using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.Views;

namespace RemoteDesktopPlus.ViewModels;

/// <summary>
/// 主窗口的视图模型。
/// <para>
/// 本类由 CommunityToolkit.Mvvm 源生成器补全：属性来自 <c>[ObservableProperty]</c> 字段，
/// 命令来自 <c>[RelayCommand]</c> 方法，<c>OnXxxChanged</c> 是生成器约定的部分方法钩子。
/// 反编译版本里那些带 <c>[GeneratedCode]</c> 的手写属性/命令字段已全部删除——
/// 它们本是生成器产物，保留会与生成器产生 CS0102 重复定义。
/// </para>
/// </summary>
public partial class MainViewModel : ObservableObject, IDropTarget
{
	[ObservableProperty]
	private string host = string.Empty;

	[ObservableProperty]
	private int port = 3389;

	[ObservableProperty]
	private string userName = string.Empty;

	[ObservableProperty]
	private string password = string.Empty;

	[ObservableProperty]
	private string resolution = "1920*1080";

	[ObservableProperty]
	private bool redirectLocalDrive;

	[ObservableProperty]
	private string localDriveLetter = PickDefaultDriveLetter();

	[ObservableProperty]
	private bool forceConsole;

	[ObservableProperty]
	private bool optionsExpanded = true;

	[ObservableProperty]
	private bool isRemoteEnabled;

	[ObservableProperty]
	private int currentRdpPort = 3389;

	[ObservableProperty]
	private bool isPortListening;

	[ObservableProperty]
	private string newPort = string.Empty;

	[ObservableProperty]
	private bool hasMasterPassword;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor("ConnectCommand")]
	[NotifyCanExecuteChangedFor("DeleteSelectedCommand")]
	[NotifyCanExecuteChangedFor("EditSelectedCommand")]
	private ConnectionItem? selectedConnection;

	private bool _suppressOptionSave;

	public ObservableCollection<ConnectionItem> Connections { get; } = new ObservableCollection<ConnectionItem>();

	public IReadOnlyList<string> ResolutionOptions { get; } = new string[10] { "全屏", "1024*768", "1280*720", "1366*768", "1440*900", "1600*900", "1680*1050", "1920*1080", "2560*1440", "3840*2160" };

	public IReadOnlyList<string> DriveLetters { get; } = BuildDriveLetters();

	private static IReadOnlyList<string> BuildDriveLetters()
	{
		try
		{
			List<string> list = (from s in (from d in DriveInfo.GetDrives()
					where d.IsReady
					select (d.Name.Length <= 0) ? string.Empty : d.Name[0].ToString().ToUpperInvariant()).Where(delegate(string s)
				{
					if (s.Length == 1)
					{
						char c2 = s[0];
						if (c2 >= 'C')
						{
							return c2 <= 'Z';
						}
						return false;
					}
					return false;
				}).Distinct()
				orderby s
				select s).ToList();
			if (list.Count > 0)
			{
				return list;
			}
		}
		catch
		{
		}
		return (from c in Enumerable.Range(67, 24)
			select ((char)c).ToString()).ToList();
	}

	private static string PickDefaultDriveLetter()
	{
		return BuildDriveLetters().FirstOrDefault((string l) => !string.Equals(l, "C", StringComparison.OrdinalIgnoreCase)) ?? "C";
	}

	public MainViewModel()
	{
		HasMasterPassword = CryptoService.Instance.IsMasterPasswordConfigured();
		LoadOptions();
		ReloadConnections();
		RefreshServerStatus();
	}

	private void LoadOptions()
	{
		try
		{
			_suppressOptionSave = true;
			DatabaseService instance = DatabaseService.Instance;
			string setting = instance.GetSetting(SettingKeys.OptionResolution);
			if (!string.IsNullOrEmpty(setting) && ResolutionOptions.Contains(setting))
			{
				Resolution = setting;
			}
			if (bool.TryParse(instance.GetSetting(SettingKeys.OptionRedirectDrive), out var result))
			{
				RedirectLocalDrive = result;
			}
			string setting2 = instance.GetSetting(SettingKeys.OptionLocalDriveLetter);
			if (!string.IsNullOrEmpty(setting2) && DriveLetters.Contains(setting2))
			{
				LocalDriveLetter = setting2;
			}
			if (bool.TryParse(instance.GetSetting(SettingKeys.OptionForceConsole), out var result2))
			{
				ForceConsole = result2;
			}
		}
		finally
		{
			_suppressOptionSave = false;
		}
	}

	public void ReloadConnections()
	{
		Connections.Clear();
		foreach (ConnectionItem item in DatabaseService.Instance.LoadAll())
		{
			Connections.Add(item);
		}
	}

	public void RefreshServerStatus()
	{
		IsRemoteEnabled = RdpServerService.IsRemoteEnabled();
		CurrentRdpPort = RdpServerService.GetCurrentPort();
		IsPortListening = RdpServerService.IsPortListening(CurrentRdpPort);
	}

	[RelayCommand(CanExecute = "CanConnect")]
	private void Connect()
	{
		if (string.IsNullOrWhiteSpace(Host))
		{
			MessageBox.Show("请填写主机 IP 地址。", "提示", MessageBoxButton.OK, MessageBoxImage.Asterisk);
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

	public void ConnectItem(ConnectionItem item)
	{
		SelectedConnection = item;
		var (width, height) = ParseResolution(Resolution);
		RdpLauncher.Launch(new RdpLauncher.RdpOptions(item.Host, item.Port, item.UserName, CryptoService.Instance.Decrypt(item.EncryptedPassword), width, height, RedirectLocalDrive, LocalDriveLetter, ForceConsole));
	}

	[RelayCommand]
	private void NewItem()
	{
		EditConnectionWindow editConnectionWindow = new EditConnectionWindow
		{
			Owner = Application.Current.MainWindow
		};
		if (editConnectionWindow.ShowDialog().GetValueOrDefault())
		{
			ConnectionItem connectionItem = new ConnectionItem
			{
				Host = editConnectionWindow.Result.Host,
				Port = editConnectionWindow.Result.Port,
				UserName = editConnectionWindow.Result.UserName,
				Remark = editConnectionWindow.Result.Remark,
				EncryptedPassword = CryptoService.Instance.Encrypt(editConnectionWindow.Result.Password),
				SortOrder = Connections.Count + 1
			};
			DatabaseService.Instance.Insert(connectionItem);
			connectionItem.Index = Connections.Count + 1;
			Connections.Add(connectionItem);
			SelectedConnection = connectionItem;
		}
	}

	[RelayCommand(CanExecute = "HasSelected")]
	private void EditSelected()
	{
		if (SelectedConnection != null)
		{
			EditConnectionWindow editConnectionWindow = new EditConnectionWindow(SelectedConnection)
			{
				Owner = Application.Current.MainWindow
			};
			if (editConnectionWindow.ShowDialog().GetValueOrDefault())
			{
				SelectedConnection.Host = editConnectionWindow.Result.Host;
				SelectedConnection.Port = editConnectionWindow.Result.Port;
				SelectedConnection.UserName = editConnectionWindow.Result.UserName;
				SelectedConnection.Remark = editConnectionWindow.Result.Remark;
				SelectedConnection.EncryptedPassword = CryptoService.Instance.Encrypt(editConnectionWindow.Result.Password);
				DatabaseService.Instance.Update(SelectedConnection);
				Host = SelectedConnection.Host;
				Port = SelectedConnection.Port;
				UserName = SelectedConnection.UserName;
				Password = editConnectionWindow.Result.Password;
			}
		}
	}

	[RelayCommand]
	private void SaveItem()
	{
		if (string.IsNullOrWhiteSpace(Host))
		{
			MessageBox.Show("请填写主机 IP 地址。", "提示");
		}
		else
		{
			SaveOrUpdateForm();
		}
	}

	private void SaveOrUpdateForm()
	{
		string encryptedPassword = CryptoService.Instance.Encrypt(Password);
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
			DatabaseService.Instance.Insert(connectionItem2);
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
			DatabaseService.Instance.Update(connectionItem);
			SelectedConnection = connectionItem;
		}
	}

	[RelayCommand(CanExecute = "HasSelected")]
	private void DeleteSelected()
	{
		if (SelectedConnection != null && MessageBox.Show($"确定删除 {SelectedConnection.Host}:{SelectedConnection.Port} 吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
		{
			DatabaseService.Instance.Delete(SelectedConnection.Id);
			Connections.Remove(SelectedConnection);
			ReindexConnections();
		}
	}

	private bool HasSelected()
	{
		return SelectedConnection != null;
	}

	private void ReindexConnections()
	{
		for (int i = 0; i < Connections.Count; i++)
		{
			Connections[i].Index = i + 1;
			Connections[i].SortOrder = i + 1;
		}
	}

	[RelayCommand]
	private void RepairCredential()
	{
		if (string.IsNullOrWhiteSpace(Host))
		{
			MessageBox.Show("请先选择或输入主机。", "提示");
			return;
		}
		RdpLauncher.ClearCredential(Host);
		MessageBox.Show("已清理 " + Host + " 的旧凭证，下次连接时会重新保存。", "凭证修复");
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

	[RelayCommand]
	private void ToggleRemote()
	{
		try
		{
			RdpServerService.SetRemoteEnabled(!IsRemoteEnabled);
			RefreshServerStatus();
			MessageBox.Show(IsRemoteEnabled ? "远程桌面已开启。" : "远程桌面已关闭。", "提示");
		}
		catch (Exception ex)
		{
			MessageBox.Show("操作失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	[RelayCommand]
	private void ChangePort()
	{
		int result;
		bool flag = !int.TryParse(NewPort, out result);
		if (!flag)
		{
			bool flag2 = ((result < 1 || result > 65535) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			MessageBox.Show("请输入有效端口（1-65535）。", "提示");
			return;
		}
		try
		{
			RdpServerService.SetPort(result);
			RefreshServerStatus();
			MessageBox.Show($"端口已修改为 {result}，重启后生效。", "提示");
		}
		catch (Exception ex)
		{
			MessageBox.Show("修改失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	[RelayCommand]
	private void RestorePort()
	{
		try
		{
			RdpServerService.SetPort(3389);
			RefreshServerStatus();
			MessageBox.Show("端口已还原为默认 3389，重启后生效。", "提示");
		}
		catch (Exception ex)
		{
			MessageBox.Show("还原失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	[RelayCommand]
	private void SetLaunchPassword()
	{
		SetLaunchPasswordWindow setLaunchPasswordWindow = new SetLaunchPasswordWindow();
		setLaunchPasswordWindow.Owner = Application.Current.MainWindow;
		setLaunchPasswordWindow.ShowDialog();
	}

	[RelayCommand]
	private void SetMasterPassword()
	{
		if (new MasterPasswordWindow(MasterPasswordPurpose.Set)
		{
			Owner = Application.Current.MainWindow
		}.ShowDialog().GetValueOrDefault())
		{
			HasMasterPassword = CryptoService.Instance.IsMasterPasswordConfigured();
			ReloadConnections();
		}
	}

	[RelayCommand]
	private void ExportData()
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Filter = "数据备份 (*.db)|*.db|所有文件 (*.*)|*.*",
			FileName = $"RemoteDesktopPlus-{DateTime.Now:yyyyMMdd-HHmmss}.db"
		};
		if (!saveFileDialog.ShowDialog().GetValueOrDefault())
		{
			return;
		}
		try
		{
			BackupService.Export(saveFileDialog.FileName);
			MessageBox.Show("导出成功。", "提示");
		}
		catch (Exception ex)
		{
			MessageBox.Show("导出失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	[RelayCommand]
	private void ImportData()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "数据备份 (*.db)|*.db|所有文件 (*.*)|*.*"
		};
		if (!openFileDialog.ShowDialog().GetValueOrDefault() || MessageBox.Show("导入会覆盖当前数据库（旧文件已自动备份为 data.db.bak）。\n如果原数据使用主密码加密，导入后请设置或解锁同一主密码。\n\n是否继续？", "确认导入", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
		{
			return;
		}
		try
		{
			BackupService.Import(openFileDialog.FileName);
			HasMasterPassword = CryptoService.Instance.IsMasterPasswordConfigured();
			if (HasMasterPassword)
			{
				MasterPasswordWindow masterPasswordWindow = new MasterPasswordWindow(MasterPasswordPurpose.Unlock);
				masterPasswordWindow.Owner = Application.Current.MainWindow;
				masterPasswordWindow.ShowDialog();
			}
			ReloadConnections();
			MessageBox.Show("导入成功。", "提示");
		}
		catch (Exception ex)
		{
			MessageBox.Show("导入失败：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	/// <summary>把「1920*1080」「1280x720」「全屏」解析为像素尺寸；「全屏」或非法值返回 (null, null)。</summary>
	public static (int? w, int? h) ParseResolution(string res)
	{
		if (string.IsNullOrEmpty(res) || res == "全屏")
		{
			return (w: null, h: null);
		}
		string[] array = res.Split('*', 'x', 'X');
		if (array.Length == 2 && int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2))
		{
			return (w: result, h: result2);
		}
		return (w: null, h: null);
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
			if (num2 > num)
			{
				num2--;
			}
			if (num >= 0 && num2 >= 0 && num != num2)
			{
				Connections.Move(num, num2);
				ReindexConnections();
				DatabaseService.Instance.UpdateSortOrders(Connections);
			}
		}
	}

	partial void OnResolutionChanged(string value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting(SettingKeys.OptionResolution, value ?? string.Empty);
		}
	}

	partial void OnRedirectLocalDriveChanged(bool value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting(SettingKeys.OptionRedirectDrive, value.ToString());
		}
	}

	partial void OnLocalDriveLetterChanged(string value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting(SettingKeys.OptionLocalDriveLetter, value ?? string.Empty);
		}
	}

	partial void OnForceConsoleChanged(bool value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting(SettingKeys.OptionForceConsole, value.ToString());
		}
	}

	partial void OnSelectedConnectionChanged(ConnectionItem? value)
	{
		if (value != null)
		{
			Host = value.Host;
			Port = value.Port;
			UserName = value.UserName;
			Password = CryptoService.Instance.Decrypt(value.EncryptedPassword);
		}
	}
}
