using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace AdoreFlowerShop.Services
{
    /// <summary>Проверка SQL Server LocalDB перед подключением к базе.</summary>
    public static class LocalDbEnvironment
    {
        public const string LocalDbDownloadUrl = "https://go.microsoft.com/fwlink/?LinkID=866658";
        public const string DotNet8DesktopDownloadUrl = "https://dotnet.microsoft.com/download/dotnet/8.0";

        public static string? GetStartupBlockMessage()
        {
            if (!IsDotNet8DesktopRuntimePresent())
            {
                return "Для работы ADORE нужен .NET 8 Desktop Runtime (Windows).\n\n"
                       + $"Скачайте и установите с сайта Microsoft:\n{DotNet8DesktopDownloadUrl}\n\n"
                       + "После установки перезапустите программу.";
            }

            if (!IsLocalDbInstalled())
            {
                return "Для работы ADORE нужен SQL Server Express LocalDB (экземпляр MSSQLLocalDB).\n\n"
                       + $"Скачайте и установите LocalDB:\n{LocalDbDownloadUrl}\n\n"
                       + "После установки перезапустите программу.";
            }

            return null;
        }

        public static bool IsDotNet8DesktopRuntimePresent()
        {
            if (HasDesktopRuntime8InRegistry(RegistryHive.LocalMachine)
                || HasDesktopRuntime8InRegistry(RegistryHive.CurrentUser))
            {
                return true;
            }

            if (HasDesktopRuntime8OnDisk())
                return true;

            // Приложение уже запущено — среда выполнения загружена.
            return Environment.Version.Major >= 8;
        }

        private static bool HasDesktopRuntime8InRegistry(RegistryHive hive)
        {
            const string subKeyPath = @"SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App";

            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(subKeyPath);
                if (key == null)
                    return false;

                foreach (var name in key.GetSubKeyNames())
                {
                    if (name.StartsWith("8.", StringComparison.Ordinal))
                        return true;
                }

                foreach (var name in key.GetValueNames())
                {
                    if (name.StartsWith("8.", StringComparison.Ordinal))
                        return true;
                }
            }
            catch
            {
                // ignore registry access errors
            }

            return false;
        }

        private static bool HasDesktopRuntime8OnDisk()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var desktopRoot = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App");

            if (DirectoryHasVersion8Folder(desktopRoot))
                return true;

            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var desktopRootX86 = Path.Combine(programFilesX86, "dotnet", "shared", "Microsoft.WindowsDesktop.App");

            return DirectoryHasVersion8Folder(desktopRootX86);
        }

        private static bool DirectoryHasVersion8Folder(string desktopRoot)
        {
            if (!Directory.Exists(desktopRoot))
                return false;

            foreach (var dir in Directory.EnumerateDirectories(desktopRoot))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith("8.", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public static bool IsLocalDbInstalled()
        {
            if (IsLocalDbInRegistry())
                return true;

            if (File.Exists(@"C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe"))
                return true;

            if (File.Exists(@"C:\Program Files\Microsoft SQL Server\150\Tools\Binn\SqlLocalDB.exe"))
                return true;

            return TryRunSqllocaldbInfo();
        }

        private static bool IsLocalDbInRegistry()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions");
                if (key == null)
                    return false;

                return key.GetSubKeyNames().Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryRunSqllocaldbInfo()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sqllocaldb",
                    Arguments = "info MSSQLLocalDB",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return false;

                process.WaitForExit(5000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
