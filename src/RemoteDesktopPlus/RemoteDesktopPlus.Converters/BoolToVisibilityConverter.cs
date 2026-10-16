using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RemoteDesktopPlus.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
	public static readonly BoolToVisibilityConverter Instance = new BoolToVisibilityConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return (!(value is bool) || !(bool)value) ? Visibility.Collapsed : Visibility.Visible;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is Visibility visibility && visibility == Visibility.Visible;
	}
}
