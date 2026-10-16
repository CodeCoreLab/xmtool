using System;
using System.Globalization;
using System.Windows.Data;

namespace RemoteDesktopPlus.Converters;

public sealed class BoolToToggleTextConverter : IValueConverter
{
	public static readonly BoolToToggleTextConverter Instance = new BoolToToggleTextConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (!(value is bool) || !(bool)value)
		{
			return "开启远程";
		}
		return "关闭远程";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
