using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RemoteDesktopPlus.Converters;

public sealed class BoolToBrushConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return new SolidColorBrush((value is int num && num != 0) ? Color.FromRgb(26, 174, 57) : Color.FromRgb(163, 158, 152));
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
