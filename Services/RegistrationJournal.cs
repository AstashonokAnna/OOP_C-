using System;
using System.IO;

namespace AdoreFlowerShop.Services
{
    /// <summary>Дополнительная фиксация регистраций в файл рядом с приложением (без пароля).</summary>
    internal static class RegistrationJournal
    {
        private const string FileName = "adore-registrations.log";
        private static readonly object Sync = new();

        public static void Append(string userName, string phoneDigitsOrRaw)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, FileName);
            var phone = string.IsNullOrWhiteSpace(phoneDigitsOrRaw) ? "-" : phoneDigitsOrRaw.Trim();
            var line = $"{DateTime.UtcNow:O}\tuser={userName}\trole=Client\tphone={phone}{Environment.NewLine}";
            lock (Sync)
            {
                File.AppendAllText(path, line);
            }
        }
    }
}
