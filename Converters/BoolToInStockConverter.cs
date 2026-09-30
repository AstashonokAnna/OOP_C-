using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AdoreFlowerShop.Converters
{
    public sealed class BoolToInStockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inStock = value is bool b && b;
            var key = inStock ? "InStockYes" : "InStockNo";
            return Application.Current.TryFindResource(key) as string ?? (inStock ? "Yes" : "No");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
