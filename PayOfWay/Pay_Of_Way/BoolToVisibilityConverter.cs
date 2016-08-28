using System;
using System.Windows;
using System.Windows.Data;

namespace Pay_Of_Way
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		private object GetVisibility(object value)
		{
			if (!(value is bool))
				return Visibility.Collapsed;
			bool objValue = (bool)value;
			if (objValue)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool b = (bool)value;
			return GetVisibility((string)parameter == "Reverse" ? !b : b);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
