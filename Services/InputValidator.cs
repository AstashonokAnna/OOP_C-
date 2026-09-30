using System;
using System.Linq;

namespace AdoreFlowerShop.Services
{
    public static class InputValidator
    {
        public static bool IsUsernameChar(char c) =>
            char.IsLetterOrDigit(c) || c is '_' or '-' or '.';

        public static int CountDigits(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            var n = 0;
            foreach (var c in s)
            {
                if (c is >= '0' and <= '9')
                    n++;
            }

            return n;
        }

        public static string NormalizePhoneDigits(string? phone)
        {
            if (string.IsNullOrEmpty(phone))
                return string.Empty;

            Span<char> buf = stackalloc char[phone.Length];
            var j = 0;
            foreach (var c in phone)
            {
                if (c is >= '0' and <= '9')
                    buf[j++] = c;
            }

            return new string(buf[..j]);
        }

        public static string? ValidateUsername(string? name)
        {
            var trimmed = name?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
                return "Введите имя пользователя.";
            if (trimmed.Length < InputLimits.UsernameMin)
                return $"Имя пользователя — от {InputLimits.UsernameMin} до {InputLimits.UsernameMax} символов.";
            if (trimmed.Length > InputLimits.UsernameMax)
                return $"Имя пользователя — не более {InputLimits.UsernameMax} символов.";
            if (trimmed.Any(c => !IsUsernameChar(c)))
                return "Имя может содержать только буквы, цифры, точку, дефис и подчёркивание.";

            return null;
        }

        public static string? ValidatePassword(string? password, bool allowEmpty = false)
        {
            if (string.IsNullOrEmpty(password))
                return allowEmpty ? null : "Введите пароль.";

            if (password.Any(char.IsWhiteSpace))
                return "Пароль не должен содержать пробелы.";
            if (password.Length < InputLimits.PasswordMin)
                return $"Пароль — от {InputLimits.PasswordMin} до {InputLimits.PasswordMax} символов.";
            if (password.Length > InputLimits.PasswordMax)
                return $"Пароль — не более {InputLimits.PasswordMax} символов.";

            return null;
        }

        public static string? ValidatePasswordMatch(string password, string confirmPassword)
        {
            if (password != confirmPassword)
                return "Пароли не совпадают.";

            return null;
        }

        public static string? ValidatePhone(string? phone)
        {
            var digits = CountDigits(phone);
            if (digits != InputLimits.PhoneDigits)
                return $"Укажите номер телефона: ровно {InputLimits.PhoneDigits} цифр (можно с +, пробелами и скобками).";

            return null;
        }

        public static string? ValidateRegistration(string name, string password, string confirmPassword, string phone)
        {
            return ValidateUsername(name)
                ?? ValidatePassword(password)
                ?? ValidatePasswordMatch(password, confirmPassword)
                ?? ValidatePhone(phone);
        }

        public static string? ValidateLogin(string name, string password)
        {
            var userErr = ValidateUsername(name);
            if (userErr != null)
                return userErr;

            var trimmed = name.Trim();
            if (string.Equals(trimmed, "admin", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(password))
                return null;

            if (string.IsNullOrEmpty(password))
                return "Введите пароль.";
            if (password.Length > InputLimits.PasswordMax)
                return $"Пароль — не более {InputLimits.PasswordMax} символов.";

            return null;
        }

        public static string? ValidatePasswordChange(string newPassword, string confirmPassword)
        {
            return ValidatePassword(newPassword)
                ?? ValidatePasswordMatch(newPassword, confirmPassword);
        }

        public static string? ValidateAddress(string? address)
        {
            var trimmed = address?.Trim() ?? string.Empty;
            if (trimmed.Length < InputLimits.AddressMin)
                return $"Адрес доставки — от {InputLimits.AddressMin} до {InputLimits.AddressMax} символов.";
            if (trimmed.Length > InputLimits.AddressMax)
                return $"Адрес доставки — не более {InputLimits.AddressMax} символов.";

            return null;
        }

        public static string? ValidateReviewComment(string? comment)
        {
            var trimmed = comment?.Trim() ?? string.Empty;
            if (trimmed.Length < InputLimits.ReviewCommentMin)
                return $"Текст отзыва — от {InputLimits.ReviewCommentMin} до {InputLimits.ReviewCommentMax} символов.";
            if (trimmed.Length > InputLimits.ReviewCommentMax)
                return $"Текст отзыва — не более {InputLimits.ReviewCommentMax} символов.";

            return null;
        }

        public static string? ValidateFlowerShortName(string? name)
        {
            var trimmed = name?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
                return "Укажите название цветка.";
            if (trimmed.Length > InputLimits.FlowerShortNameMax)
                return $"Название — не более {InputLimits.FlowerShortNameMax} символов.";

            return null;
        }

        public static string? ValidateFlowerDescription(string? description)
        {
            if (description != null && description.Length > InputLimits.FlowerDescriptionMax)
                return $"Описание — не более {InputLimits.FlowerDescriptionMax} символов.";

            return null;
        }

        public static string? ValidateFlowerCountry(string? country)
        {
            if (country != null && country.Trim().Length > InputLimits.FlowerCountryMax)
                return $"Страна — не более {InputLimits.FlowerCountryMax} символов.";

            return null;
        }

        public static string? ValidateFlowerColor(string? color)
        {
            if (color != null && color.Trim().Length > InputLimits.FlowerColorMax)
                return $"Цвет — не более {InputLimits.FlowerColorMax} символов.";

            return null;
        }

        public static string? ValidateFlowerDiscount(double discount)
        {
            if (discount < 0 || discount > 100)
                return "Скидка должна быть от 0 до 100%.";

            return null;
        }

        public static string? ValidateFlower(FlowerShop.Flower flower)
        {
            return ValidateFlowerShortName(flower.ShortName)
                ?? ValidateFlowerDescription(flower.Description)
                ?? ValidateFlowerCountry(flower.Country)
                ?? ValidateFlowerColor(flower.Color)
                ?? ValidateFlowerDiscount(flower.Discount);
        }

        public static string? ValidateDeliveryDate(DateTime deliveryDate)
        {
            if (deliveryDate.Date < DateTime.Today)
                return "Дата доставки не может быть в прошлом.";

            return null;
        }
    }
}
