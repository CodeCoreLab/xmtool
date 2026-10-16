using System.Windows;
using Microsoft.Win32;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;

namespace RemoteDesktopPlus.Views;

/// <summary>
/// <see cref="IDialogService"/> 的 WPF 实现——整个程序里唯一一处把「视图模型的需求」翻译成
/// 「具体窗口」的地方。ViewModel 只依赖接口，不含任何 Window 类型。
/// </summary>
public sealed class WpfDialogService : IDialogService
{
	public void ShowMessage(string message, string title, DialogKind kind = DialogKind.Information)
	{
		MessageBox.Show(Owner(), message, title, MessageBoxButton.OK, ToImage(kind));
	}

	public bool Confirm(string message, string title, DialogKind kind = DialogKind.Warning)
	{
		Window? owner = OwnerOrNull();
		MessageBoxResult result = owner == null
			? MessageBox.Show(message, title, MessageBoxButton.YesNo, ToImage(kind))
			: MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, ToImage(kind));
		return result == MessageBoxResult.Yes;
	}

	public bool EditConnection(ConnectionItem? source, out ConnectionFormResult result)
	{
		EditConnectionWindow window = source == null ? new EditConnectionWindow() : new EditConnectionWindow(source);
		window.Owner = OwnerOrNull();
		bool ok = window.ShowDialog().GetValueOrDefault();
		if (ok)
		{
			result = window.Result;
		}
		else
		{
			// 独立实例而非 null：调用方在任何分支都能安全读取字段。
			result = new ConnectionFormResult();
		}
		return ok;
	}

	public bool SetLaunchPassword()
	{
		SetLaunchPasswordWindow window = new SetLaunchPasswordWindow();
		window.Owner = OwnerOrNull();
		return window.ShowDialog().GetValueOrDefault();
	}

	public bool ShowMasterPassword(MasterPasswordPurpose purpose)
	{
		MasterPasswordWindow window = new MasterPasswordWindow(purpose);
		window.Owner = OwnerOrNull();
		return window.ShowDialog().GetValueOrDefault();
	}

	public string? PickExportTarget(string suggestedFileName)
	{
		SaveFileDialog dialog = new SaveFileDialog
		{
			Filter = "数据备份 (*.db)|*.db|所有文件 (*.*)|*.*",
			FileName = suggestedFileName
		};
		return dialog.ShowDialog().GetValueOrDefault() ? dialog.FileName : null;
	}

	public string? PickImportSource()
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Filter = "数据备份 (*.db)|*.db|所有文件 (*.*)|*.*"
		};
		return dialog.ShowDialog().GetValueOrDefault() ? dialog.FileName : null;
	}

	private static MessageBoxImage ToImage(DialogKind kind)
	{
		return kind switch
		{
			DialogKind.Error => MessageBoxImage.Hand,
			DialogKind.Warning => MessageBoxImage.Exclamation,
			_ => MessageBoxImage.Asterisk,
		};
	}

	private static Window? OwnerOrNull()
	{
		return Application.Current?.MainWindow;
	}

	/// <summary>
	/// 无主窗口时也要能弹窗（例如启动阶段），回退到无 owner 的 MessageBox.Show 重载。
	/// </summary>
	private static Window Owner()
	{
		return Application.Current?.MainWindow ?? new Window();
	}
}
