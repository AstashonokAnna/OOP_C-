using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace AdoreFlowerShop.Converters {
    public sealed class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }
    }

    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }

    /// <summary>Соответствие строки страны (RU/EN) эмодзи флага. Порядок правил важен: сначала более специфичные ключи.</summary>
    public sealed class CountryToFlagConverter : IValueConverter
    {
        private static readonly (string[] Keys, string Flag)[] Rules =
        {
            (new[] { "юар", "южная африка", "южноафриканск", "south africa", "south-africa", "z.a.r." }, "🇿🇦"),
            (new[] { "росси", "russia", "россия", "рф" }, "🇷🇺"),
            (new[] { "украин", "ukraine" }, "🇺🇦"),
            (new[] { "беларус", "belarus", "белорус" }, "🇧🇾"),
            (new[] { "казахстан", "kazakhstan" }, "🇰🇿"),
            (new[] { "таиланд", "таил", "thailand" }, "🇹🇭"),
            (new[] { "вьетнам", "vietnam", "вьет" }, "🇻🇳"),
            (new[] { "камбодж", "cambodia" }, "🇰🇭"),
            (new[] { "лаос", "laos" }, "🇱🇦"),
            (new[] { "мианм", "myanmar", "бирм" }, "🇲🇲"),
            (new[] { "малайз", "malaysia" }, "🇲🇾"),
            (new[] { "сингапур", "singapore" }, "🇸🇬"),
            (new[] { "индонез", "indonesia" }, "🇮🇩"),
            (new[] { "филиппин", "philippines" }, "🇵🇭"),
            (new[] { "индия", "india", "индии" }, "🇮🇳"),
            (new[] { "шри-ланк", "sri lanka" }, "🇱🇰"),
            (new[] { "бангладеш", "bangladesh" }, "🇧🇩"),
            (new[] { "пакистан", "pakistan" }, "🇵🇰"),
            (new[] { "непал", "nepal" }, "🇳🇵"),
            (new[] { "япон", "japan" }, "🇯🇵"),
            (new[] { "кита", "china", "кнр" }, "🇨🇳"),
            (new[] { "коре", "korea" }, "🇰🇷"),
            (new[] { "монгол", "mongolia" }, "🇲🇳"),
            (new[] { "австрал", "australia" }, "🇦🇺"),
            (new[] { "новая зеланд", "new zealand" }, "🇳🇿"),
            (new[] { "франц", "france" }, "🇫🇷"),
            (new[] { "герман", "german", "deutsch" }, "🇩🇪"),
            (new[] { "итали", "italy", "италия" }, "🇮🇹"),
            (new[] { "испани", "spain", "эспан" }, "🇪🇸"),
            (new[] { "португал", "portugal" }, "🇵🇹"),
            (new[] { "великобритан", "united kingdom", "britain", "england", "шотланд", "уэльс", "северная ирланд", "great britain", "uk" }, "🇬🇧"),
            (new[] { "ирланд", "ireland" }, "🇮🇪"),
            (new[] { "швейцар", "switzerland", "швейц" }, "🇨🇭"),
            (new[] { "австр", "austria", "австрия" }, "🇦🇹"),
            (new[] { "бельг", "belgium" }, "🇧🇪"),
            (new[] { "нидерланд", "голланд", "netherlands", "holland" }, "🇳🇱"),
            (new[] { "люксембург", "luxembourg" }, "🇱🇺"),
            (new[] { "дани", "denmark", "дания" }, "🇩🇰"),
            (new[] { "швед", "sweden", "швеция" }, "🇸🇪"),
            (new[] { "норвег", "norway" }, "🇳🇴"),
            (new[] { "финлянд", "finland", "финляндия" }, "🇫🇮"),
            (new[] { "исланд", "iceland" }, "🇮🇸"),
            (new[] { "польш", "poland", "польша" }, "🇵🇱"),
            (new[] { "чех", "czech", "чешск", "чехия" }, "🇨🇿"),
            (new[] { "словац", "slovakia", "словацк" }, "🇸🇰"),
            (new[] { "венгр", "hungary", "венгри" }, "🇭🇺"),
            (new[] { "румын", "romania", "румыни" }, "🇷🇴"),
            (new[] { "болгар", "bulgaria", "болгари" }, "🇧🇬"),
            (new[] { "грец", "greece", "греция", "эллад" }, "🇬🇷"),
            (new[] { "хорват", "croatia", "хорвати" }, "🇭🇷"),
            (new[] { "серб", "serbia", "серби" }, "🇷🇸"),
            (new[] { "словен", "slovenia", "словени" }, "🇸🇮"),
            (new[] { "литв", "lithuania", "литва" }, "🇱🇹"),
            (new[] { "латв", "latvia", "латви" }, "🇱🇻"),
            (new[] { "эстон", "estonia", "эстони" }, "🇪🇪"),
            (new[] { "мальт", "malta" }, "🇲🇹"),
            (new[] { "кипр", "cyprus" }, "🇨🇾"),
            (new[] { "молдав", "moldova", "молдова" }, "🇲🇩"),
            (new[] { "грузи", "georgia", "сакартвело" }, "🇬🇪"),
            (new[] { "армен", "armenia", "армени" }, "🇦🇲"),
            (new[] { "азербайджан", "azerbaijan" }, "🇦🇿"),
            (new[] { "узбек", "uzbekistan" }, "🇺🇿"),
            (new[] { "кыргыз", "kyrgyz", "киргиз" }, "🇰🇬"),
            (new[] { "таджик", "tajikistan" }, "🇹🇯"),
            (new[] { "туркмен", "turkmenistan" }, "🇹🇲"),
            (new[] { "афган", "afghanistan" }, "🇦🇫"),
            (new[] { "иран", "iran", "персия" }, "🇮🇷"),
            (new[] { "ирак", "iraq" }, "🇮🇶"),
            (new[] { "израил", "israel" }, "🇮🇱"),
            (new[] { "палестин", "palestine" }, "🇵🇸"),
            (new[] { "иордан", "jordan" }, "🇯🇴"),
            (new[] { "ливан", "lebanon" }, "🇱🇧"),
            (new[] { "сири", "syria" }, "🇸🇾"),
            (new[] { "турц", "turkey", "турци" }, "🇹🇷"),
            (new[] { "саудов", "saudi", "саудовск" }, "🇸🇦"),
            (new[] { "оаэ", "эмират", "uae", "дубай", "абу-даби", "united arab" }, "🇦🇪"),
            (new[] { "катар", "qatar" }, "🇶🇦"),
            (new[] { "кувейт", "kuwait" }, "🇰🇼"),
            (new[] { "оман", "oman" }, "🇴🇲"),
            (new[] { "бахрейн", "bahrain" }, "🇧🇭"),
            (new[] { "йемен", "yemen" }, "🇾🇪"),
            (new[] { "египет", "egypt", "египт" }, "🇪🇬"),
            (new[] { "марокк", "morocco", "марокко" }, "🇲🇦"),
            (new[] { "алжир", "algeria" }, "🇩🇿"),
            (new[] { "тунис", "tunisia" }, "🇹🇳"),
            (new[] { "лив", "libya" }, "🇱🇾"),
            (new[] { "судан", "sudan" }, "🇸🇩"),
            (new[] { "эфиоп", "ethiopia", "эфиопи" }, "🇪🇹"),
            (new[] { "кени", "kenya" }, "🇰🇪"),
            (new[] { "танзан", "tanzania" }, "🇹🇿"),
            (new[] { "уганд", "uganda" }, "🇺🇬"),
            (new[] { "нигер", "nigeria", "нигерии", "нигерия" }, "🇳🇬"),
            (new[] { "канад", "canada" }, "🇨🇦"),
            (new[] { "сша", "usa", "united states", "америк", "u.s.", "u.s.a." }, "🇺🇸"),
            (new[] { "мексик", "mexico", "мексика" }, "🇲🇽"),
            (new[] { "куб", "cuba", "куба" }, "🇨🇺"),
            (new[] { "ямайк", "jamaica" }, "🇯🇲"),
            (new[] { "бразил", "brazil", "бразили" }, "🇧🇷"),
            (new[] { "аргентин", "argentina" }, "🇦🇷"),
            (new[] { "чили", "chile", "республика чили" }, "🇨🇱"),
            (new[] { "перу", "peru" }, "🇵🇪"),
            (new[] { "колумб", "colombia", "колумби" }, "🇨🇴"),
            (new[] { "венесуэл", "venezuela" }, "🇻🇪"),
            (new[] { "эквадор", "ecuador" }, "🇪🇨"),
            (new[] { "уругва", "uruguay" }, "🇺🇾"),
            (new[] { "парагва", "paraguay" }, "🇵🇾"),
            (new[] { "болив", "bolivia" }, "🇧🇴"),
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return "🏳️";

            var n = Normalize(s);
            foreach (var (keys, flag) in Rules)
            {
                if (keys.Any(k => n.Contains(Normalize(k), StringComparison.Ordinal)))
                    return flag;
            }

            return "🏳️";
        }

        private static string Normalize(string text)
        {
            var t = text.Trim().ToLowerInvariant();
            return t.Replace('ё', 'е');
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
