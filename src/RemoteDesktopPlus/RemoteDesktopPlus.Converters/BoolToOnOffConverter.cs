using System;
using System.Globalization;
using System.Windows.Data;

namespace RemoteDesktopPlus.Converters;

public sealed class BoolToOnOffConverter : IValueConverter
{
	public static readonly BoolToOnOffConverter Instance = new BoolToOnOffConverter();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (!(value is bool) || !(bool)value)
		{
			return "关闭";
		}
		return "开启";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
