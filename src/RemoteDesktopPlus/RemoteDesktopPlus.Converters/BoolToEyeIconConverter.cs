using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RemoteDesktopPlus.Converters;

public sealed class BoolToEyeIconConverter : IValueConverter
{
	public static readonly BoolToEyeIconConverter Instance = new BoolToEyeIconConverter();

	private static readonly Geometry EyeOpen = Geometry.Parse("M12,4.5C7,4.5 2.7,7.6 1,12 2.7,16.4 7,19.5 12,19.5 17,19.5 21.3,16.4 23,12 21.3,7.6 17,4.5 12,4.5z M12,17A5,5 0 0 1 7,12 5,5 0 0 1 12,7 5,5 0 0 1 17,12 5,5 0 0 1 12,17z M12,9A3,3 0 0 0 9,12 3,3 0 0 0 12,15 3,3 0 0 0 15,12 3,3 0 0 0 12,9z");

	private static readonly Geometry EyeClosed = Geometry.Parse("M2,5.27L3.28,4 20,20.72 18.73,22 15.65,18.92C14.5,19.3 13.28,19.5 12,19.5 7,19.5 2.73,16.39 1,12 1.69,10.24 2.79,8.69 4.19,7.46L2,5.27z M12,9A3,3 0 0 1 15,12 3,3 0 0 1 14.83,13.12L11.88,10.18C12.25,10.06 12.62,10 13,10z M12,4.5C17,4.5 21.27,7.61 23,12 22.18,14.08 20.79,15.88 19,17.19L17.58,15.76C18.94,14.82 20.06,13.54 20.82,12 19.17,8.64 15.76,6.5 12,6.5 10.91,6.5 9.84,6.68 8.84,7L7.3,5.47C8.74,4.85 10.32,4.5 12,4.5z M3.18,12C4.83,15.36 8.24,17.5 12,17.5 12.69,17.5 13.37,17.43 14,17.29L11.72,15A3,3 0 0 1 9,12.28L5.6,8.87C4.61,9.72 3.78,10.78 3.18,12z");

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (!(value is bool) || !(bool)value)
		{
			return EyeClosed;
		}
		return EyeOpen;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
