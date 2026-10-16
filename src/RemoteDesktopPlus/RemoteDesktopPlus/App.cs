using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RemoteDesktopPlus.Services;
using RemoteDesktopPlus.Views;

namespace RemoteDesktopPlus;

public class App : Application
{
	private const string MutexName = "Local\\RemoteDesktopPlusPlus.SingleInstance.Mutex";

	private const string PipeName = "RemoteDesktopPlusPlus.SingleInstance.Activate";

	private Mutex? _instanceMutex;

	private CancellationTokenSource? _pipeCts;

	private TrayController? _tray;

	private bool _contentLoaded;

	protected override void OnStartup(StartupEventArgs e)
	{
		SQLitePCL.Batteries.Init();
		_instanceMutex = new Mutex(initiallyOwned: true, "Local\\RemoteDesktopPlusPlus.SingleInstance.Mutex", out var createdNew);
		if (!createdNew)
		{
			try
			{
				using NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "RemoteDesktopPlusPlus.SingleInstance.Activate", PipeDirection.Out);
				namedPipeClientStream.Connect(800);
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
		DatabaseService.Instance.Initialize();
		if (DatabaseService.Instance.HasLaunchPassword() && !new LoginWindow().ShowDialog().GetValueOrDefault())
		{
			Shutdown();
			return;
		}
		if (CryptoService.IsMasterPasswordConfigured() && !CryptoService.Instance.HasMasterKey)
		{
			new MasterPasswordWindow(MasterPasswordPurpose.Unlock).ShowDialog();
		}
		MainWindow mainWindow = (MainWindow)(base.MainWindow = new MainWindow());
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
		Window main2 = main;
		_pipeCts = new CancellationTokenSource();
		CancellationToken token = _pipeCts.Token;
		Task.Run(async delegate
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					using NamedPipeServerStream server = new NamedPipeServerStream("RemoteDesktopPlusPlus.SingleInstance.Activate", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
					await server.WaitForConnectionAsync(token).ConfigureAwait(continueOnCapturedContext: false);
					((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
					{
						BringToFront(main2);
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

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/远程桌面连接++;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
