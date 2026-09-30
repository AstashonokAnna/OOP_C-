using System;
using System.IO;
using Microsoft.Win32;

namespace AdoreFlowerShop.Services
{
    /// <summary>Выбор файла изображения для позиции каталога.</summary>
    public static class FlowerImagePicker
    {
        public const int MaxFileBytes = 2 * 1024 * 1024;

        private const string Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.webp|Все файлы|*.*";

        /// <returns>Байты изображения или null, если диалог отменён.</returns>
        public static byte[]? TryPickImage(out string? errorMessage)
        {
            errorMessage = null;

            var dlg = new OpenFileDialog
            {
                Title = "Выберите фото цветка",
                Filter = Filter,
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog() != true)
                return null;

            try
            {
                var info = new FileInfo(dlg.FileName);
                if (info.Length == 0)
                {
                    errorMessage = "Файл пустой.";
                    return null;
                }

                if (info.Length > MaxFileBytes)
                {
                    errorMessage = "Файл слишком большой (максимум 2 МБ).";
                    return null;
                }

                return File.ReadAllBytes(dlg.FileName);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }
    }
}
