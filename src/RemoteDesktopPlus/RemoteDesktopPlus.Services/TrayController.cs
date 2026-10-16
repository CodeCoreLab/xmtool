using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace RemoteDesktopPlus.Services;

/// <summary>
/// 系统托盘控制器：最小化/关闭到托盘，左键单击恢复，右键菜单（打开/退出）。
/// 新增功能，原版无此能力。
/// </summary>
public class TrayController : IDisposable
{
	private readonly Window _main;
	private readonly NotifyIcon _icon;
	private bool _disposed;
	private bool _exitRequested;

	public TrayController(Window main)
	{
		_main = main;
		_icon = new NotifyIcon
		{
			Text = "小马远程桌面",
			Visible = true
		};
		try
		{
			using var stream = Application.GetResourceStream(new Uri("pack://application:,,,/app.ico")).Stream;
			_icon.Icon = new Icon(stream);
		}
		catch
		{
			_icon.Icon = SystemIcons.Application;
		}

		var menu = new ContextMenuStrip();
		menu.Items.Add("打开", null, (_, _) => RestoreFromTray());
		menu.Items.Add("-");
		menu.Items.Add("退出", null, (_, _) => ExitApplication());
		_icon.ContextMenuStrip = menu;
		_icon.DoubleClick += (_, _) => RestoreFromTray();
		_icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) RestoreFromTray(); };

		_main.StateChanged += OnStateChanged;
		_main.Closing += OnClosing;
	}

	private void OnStateChanged(object sender, EventArgs e)
	{
		// 最小化 → 藏进托盘
		if (_main.WindowState == WindowState.Minimized)
		{
			_main.Hide();
		}
	}

	private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		if (!_exitRequested)
		{
			// 点关闭 → 隐藏到托盘而不是退出
			e.Cancel = true;
			_main.Hide();
		}
	}

	private void RestoreFromTray()
	{
		_main.Show();
		_main.WindowState = WindowState.Normal;
		_main.Activate();
		_main.Topmost = true;
		_main.Topmost = false;
	}

	private void ExitApplication()
	{
		_exitRequested = true;
		_icon.Visible = false;
		_main.Close();
		Application.Current.Shutdown();
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_main.StateChanged -= OnStateChanged;
		_main.Closing -= OnClosing;
		_icon.Visible = false;
		_icon.Dispose();
	}
}
