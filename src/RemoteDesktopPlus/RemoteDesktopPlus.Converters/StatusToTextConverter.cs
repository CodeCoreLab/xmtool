using System;
using System.Globalization;
using System.Windows.Data;
using RemoteDesktopPlus.Models;

namespace RemoteDesktopPlus.Converters;

public sealed class StatusToTextConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is OnlineStatus)
		{
			switch ((OnlineStatus)value)
			{
			case OnlineStatus.Online:
				return "在线";
			case OnlineStatus.Offline:
				return "离线";
			case OnlineStatus.Checking:
				return "检测中";
			}
		}
		return "未知";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
