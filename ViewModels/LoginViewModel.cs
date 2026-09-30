using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AdoreFlowerShop.Mvvm;
using AdoreFlowerShop.Services;

namespace AdoreFlowerShop.ViewModels
{
    public sealed class LoginViewModel : ViewModelBase
    {
        private readonly Func<string, string, string, Task<string?>>? _tryRegisterAsync;
        private readonly Func<string, string, Task<(bool ok, string displayName, bool isAdmin, int userId)>>? _tryAuthenticateAsync;
        private readonly string? _defaultLoginUsername;
        private readonly string? _defaultLoginPassword;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _phone = "+";
        private bool _isRegisterMode;
        private bool _isBusy;

        public string Username
        {
            get => _username;
            set
            {
                if (!SetProperty(ref _username, value))
                    return;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (!SetProperty(ref _password, value))
                    return;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (!SetProperty(ref _confirmPassword, value))
                    return;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                if (!SetProperty(ref _phone, value))
                    return;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsRegisterMode
        {
            get => _isRegisterMode;
            set
            {
                if (!SetProperty(ref _isRegisterMode, value))
                    return;
                NotifyLoginTexts();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (!SetProperty(ref _isBusy, value))
                    return;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string HeaderText =>
            IsRegisterMode ? T("LoginDialog_TitleRegister") : T("LoginDialog_TitleLogin");

        public string PrimaryHint =>
            IsRegisterMode ? T("LoginDialog_HintRegister") : T("LoginDialog_HintLogin");

        public string PrimaryButtonText =>
            IsRegisterMode ? T("LoginDialog_SubmitRegister") : T("LoginDialog_SubmitLogin");

        public ICommand PrimarySubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ShowRegisterCommand { get; }
        public ICommand ShowLoginCommand { get; }

        public event EventHandler<bool?>? RequestClose;

        /// <summary>После успешной проверки логина — Id пользователя в БД (0 если не входили).</summary>
        public int AuthenticatedUserId { get; private set; }

        /// <summary>Синхронизация паролей из PasswordBox в VM перед проверкой (обходит сбои привязки).</summary>
        public Action? SyncSecretsFromView { get; set; }

        public LoginViewModel(
            Func<string, string, string, Task<string?>>? tryRegisterAsync = null,
            Func<string, string, Task<(bool ok, string displayName, bool isAdmin, int userId)>>? tryAuthenticateAsync = null,
            string? defaultLoginUsername = null,
            string? defaultLoginPassword = null)
        {
            _tryRegisterAsync = tryRegisterAsync;
            _tryAuthenticateAsync = tryAuthenticateAsync;
            _defaultLoginUsername = string.IsNullOrWhiteSpace(defaultLoginUsername) ? null : defaultLoginUsername.Trim();
            _defaultLoginPassword = string.IsNullOrWhiteSpace(defaultLoginPassword) ? null : defaultLoginPassword;

            PrimarySubmitCommand = new RelayCommand(_ => _ = PrimarySubmitAsync(), _ => !IsBusy);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));
            ShowRegisterCommand = new RelayCommand(_ =>
            {
                IsRegisterMode = true;
                Username = string.Empty;
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                Phone = "+";
            }, _ => _tryRegisterAsync != null && !IsBusy);
            ShowLoginCommand = new RelayCommand(_ =>
            {
                IsRegisterMode = false;
                ConfirmPassword = string.Empty;
                Phone = string.Empty;
                ApplyLoginPresetCredentials();
            }, _ => !IsBusy);

            ApplyLoginPresetCredentials();
        }

        private void ApplyLoginPresetCredentials()
        {
            if (_defaultLoginUsername != null)
                Username = _defaultLoginUsername;
            if (_defaultLoginPassword != null)
                Password = _defaultLoginPassword;
        }

        private void NotifyLoginTexts()
        {
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(PrimaryHint));
            OnPropertyChanged(nameof(PrimaryButtonText));
        }

        private static string T(string key) =>
            Application.Current.TryFindResource(key) as string ?? key;

        private static string TitleBox =>
            Application.Current.TryFindResource("AppTitle") as string ?? "ADORE";

        private async Task PrimarySubmitAsync()
        {
            if (IsBusy)
                return;

            SyncSecretsFromView?.Invoke();

            if (IsRegisterMode)
            {
                if (_tryRegisterAsync == null)
                    return;

                var registerErr = InputValidator.ValidateRegistration(Username, Password, ConfirmPassword, Phone);
                if (registerErr != null)
                {
                    MessageBox.Show(registerErr, TitleBox, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await RegisterAsync();
            }
            else
            {
                var loginErr = InputValidator.ValidateLogin(Username, Password);
                if (loginErr != null)
                {
                    MessageBox.Show(loginErr, TitleBox, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await LoginAsync();
            }
        }

        private async Task LoginAsync()
        {
            if (IsBusy)
                return;

            AuthenticatedUserId = 0;

            if (_tryAuthenticateAsync == null)
            {
                RequestClose?.Invoke(this, true);
                return;
            }

            var userName = Username?.Trim() ?? string.Empty;

            // UX: allow quick admin login by typing only "admin" (password defaults to "admin").
            if (string.Equals(userName, "admin", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(Password))
            {
                Password = "admin";
            }

            IsBusy = true;
            try
            {
                var r = await _tryAuthenticateAsync(userName, Password);
                if (!r.ok)
                {
                    MessageBox.Show(T("LoginDialog_InvalidCredentials"), TitleBox, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                AuthenticatedUserId = r.userId;

                var msg = r.isAdmin
                    ? T("LoginDialog_SuccessAdmin")
                    : string.Format(T("LoginDialog_SuccessLogin"), r.displayName, T("LoginDialog_RoleClient"));
                MessageBox.Show(msg, TitleBox, MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, TitleBox, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RegisterAsync()
        {
            if (_tryRegisterAsync == null)
                return;

            IsBusy = true;
            try
            {
                var err = await _tryRegisterAsync(Username.Trim(), Password, Phone);
                if (!string.IsNullOrEmpty(err))
                {
                    MessageBox.Show(err, TitleBox, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show(T("LoginDialog_SuccessRegister"), TitleBox, MessageBoxButton.OK, MessageBoxImage.Information);
                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, TitleBox, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
