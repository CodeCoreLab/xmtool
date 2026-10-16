using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RemoteDesktopPlus.Models;
using RemoteDesktopPlus.ViewModels;

namespace RemoteDesktopPlus.Views;

/// <summary>
/// 主窗口。视图结构见 Views/MainWindow.xaml。
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
	private bool _showPlainPasswordOnMain;

	public bool ShowPlainPasswordOnMain
	{
		get
		{
			return _showPlainPasswordOnMain;
		}
		set
		{
			if (_showPlainPasswordOnMain == value)
			{
				return;
			}
			_showPlainPasswordOnMain = value;
			OnPropertyChanged();
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
		if (base.DataContext is not MainViewModel mainViewModel)
		{
			return;
		}
		mainViewModel.PropertyChanged += delegate(object? _, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == nameof(MainViewModel.Password) && PwdBox.Password != mainViewModel.Password)
			{
				PwdBox.Password = mainViewModel.Password;
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
		AboutWindow aboutWindow = new AboutWindow
		{
			Owner = this
		};
		aboutWindow.ShowDialog();
	}

	private void ConnList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (sender is not DataGrid)
		{
			return;
		}
		DataGridRow? row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
		if (row?.Item is ConnectionItem item && base.DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.ConnectItem(item);
		}
	}

	private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
	{
		while (d != null)
		{
			if (d is T t)
			{
				return t;
			}
			d = VisualTreeHelper.GetParent(d);
		}
		return null;
	}

	private void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));
	}
}
