using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AdoreFlowerShop.Data;
using AdoreFlowerShop.Data.UnitOfWork;
using AdoreFlowerShop.Services;
using AdoreFlowerShop.ViewModels;

namespace AdoreFlowerShop.Views
{
    public partial class ProfileWindow : Window, INotifyPropertyChanged
    {
        private readonly MainWindowViewModel _main;
        private string _userName = string.Empty;
        private string _userPhone = string.Empty;
        private bool _isLightTheme;
        private bool _isDarkTheme;
        private bool _isAdmin;
        private string _statusMessage = string.Empty;
        private ObservableCollection<OrderTableRow> _orders = new();
        private ObservableCollection<string> _registeredOrders = new();

        public string UserName { get => _userName; set { _userName = value; OnPropertyChanged(); } }
        public string UserPhone { get => _userPhone; set { _userPhone = value; OnPropertyChanged(); } }
        public bool IsLightTheme { get => _isLightTheme; set { _isLightTheme = value; OnPropertyChanged(); } }
        public bool IsDarkTheme { get => _isDarkTheme; set { _isDarkTheme = value; OnPropertyChanged(); } }
        public bool IsAdmin { get => _isAdmin; set { _isAdmin = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public ObservableCollection<OrderTableRow> Orders { get => _orders; set { _orders = value; OnPropertyChanged(); } }
        public ObservableCollection<string> RegisteredOrders { get => _registeredOrders; set { _registeredOrders = value; OnPropertyChanged(); } }
        public bool HasRegisteredNotice => RegisteredOrders.Count > 0;
        public string RegisteredNoticeTitle => OrderNoticeTexts.RegisteredTitle;
        public string RegisteredNoticeDescription => OrderNoticeTexts.RegisteredDescription;

        public ProfileWindow(MainWindowViewModel mainWindowViewModel)
        {
            _main = mainWindowViewModel;
            InitializeComponent();
            DataContext = this;
            LoadCurrentSettings();
            _ = LoadOrdersAsync();
        }

        private void LoadCurrentSettings()
        {
            UserName = _main.CurrentUser?.Name ?? "client";
            var phone = _main.CurrentUser?.Phone;
            UserPhone = string.IsNullOrWhiteSpace(phone) ? "—" : "+" + phone;
            IsAdmin = _main.IsAdmin;

            string currentTheme = _main.GetCurrentTheme();
            IsLightTheme = currentTheme == "Light";
            IsDarkTheme = currentTheme == "Dark";
        }

        private async System.Threading.Tasks.Task LoadOrdersAsync()
        {
            if (_main.CurrentUser == null || _main.IsAdmin)
                return;

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                var orders = await uow.Orders.GetOrdersByUserIdAsync(_main.CurrentUser.Id);
                Orders = new ObservableCollection<OrderTableRow>(orders);
                UpdateRegisteredNotices(orders);
            }
            catch
            {
                Orders.Clear();
                UpdateRegisteredNotices(Array.Empty<OrderTableRow>());
            }
        }

        private void UpdateRegisteredNotices(IEnumerable<OrderTableRow> orders)
        {
            var lines = orders
                .Where(o => o.IsRegisteredNotice)
                .Select(o => $"Заказ №{o.Id}: {o.FlowerName} — {o.TotalPrice:F2} $, доставка {o.DeliveryDate ?? "—"}")
                .ToList();

            RegisteredOrders = new ObservableCollection<string>(lines);
            OnPropertyChanged(nameof(HasRegisteredNotice));
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (IsLightTheme) _main.SetTheme("Light");
            else if (IsDarkTheme) _main.SetTheme("Dark");

            if (!string.IsNullOrEmpty(NewPasswordBox.Password))
            {
                var pwdErr = InputValidator.ValidatePasswordChange(
                    NewPasswordBox.Password,
                    ConfirmPasswordBox.Password);
                if (pwdErr != null)
                {
                    StatusMessage = pwdErr;
                    MessageBox.Show(pwdErr, "Профиль", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_main.CurrentUser != null)
                {
                    _main.CurrentUser.Password = NewPasswordBox.Password;
                    try
                    {
                        await using var uow = FlowerShopUnitOfWork.Create();
                        await uow.Users.UpdatePasswordAsync(
                            _main.CurrentUser.Name ?? "",
                            _main.CurrentUser.Password ?? "");
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = ex.Message;
                        MessageBox.Show(ex.Message, "Профиль", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    StatusMessage = "Пароль успешно изменён.";
                    NewPasswordBox.Password = "";
                    ConfirmPasswordBox.Password = "";
                }
            }

            if (!string.IsNullOrEmpty(StatusMessage))
                MessageBox.Show(StatusMessage, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
