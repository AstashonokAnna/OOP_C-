using System;
using System.Globalization;
using System.Linq;

namespace AdoreFlowerShop.Services
{
    /// <summary>Разбор цены из текста: 4500, 4 500, 4.500 — без путаницы с 4,50.</summary>
    internal static class PriceParsing
    {
        public static bool TryParse(string? text, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var culture = CultureInfo.CurrentCulture;
            var s = RemoveGroupSeparators(text.Trim(), culture);

            if (TryParseThousandsWithDotOrComma(s, culture, out value))
                return true;

            if (double.TryParse(s, NumberStyles.Any, culture, out value))
                return true;

            var normalized = culture.NumberFormat.NumberDecimalSeparator == ","
                ? s.Replace('.', ',')
                : s.Replace(',', '.');

            return double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static string RemoveGroupSeparators(string s, CultureInfo culture)
        {
            var group = culture.NumberFormat.NumberGroupSeparator;
            if (!string.IsNullOrEmpty(group))
                s = s.Replace(group, string.Empty);

            return s.Replace(" ", string.Empty).Replace("\u00A0", string.Empty);
        }

        /// <summary>4.500 → 4500 (ru), 4,500 → 4500 (en).</summary>
        private static bool TryParseThousandsWithDotOrComma(string s, CultureInfo culture, out double value)
        {
            value = 0;
            var decimalSep = culture.NumberFormat.NumberDecimalSeparator;

            if (decimalSep == ",")
            {
                var dot = s.IndexOf('.');
                if (dot >= 0 && !s.Contains(','))
                    return TryJoinThousandsParts(s[..dot], s[(dot + 1)..], out value);
            }
            else if (decimalSep == ".")
            {
                var comma = s.IndexOf(',');
                if (comma >= 0 && !s.Contains('.'))
                    return TryJoinThousandsParts(s[..comma], s[(comma + 1)..], out value);
            }

            return false;
        }

        private static bool TryJoinThousandsParts(string before, string after, out double value)
        {
            value = 0;
            if (before.Length == 0 || after.Length != 3)
                return false;
            if (!before.All(char.IsDigit) || !after.All(char.IsDigit))
                return false;

            return double.TryParse(before + after, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }
    }
}
