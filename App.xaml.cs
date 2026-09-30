using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AdoreFlowerShop.Data;
using AdoreFlowerShop.Services;
using AdoreFlowerShop.ViewModels;
using AdoreFlowerShop.Views;

namespace AdoreFlowerShop
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            RegisterAppWindowIcon();
            base.OnStartup(e);

            var prerequisiteMessage = LocalDbEnvironment.GetStartupBlockMessage();
            if (prerequisiteMessage != null)
            {
                MessageBox.Show(prerequisiteMessage, "Требования для установки ADORE",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown(-1);
                return;
            }

            try
            {
                await DbInitializer.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось инициализировать базу данных: {ex.Message}", "Ошибка БД",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
                return;
            }

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginVm = new LoginViewModel(
                MainWindowViewModel.TryRegisterNewClientAsync,
                MainWindowViewModel.TryAuthenticateAsync);

            var loginWindow = new LoginWindow(loginVm)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            if (loginWindow.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var main = new MainWindow();
            MainWindow = main;
            main.Show();

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            await main.ViewModel.InitializeWithUserAsync(loginVm.Username, loginVm.Password);
        }

        private static void RegisterAppWindowIcon()
        {
            var icon = BitmapFrame.Create(
                new Uri("pack://application:,,,/Resources/AppIcon.ico", UriKind.Absolute));

            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    if (sender is Window window && window.Icon == null)
                        window.Icon = icon;
                }),
                true);
        }
    }
}