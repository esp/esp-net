using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Esp.Net.Examples.ReactiveModel.Common.UI
{
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}