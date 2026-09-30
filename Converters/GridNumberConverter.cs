using System;
using System.Globalization;
using System.Windows.Data;
using AdoreFlowerShop.Services;

namespace AdoreFlowerShop.Converters
{
    /// <summary>Формат чисел в DataGrid (P — цена, Q — количество, D — скидка, T — сумма заказа с копейками).</summary>
    public sealed class GridNumberConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (!TryToDouble(value, out var number))
                return value.ToString() ?? string.Empty;

            var mode = parameter as string ?? "P";
            return mode.ToUpperInvariant() switch
            {
                "Q" => number.ToString("N0", culture),
                "D" => number.ToString("N1", culture),
                "T" => number.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")),
                _ => FormatPrice(number, culture)
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var text = value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return 0d;

            if (PriceParsing.TryParse(text, out var parsed))
                return parsed;

            return Binding.DoNothing;
        }

        private static string FormatPrice(double number, CultureInfo culture)
        {
            if (Math.Abs(number % 1) < 0.0000001)
                return number.ToString("N0", culture);

            return number.ToString("N2", culture);
        }

        private static bool TryToDouble(object value, out double number)
        {
            switch (value)
            {
                case double d:
                    number = d;
                    return true;
                case float f:
                    number = f;
                    return true;
                case decimal m:
                    number = (double)m;
                    return true;
                case int i:
                    number = i;
                    return true;
                case long l:
                    number = l;
                    return true;
                default:
                    return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
            }
        }
    }
}
