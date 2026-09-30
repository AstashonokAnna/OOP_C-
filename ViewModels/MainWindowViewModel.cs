using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FlowerShop;
using AdoreFlowerShop.Data.UnitOfWork;
using AdoreFlowerShop.Views;
using AdoreFlowerShop.Mvvm;
using AdoreFlowerShop.Services;
namespace AdoreFlowerShop.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private DataModel _data = new();
        private User? _currentUser;
        private Flower? _selectedFlower;
        private ObservableCollection<Flower> _filteredFlowers = new();
        private ObservableCollection<string> _categories = new();
        private bool _isEditMode;
        private string _searchText = string.Empty;
        private string _selectedCategory = "Все";
        private string _currentTheme = "Light";
        private int _orderQuantity = 1;
        private DateTime _deliveryDate = DateTime.Today.AddDays(1);
        private string _deliveryTime = "12:00";
        private string _address = string.Empty;

        private ObservableCollection<Review> _reviews = new();


        public DataModel Data
        {
            get => _data;
            private set => SetProperty(ref _data, value);
        }

        public User? CurrentUser => _currentUser;
        public ObservableCollection<Flower> FilteredFlowers
        {
            get => _filteredFlowers;
            private set => SetProperty(ref _filteredFlowers, value);
        }

        public ObservableCollection<string> Categories
        {
            get => _categories;
            private set => SetProperty(ref _categories, value);
        }

        public Flower? SelectedFlower
        {
            get => _selectedFlower;
            set
            {
                if (_selectedFlower == value)
                    return;

                DetachSelectedFlowerEvents();

                if (_isEditMode && IsAdmin)
                {
                    var result = MessageBox.Show("Сохранить изменения?", "Вопрос", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes)
                    {
                        IsEditMode = false;
                        Save(null);
                    }
                    else if (result == MessageBoxResult.Cancel) return;
                }

                _selectedFlower = value;
                AttachSelectedFlowerEvents();

                IsEditMode = false;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanPickFlowerPhoto));

                var maxQ = GetMaxOrderQuantityForSelectedFlower();
                OrderQuantity = maxQ > 0 ? 1 : 0;

                RaiseOrderComputedChanged();
                CommandManager.InvalidateRequerySuggested();

                _ = LoadReviewsAsync();
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode && IsAdmin;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(IsEditMode));
                    OnPropertyChanged(nameof(CanPickFlowerPhoto));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    Filter();
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    Filter();
            }
        }

        public bool IsAdmin => _currentUser?.Role == UserRole.Admin;
        public bool IsLoggedIn => _currentUser != null;

        public ObservableCollection<Review> Reviews
        {
            get => _reviews;
            private set => SetProperty(ref _reviews, value);
        }

        public int OrderQuantity
        {
            get => _orderQuantity;
            set
            {
                var maxAllowed = GetMaxOrderQuantityForSelectedFlower();
                var newValue = value;

                if (maxAllowed <= 0)
                    newValue = 0;
                else
                {
                    if (newValue < 1) newValue = 1;
                    if (newValue > 200000) newValue = 200000;
                    if (newValue > maxAllowed)
                        newValue = maxAllowed;
                }

                if (!SetProperty(ref _orderQuantity, newValue))
                    return;

                RaiseOrderComputedChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public int OrderMaxQuantity => GetMaxOrderQuantityForSelectedFlower();
        public string OrderMaxText => $"Max: {OrderMaxQuantity}";
        public int OrderSliderMinimum => OrderMaxQuantity > 0 ? 1 : 0;
        public int OrderSliderMaximum => OrderMaxQuantity;
        public decimal OrderUnitPriceUsd => GetUnitPriceUsdForSelectedFlower();
        public decimal OrderTotalUsd => OrderUnitPriceUsd * OrderQuantity;

        public DateTime MinDeliveryDate => DateTime.Today;

        public DateTime DeliveryDate
        {
            get => _deliveryDate;
            set
            {
                var normalized = value.Date;
                if (normalized < DateTime.Today)
                    normalized = DateTime.Today;

                if (SetProperty(ref _deliveryDate, normalized))
                    OnPropertyChanged(nameof(DeliveryDateFormatted));
            }
        }

        public string DeliveryTime
        {
            get => _deliveryTime;
            set
            {
                if (SetProperty(ref _deliveryTime, value))
                    OnPropertyChanged(nameof(DeliveryDateFormatted));
            }
        }

        public string DeliveryDateFormatted => $"{_deliveryDate:yyyy-MM-dd} {_deliveryTime}";

        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        public ObservableCollection<string> TimeSlots { get; } = new ObservableCollection<string>
        {
            "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00"
        };

        private void RaiseOrderComputedChanged()
        {
            OnPropertyChanged(nameof(OrderUnitPriceUsd));
            OnPropertyChanged(nameof(OrderMaxQuantity));
            OnPropertyChanged(nameof(OrderMaxText));
            OnPropertyChanged(nameof(OrderSliderMinimum));
            OnPropertyChanged(nameof(OrderSliderMaximum));
            OnPropertyChanged(nameof(OrderTotalUsd));
        }

        private int GetMaxOrderQuantityForSelectedFlower()
        {
            if (SelectedFlower == null)
                return 1;

            if (!SelectedFlower.InStock || SelectedFlower.Quantity <= 0)
                return 0;

            var perFlowerMax = SelectedFlower.ShortName?.ToLowerInvariant() switch
            {
                var s when s != null && s.Contains("роза") => 100000,
                var s when s != null && s.Contains("тюльпан") => 200000,
                var s when s != null && s.Contains("лилия") => 50000,
                _ => 200000
            };

            if (perFlowerMax < 1)
                perFlowerMax = 1;

            var maxByStock = SelectedFlower.Quantity;

            return Math.Min(perFlowerMax, maxByStock);
        }

        private decimal GetUnitPriceUsdForSelectedFlower()
        {
            if (SelectedFlower == null)
                return 0m;

            return (decimal)Math.Max(0, SelectedFlower.PriceWithDiscount);
        }

        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ProfileCommand { get; }
        public ICommand SaveOrderCommand { get; }
        public ICommand OpenAdminDbCommand { get; }

        public ICommand AddReviewCommand { get; }
        public ICommand PickFlowerPhotoCommand { get; }

        public bool CanPickFlowerPhoto =>
            IsAdmin && IsEditMode && SelectedFlower != null;

        public MainWindowViewModel()
        {
            LoginCommand = new RelayCommand(Login);
            LogoutCommand = new RelayCommand(Logout, _ => _currentUser != null);
            ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());

            AddCommand = new RelayCommand(Add, _ => IsAdmin);
            EditCommand = new RelayCommand(Edit, _ => IsAdmin && SelectedFlower != null && !IsEditMode);
            DeleteCommand = new RelayCommand(Delete, _ => IsAdmin && SelectedFlower != null);
            SaveCommand = new RelayCommand(Save, _ => IsAdmin && SelectedFlower != null && IsEditMode);
            CancelCommand = new RelayCommand(Cancel, _ => IsEditMode);

            ProfileCommand = new RelayCommand(Profile, _ => _currentUser != null);
            OpenAdminDbCommand = new RelayCommand(OpenAdminDb, _ => IsAdmin);

            SaveOrderCommand = new RelayCommand(_ => SaveOrder(), _ => CanSaveOrder());
            AddReviewCommand = new RelayCommand(_ => AddReview(), _ => CanAddReview());
            PickFlowerPhotoCommand = new RelayCommand(PickFlowerPhoto, _ => CanPickFlowerPhoto);
        }

        public async Task InitializeAsync()
        {
            await ReloadFromDbAsync();
        }

        public async Task InitializeWithUserAsync(string username, string password)
        {
            await ReloadFromDbAsync(resetSessionUserToDefault: false);

            var user = Data.Users?.FirstOrDefault(u =>
                string.Equals(u.Name?.Trim(), username.Trim(), StringComparison.OrdinalIgnoreCase)
                && u.Password == password);

            if (user != null)
            {
                SetCurrentUser(user);
                OnPropertyChanged(nameof(IsEditMode));
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                await ReloadFromDbAsync(resetSessionUserToDefault: true);
            }
        }

        private void AttachSelectedFlowerEvents()
        {
            if (_selectedFlower != null)
                _selectedFlower.PropertyChanged += SelectedFlower_PropertyChanged;
        }

        private void DetachSelectedFlowerEvents()
        {
            if (_selectedFlower != null)
                _selectedFlower.PropertyChanged -= SelectedFlower_PropertyChanged;
        }

        private void SelectedFlower_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not Flower flower)
                return;

            if (e.PropertyName == nameof(Flower.Discount) && flower.Discount == 100)
            {
                MessageBox.Show("Внимание: скидка 100% (MAX)!", "Оповещение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (e.PropertyName == nameof(Flower.Quantity) || e.PropertyName == nameof(Flower.InStock))
            {
                RaiseOrderComputedChanged();
                var max = GetMaxOrderQuantityForSelectedFlower();
                if (max <= 0)
                    OrderQuantity = 0;
                else if (_orderQuantity > max)
                    OrderQuantity = max;
                else if (_orderQuantity < 1)
                    OrderQuantity = 1;

                CommandManager.InvalidateRequerySuggested();
            }
        }

        /// <param name="resetSessionUserToDefault">
        /// Если true — после загрузки выставляется первый клиент (как при старте). Если false — текущий пользователь не меняется.
        /// </param>
        private async Task ReloadFromDbAsync(int? selectFlowerId = null, bool resetSessionUserToDefault = false)
        {
            await using var uow = FlowerShopUnitOfWork.Create();
            var flowers = await uow.Flowers.GetAllAsync();
            var users = await uow.Users.GetAllAsync();

            Data = new DataModel
            {
                Flowers = new ObservableCollection<Flower>(flowers),
                Users = new ObservableCollection<User>(users)
            };

            FilteredFlowers = new ObservableCollection<Flower>(Data.Flowers);
            UpdateCategories();

            if (resetSessionUserToDefault)
                SetCurrentUser(Data.Users?.FirstOrDefault(u => u.Role == UserRole.Client));

            OnPropertyChanged(nameof(IsEditMode));
            CommandManager.InvalidateRequerySuggested();

            if (selectFlowerId != null)
                SelectedFlower = Data.Flowers.FirstOrDefault(f => f.Id == selectFlowerId.Value);
        }

        private void UpdateCategories()
        {
            var cats = new ObservableCollection<string>(
                Data.Flowers
                    .Select(f => f.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c!)
                    .Distinct());
            cats.Insert(0, "Все");
            Categories = cats;
        }

        private void Filter()
        {
            var query = Data.Flowers.AsEnumerable();
            if (!string.IsNullOrEmpty(SearchText))
                query = query.Where(f => f.ShortName != null && f.ShortName.ToLower().Contains(SearchText.ToLower()));
            if (SelectedCategory != "Все" && !string.IsNullOrEmpty(SelectedCategory))
                query = query.Where(f => f.Category == SelectedCategory);

            FilteredFlowers.Clear();
            foreach (var f in query)
                FilteredFlowers.Add(f);
        }

        private async void Login(object? _)
        {
            var vm = new LoginViewModel(
                TryRegisterNewClientAsync,
                TryAuthenticateAsync,
                defaultLoginUsername: "",
                defaultLoginPassword: "");
            var dialog = new LoginWindow(vm) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() != true)
                return;

            await ReloadFromDbAsync(resetSessionUserToDefault: false);

            User? user = null;
            if (vm.AuthenticatedUserId > 0)
                user = Data.Users?.FirstOrDefault(u => u.Id == vm.AuthenticatedUserId);

            if (user == null)
            {
                user = Data.Users?.FirstOrDefault(u =>
                    string.Equals(u.Name?.Trim(), vm.Username.Trim(), StringComparison.OrdinalIgnoreCase)
                    && u.Password == vm.Password);
            }

            if (user != null)
            {
                SetCurrentUser(user);
                OnPropertyChanged(nameof(IsEditMode));
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                await ReloadFromDbAsync(resetSessionUserToDefault: true);
                MessageBox.Show(
                    "Не удалось применить учётную запись после входа. Попробуйте ещё раз.",
                    "ADORE",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        internal static async Task<(bool ok, string displayName, bool isAdmin, int userId)> TryAuthenticateAsync(string name, string password)
        {
            await using var uow = FlowerShopUnitOfWork.Create();
            var users = await uow.Users.GetAllAsync();
            var user = users.FirstOrDefault(u =>
                string.Equals(u.Name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)
                && u.Password == password);
            return user == null
                ? (false, string.Empty, false, 0)
                : (true, user.Name ?? string.Empty, user.Role == UserRole.Admin, user.Id);
        }

        internal static async Task<string?> TryRegisterNewClientAsync(string name, string password, string phone)
        {
            await using var uow = FlowerShopUnitOfWork.Create();
            var err = await uow.Users.TryInsertClientAsync(name, password, phone);
            if (err == null)
                RegistrationJournal.Append(name.Trim(), phone);
            return err;
        }

        private void Logout(object? _)
        {
            SetCurrentUser(null);
            SelectedFlower = null;
            IsEditMode = false;
            OnPropertyChanged(nameof(IsEditMode));
            CommandManager.InvalidateRequerySuggested();
            MessageBox.Show("Вы вышли из системы");
        }

        private void Add(object? _)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("У вас нет прав для добавления товаров!");
                return;
            }

            var newFlower = new Flower
            {
                Id = 0,
                ShortName = "Новый цветок",
                FullName = "Новый цветок",
                Category = "Цветы",
            };

            Data.Flowers.Add(newFlower);
            SelectedFlower = newFlower;
            IsEditMode = true;
            Filter();
            UpdateCategories();
        }

        private void Edit(object? _)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("У вас нет прав для редактирования товаров!");
                return;
            }

            if (SelectedFlower != null)
                IsEditMode = true;
        }

        private async void Delete(object? _)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("У вас нет прав для удаления товаров!");
                return;
            }

            if (SelectedFlower == null)
                return;

            if (_isEditMode && IsAdmin)
            {
                var editPrompt = MessageBox.Show("Сохранить изменения перед удалением?", "Вопрос", MessageBoxButton.YesNoCancel);
                if (editPrompt == MessageBoxResult.Cancel) return;
                IsEditMode = false;
                if (editPrompt == MessageBoxResult.Yes) Save(null);
            }

            if (MessageBox.Show($"Удалить '{SelectedFlower.ShortName}'?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            var flower = SelectedFlower;
            try
            {
                await using (var uow = FlowerShopUnitOfWork.Create())
                    await uow.Flowers.DeleteAsync(flower.Id);
                await ReloadFromDbAsync();
                MessageBox.Show("Запись удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось удалить: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Save(object? _)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("У вас нет прав для сохранения изменений!");
                return;
            }

            if (SelectedFlower == null || !IsEditMode)
                return;

            var flowerErr = InputValidator.ValidateFlower(SelectedFlower);
            if (flowerErr != null)
            {
                MessageBox.Show(flowerErr, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int? selectId;
                await using (var uow = FlowerShopUnitOfWork.Create())
                {
                    if (SelectedFlower.Id == 0)
                        selectId = await uow.Flowers.InsertAsync(SelectedFlower, SelectedFlower.MainImageBytes);
                    else
                    {
                        await uow.Flowers.UpdateAsync(SelectedFlower, newMainImageBytesOrNullToKeep: SelectedFlower.MainImageBytes);
                        selectId = SelectedFlower.Id;
                    }
                }

                await ReloadFromDbAsync(selectFlowerId: selectId);

                IsEditMode = false;
                Filter();
                MessageBox.Show("Изменения сохранены!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения в БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Cancel(object? _)
        {
            if (SelectedFlower != null && IsAdmin)
                await ReloadFromDbAsync(selectFlowerId: SelectedFlower.Id);
            IsEditMode = false;
        }

        private void Profile(object? _)
        {
            if (_currentUser != null)
            {
                if (Application.Current.MainWindow is not MainWindow main)
                    return;

                var profileWindow = new ProfileWindow(main.ViewModel);
                profileWindow.Owner = Application.Current.MainWindow;
                profileWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Войдите в систему для доступа к личному кабинету!");
            }
        }

        private async void OpenAdminDb(object? _)
        {
            if (!IsAdmin)
                return;

            var w = new AdminDatabaseWindow();
            w.Owner = Application.Current.MainWindow;
            w.ShowDialog();
            await ReloadFromDbAsync();
        }

        public void SetTheme(string themeName)
        {
            try
            {
                _currentTheme = themeName;

                static void SetBrush(string key, Color color)
                {
                    if (Application.Current.Resources[key] is SolidColorBrush brush)
                    {
                        if (!brush.IsFrozen)
                        {
                            brush.Color = color;
                            return;
                        }
                    }

                    Application.Current.Resources[key] = new SolidColorBrush(color);
                }

                switch (themeName)
                {
                    case "Dark":
                        SetBrush("PrimaryBrush", Color.FromRgb(33, 150, 243));
                        SetBrush("PrimaryDarkBrush", Color.FromRgb(25, 118, 210));
                        SetBrush("BackgroundBrush", Color.FromRgb(224, 224, 224));
                        SetBrush("SurfaceBrush", Color.FromRgb(245, 245, 245));
                        SetBrush("CardBackgroundBrush", Colors.White);
                        SetBrush("MenuBackgroundBrush", Color.FromRgb(238, 238, 238));
                        SetBrush("ToolBarBackgroundBrush", Color.FromRgb(229, 229, 229));
                        SetBrush("BorderBrush", Color.FromRgb(189, 189, 189));
                        SetBrush("TextBrush", Color.FromRgb(33, 33, 33));
                        SetBrush("TextSecondaryBrush", Color.FromRgb(97, 97, 97));
                        break;
                    default:
                        SetBrush("PrimaryBrush", Color.FromRgb(183, 110, 121));
                        SetBrush("PrimaryDarkBrush", Color.FromRgb(142, 74, 85));
                        SetBrush("BackgroundBrush", Color.FromRgb(255, 252, 250));
                        SetBrush("SurfaceBrush", Color.FromRgb(255, 245, 247));
                        SetBrush("CardBackgroundBrush", Colors.White);
                        SetBrush("MenuBackgroundBrush", Color.FromRgb(255, 240, 243));
                        SetBrush("ToolBarBackgroundBrush", Color.FromRgb(245, 230, 234));
                        SetBrush("BorderBrush", Color.FromRgb(232, 213, 220));
                        SetBrush("TextBrush", Color.FromRgb(45, 42, 50));
                        SetBrush("TextSecondaryBrush", Color.FromRgb(110, 101, 112));
                        break;
                }

                MessageBox.Show($"Тема изменена на {themeName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка смены темы: {ex.Message}");
            }
        }

        public string GetCurrentTheme() => _currentTheme;

        private void SetCurrentUser(User? user)
        {
            if (ReferenceEquals(_currentUser, user))
                return;

            _currentUser = user;
            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(CanPickFlowerPhoto));
            CommandManager.InvalidateRequerySuggested();
        }

        private void PickFlowerPhoto(object? _)
        {
            if (!CanPickFlowerPhoto || SelectedFlower == null)
                return;

            var bytes = FlowerImagePicker.TryPickImage(out var err);
            if (err != null)
            {
                MessageBox.Show(err, "Фото", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (bytes == null)
                return;

            SelectedFlower.MainImageBytes = bytes;
        }

        private async Task LoadReviewsAsync()
        {
            if (SelectedFlower == null)
            {
                Reviews.Clear();
                return;
            }

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                var reviews = await uow.Reviews.GetByFlowerIdAsync(SelectedFlower.Id);
                Reviews = new ObservableCollection<Review>(reviews);
            }
            catch
            {
                Reviews.Clear();
            }
        }

        private bool CanAddReview()
        {
            if (_currentUser == null)
                return false;
            if (IsAdmin)
                return false;
            if (SelectedFlower == null)
                return false;
            return true;
        }

        private async void AddReview()
        {
            if (!CanAddReview())
                return;

            var dialog = new ReviewDialogWindow(SelectedFlower!.Id, _currentUser!.Id)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
                await ReloadFromDbAsync(selectFlowerId: SelectedFlower?.Id);
        }

        private bool CanSaveOrder()
        {
            if (_currentUser == null)
                return false;
            if (IsAdmin)
                return false;
            if (SelectedFlower == null)
                return false;
            if (OrderMaxQuantity <= 0)
                return false;
            if (OrderQuantity < 1)
                return false;
            if (OrderQuantity > OrderMaxQuantity)
                return false;
            if (InputValidator.ValidateAddress(_address) != null)
                return false;
            if (InputValidator.ValidateDeliveryDate(_deliveryDate) != null)
                return false;
            return true;
        }

        private async void SaveOrder()
        {
            if (!CanSaveOrder())
                return;

            var dateErr = InputValidator.ValidateDeliveryDate(_deliveryDate);
            if (dateErr != null)
            {
                MessageBox.Show(dateErr, "Проверка заказа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var flower = SelectedFlower!;
            var qty = OrderQuantity;

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                var unitPrice = Math.Round(flower.PriceWithDiscount, 2, MidpointRounding.AwayFromZero);
                await uow.Orders.InsertAsync(_currentUser!.Id, flower.Id, qty, unitPrice, DeliveryDateFormatted, Address);
                await ReloadFromDbAsync(selectFlowerId: flower.Id);

                MessageBox.Show("Заказ сохранён.", "Оповещение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("trigger", StringComparison.OrdinalIgnoreCase)
                    && msg.Contains("OUTPUT", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "Не удалось сохранить заказ: конфликт с триггером базы данных. Перезапустите приложение после обновления.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show($"Не удалось сохранить заказ: {msg}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

