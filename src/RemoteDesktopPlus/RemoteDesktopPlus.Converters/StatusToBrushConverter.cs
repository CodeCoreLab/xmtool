using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RemoteDesktopPlus.Models;

namespace RemoteDesktopPlus.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is OnlineStatus)
		{
			switch ((OnlineStatus)value)
			{
			case OnlineStatus.Online:
				return new SolidColorBrush(Color.FromRgb(26, 174, 57));
			case OnlineStatus.Offline:
				return new SolidColorBrush(Color.FromRgb(224, 49, 49));
			case OnlineStatus.Checking:
				return new SolidColorBrush(Color.FromRgb(221, 91, 0));
			}
		}
		return new SolidColorBrush(Color.FromRgb(163, 158, 152));
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
