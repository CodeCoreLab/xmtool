using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.ViewModels;

namespace RemoteDesktopPlus.Views;

public class MainWindow : Window, INotifyPropertyChanged, IComponentConnector
{
	private bool _showPlainPasswordOnMain;

	internal PasswordBox PwdBox;

	internal TextBox PlainPwdBox;

	internal DataGrid ConnList;

	private bool _contentLoaded;

	public bool ShowPlainPasswordOnMain
	{
		get
		{
			return _showPlainPasswordOnMain;
		}
		set
		{
			if (_showPlainPasswordOnMain != value)
			{
				_showPlainPasswordOnMain = value;
				OnPropertyChanged("ShowPlainPasswordOnMain");
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public MainWindow()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		object dataContext = base.DataContext;
		MainViewModel vm = dataContext as MainViewModel;
		if (vm == null)
		{
			return;
		}
		vm.PropertyChanged += delegate(object? _, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Password" && PwdBox.Password != vm.Password)
			{
				PwdBox.Password = vm.Password;
			}
		};
	}

	private void PwdBox_PasswordChanged(object sender, RoutedEventArgs e)
	{
		if (sender is PasswordBox passwordBox && base.DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.Password = passwordBox.Password;
		}
	}

	private void MinimizeButton_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void AboutButton_Click(object sender, RoutedEventArgs e)
	{
		AboutWindow aboutWindow = new AboutWindow();
		aboutWindow.Owner = this;
		aboutWindow.ShowDialog();
	}

	private void ConnList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is DataGrid)
		{
			object originalSource = e.OriginalSource;
			DataGridRow dataGridRow = FindAncestor<DataGridRow>((DependencyObject?)((originalSource is DependencyObject) ? originalSource : null));
			if (dataGridRow != null && dataGridRow.Item is ConnectionItem item && base.DataContext is MainViewModel mainViewModel)
			{
				mainViewModel.ConnectItem(item);
			}
		}
	}

	private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
	{
		while (d != null)
		{
			T val = (T)(object)((d is T) ? d : null);
			if (val != null)
			{
				return val;
			}
			d = VisualTreeHelper.GetParent(d);
		}
		return default(T);
	}

	private void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/远程桌面连接++;component/views/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.5.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Button)target).Click += AboutButton_Click;
			break;
		case 2:
			((Button)target).Click += MinimizeButton_Click;
			break;
		case 3:
			((Button)target).Click += CloseButton_Click;
			break;
		case 4:
			PwdBox = (PasswordBox)target;
			PwdBox.PasswordChanged += PwdBox_PasswordChanged;
			break;
		case 5:
			PlainPwdBox = (TextBox)target;
			break;
		case 6:
			ConnList = (DataGrid)target;
			ConnList.MouseDoubleClick += ConnList_MouseDoubleClick;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
