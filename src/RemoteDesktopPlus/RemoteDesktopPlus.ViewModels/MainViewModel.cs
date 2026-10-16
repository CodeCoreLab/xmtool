using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Win32;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.Views;

namespace RemoteDesktopPlus.ViewModels;

public class MainViewModel : ObservableObject, IDropTarget
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? connectCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? newItemCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? editSelectedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? saveItemCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? deleteSelectedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? repairCredentialCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private AsyncRelayCommand? scanOnlineCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? toggleRemoteCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? changePortCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? restorePortCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? setLaunchPasswordCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? setMasterPasswordCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? exportDataCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	private RelayCommand? importDataCommand;

	public ObservableCollection<ConnectionItem> Connections { get; } = new ObservableCollection<ConnectionItem>();


	public IReadOnlyList<string> ResolutionOptions { get; } = new string[10] { "全屏", "1024*768", "1280*720", "1366*768", "1440*900", "1600*900", "1680*1050", "1920*1080", "2560*1440", "3840*2160" };


	public IReadOnlyList<string> DriveLetters { get; } = BuildDriveLetters();


	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Host
	{
		get
		{
			return host;
		}
		[MemberNotNull("host")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(host, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Host);
				host = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Host);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int Port
	{
		get
		{
			return port;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(port, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Port);
				port = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Port);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string UserName
	{
		get
		{
			return userName;
		}
		[MemberNotNull("userName")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(userName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UserName);
				userName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UserName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Password
	{
		get
		{
			return password;
		}
		[MemberNotNull("password")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(password, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Password);
				password = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Password);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string Resolution
	{
		get
		{
			return resolution;
		}
		[MemberNotNull("resolution")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(resolution, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Resolution);
				resolution = value;
				OnResolutionChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Resolution);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool RedirectLocalDrive
	{
		get
		{
			return redirectLocalDrive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(redirectLocalDrive, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RedirectLocalDrive);
				redirectLocalDrive = value;
				OnRedirectLocalDriveChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RedirectLocalDrive);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string LocalDriveLetter
	{
		get
		{
			return localDriveLetter;
		}
		[MemberNotNull("localDriveLetter")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(localDriveLetter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LocalDriveLetter);
				localDriveLetter = value;
				OnLocalDriveLetterChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LocalDriveLetter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ForceConsole
	{
		get
		{
			return forceConsole;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(forceConsole, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ForceConsole);
				forceConsole = value;
				OnForceConsoleChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ForceConsole);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool OptionsExpanded
	{
		get
		{
			return optionsExpanded;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(optionsExpanded, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OptionsExpanded);
				optionsExpanded = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OptionsExpanded);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsRemoteEnabled
	{
		get
		{
			return isRemoteEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isRemoteEnabled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRemoteEnabled);
				isRemoteEnabled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRemoteEnabled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public int CurrentRdpPort
	{
		get
		{
			return currentRdpPort;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(currentRdpPort, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentRdpPort);
				currentRdpPort = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentRdpPort);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsPortListening
	{
		get
		{
			return isPortListening;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isPortListening, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsPortListening);
				isPortListening = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsPortListening);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public string NewPort
	{
		get
		{
			return newPort;
		}
		[MemberNotNull("newPort")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(newPort, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NewPort);
				newPort = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NewPort);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HasMasterPassword
	{
		get
		{
			return hasMasterPassword;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(hasMasterPassword, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasMasterPassword);
				hasMasterPassword = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasMasterPassword);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public ConnectionItem? SelectedConnection
	{
		get
		{
			return selectedConnection;
		}
		set
		{
			if (!EqualityComparer<ConnectionItem>.Default.Equals(selectedConnection, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedConnection);
				selectedConnection = value;
				OnSelectedConnectionChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedConnection);
				ConnectCommand.NotifyCanExecuteChanged();
				DeleteSelectedCommand.NotifyCanExecuteChanged();
				EditSelectedCommand.NotifyCanExecuteChanged();
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ConnectCommand => connectCommand ?? (connectCommand = new RelayCommand(Connect, CanConnect));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand NewItemCommand => newItemCommand ?? (newItemCommand = new RelayCommand(NewItem));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand EditSelectedCommand => editSelectedCommand ?? (editSelectedCommand = new RelayCommand(EditSelected, HasSelected));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SaveItemCommand => saveItemCommand ?? (saveItemCommand = new RelayCommand(SaveItem));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand DeleteSelectedCommand => deleteSelectedCommand ?? (deleteSelectedCommand = new RelayCommand(DeleteSelected, HasSelected));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RepairCredentialCommand => repairCredentialCommand ?? (repairCredentialCommand = new RelayCommand(RepairCredential));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ScanOnlineCommand => scanOnlineCommand ?? (scanOnlineCommand = new AsyncRelayCommand(ScanOnlineAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleRemoteCommand => toggleRemoteCommand ?? (toggleRemoteCommand = new RelayCommand(ToggleRemote));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ChangePortCommand => changePortCommand ?? (changePortCommand = new RelayCommand(ChangePort));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RestorePortCommand => restorePortCommand ?? (restorePortCommand = new RelayCommand(RestorePort));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SetLaunchPasswordCommand => setLaunchPasswordCommand ?? (setLaunchPasswordCommand = new RelayCommand(SetLaunchPassword));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SetMasterPasswordCommand => setMasterPasswordCommand ?? (setMasterPasswordCommand = new RelayCommand(SetMasterPassword));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ExportDataCommand => exportDataCommand ?? (exportDataCommand = new RelayCommand(ExportData));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.3.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ImportDataCommand => importDataCommand ?? (importDataCommand = new RelayCommand(ImportData));

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
		HasMasterPassword = CryptoService.IsMasterPasswordConfigured();
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
			string setting = instance.GetSetting("options.resolution");
			if (!string.IsNullOrEmpty(setting) && ResolutionOptions.Contains(setting))
			{
				Resolution = setting;
			}
			if (bool.TryParse(instance.GetSetting("options.redirect_drive"), out var result))
			{
				RedirectLocalDrive = result;
			}
			string setting2 = instance.GetSetting("options.local_drive_letter");
			if (!string.IsNullOrEmpty(setting2) && DriveLetters.Contains(setting2))
			{
				LocalDriveLetter = setting2;
			}
			if (bool.TryParse(instance.GetSetting("options.force_console"), out var result2))
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
			HasMasterPassword = CryptoService.IsMasterPasswordConfigured();
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
			HasMasterPassword = CryptoService.IsMasterPasswordConfigured();
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

	private static (int? w, int? h) ParseResolution(string res)
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnResolutionChanged(string value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting("options.resolution", value ?? string.Empty);
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnRedirectLocalDriveChanged(bool value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting("options.redirect_drive", value.ToString());
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnLocalDriveLetterChanged(string value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting("options.local_drive_letter", value ?? string.Empty);
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnForceConsoleChanged(bool value)
	{
		if (!_suppressOptionSave)
		{
			DatabaseService.Instance.SetSetting("options.force_console", value.ToString());
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.3.0.0")]
	private void OnSelectedConnectionChanged(ConnectionItem? value)
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
