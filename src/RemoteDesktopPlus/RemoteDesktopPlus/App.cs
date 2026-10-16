using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.ViewModels;
using RemoteDesktopPlus.Views;

namespace RemoteDesktopPlus;

/// <summary>
/// 应用入口。视图结构见 App.xaml；<c>InitializeComponent</c> 与 <c>Main</c> 由 XAML 编译器生成
/// （见 obj\**\App.g.cs），故此处不再手写。
/// </summary>
public partial class App : Application
{
	private const string MutexName = "Local\\RemoteDesktopPlusPlus.SingleInstance.Mutex";

	private const string PipeName = "RemoteDesktopPlusPlus.SingleInstance.Activate";

	private Mutex? _instanceMutex;

	private CancellationTokenSource? _pipeCts;

	private TrayController? _tray;

	protected override void OnStartup(StartupEventArgs e)
	{
		SQLitePCL.Batteries.Init();
		// 单实例：第二个实例通过命名管道唤醒已运行的实例后自行退出。
		_instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
		if (!createdNew)
		{
			try
			{
				using NamedPipeClientStream pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
				pipe.Connect(800);
			}
			catch
			{
			}
			try
			{
				_instanceMutex.Dispose();
			}
			catch
			{
			}
			_instanceMutex = null;
			Shutdown();
			return;
		}

		base.OnStartup(e);
		RdpLauncher.CleanupTempFiles();
		DatabaseService db = DatabaseService.Instance;
		CryptoService crypto = CryptoService.Instance;
		db.Initialize();
		// 组合根：整个程序只有这里把服务实例与视图实现装配到一起。
		WpfDialogService dialogs = new WpfDialogService();

		if (db.HasLaunchPassword() && !new LoginWindow().ShowDialog().GetValueOrDefault())
		{
			Shutdown();
			return;
		}
		if (crypto.IsMasterPasswordConfigured() && !crypto.HasMasterKey)
		{
			dialogs.ShowMasterPassword(MasterPasswordPurpose.Unlock);
		}

		MainViewModel viewModel = new MainViewModel(db, crypto, dialogs);
		MainWindow mainWindow = new MainWindow(viewModel);
		base.MainWindow = mainWindow;
		mainWindow.Closed += delegate
		{
			Shutdown();
		};
		mainWindow.Show();
		_tray = new TrayController(mainWindow);
		StartActivationListener(mainWindow);
	}

	private void StartActivationListener(Window main)
	{
		_pipeCts = new CancellationTokenSource();
		CancellationToken token = _pipeCts.Token;
		Task.Run(async delegate
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					using NamedPipeServerStream server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
					await server.WaitForConnectionAsync(token).ConfigureAwait(continueOnCapturedContext: false);
					Dispatcher.Invoke(delegate
					{
						BringToFront(main);
					});
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch
				{
					try
					{
						await Task.Delay(300, token).ConfigureAwait(continueOnCapturedContext: false);
					}
					catch
					{
						break;
					}
				}
			}
		}, token);
	}

	private static void BringToFront(Window window)
	{
		if (window.WindowState == WindowState.Minimized)
		{
			window.WindowState = WindowState.Normal;
		}
		if (!window.IsVisible)
		{
			window.Show();
		}
		window.Activate();
		window.Topmost = true;
		window.Topmost = false;
		window.Focus();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		try
		{
			_tray?.Dispose();
		}
		catch
		{
		}
		try
		{
			_pipeCts?.Cancel();
			_pipeCts?.Dispose();
		}
		catch
		{
		}
		try
		{
			_instanceMutex?.ReleaseMutex();
		}
		catch (ApplicationException)
		{
		}
		catch
		{
		}
		try
		{
			_instanceMutex?.Dispose();
		}
		catch
		{
		}
		base.OnExit(e);
	}
}
