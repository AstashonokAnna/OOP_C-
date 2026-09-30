using FlowerShop;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AdoreFlowerShop.Data;
using AdoreFlowerShop.Data.UnitOfWork;
using AdoreFlowerShop.Services;

namespace AdoreFlowerShop.Views
{
    public partial class AdminDatabaseWindow : Window, INotifyPropertyChanged
    {
        private ObservableCollection<Flower> _flowers = new ObservableCollection<Flower>();
        private ObservableCollection<User> _users = new ObservableCollection<User>();
        private ObservableCollection<OrderTableRow> _orders = new ObservableCollection<OrderTableRow>();
        private Flower? _selectedFlower;

        public ObservableCollection<Flower> Flowers { get => _flowers; set { _flowers = value; OnPropertyChanged(); } }
        public ObservableCollection<User> Users { get => _users; set { _users = value; OnPropertyChanged(); } }
        public ObservableCollection<OrderTableRow> Orders { get => _orders; set { _orders = value; OnPropertyChanged(); } }
        public Flower? SelectedFlower
        {
            get => _selectedFlower;
            set
            {
                _selectedFlower = value;
                OnPropertyChanged();
                UpdatePickPhotoButton();
            }
        }

        public string DbPathInfo => $"Сервер: (LocalDB)\\MSSQLLocalDB  ·  БД: FlowerShopDb";

        public AdminDatabaseWindow()
        {
            InitializeComponent();
            DataContext = this;
            UpdatePickPhotoButton();
            Loaded += async (_, __) =>
            {
                try
                {
                    await RefreshAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Не удалось загрузить данные: {ex.Message}",
                        "Администрирование БД",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            };
        }

        private async Task RefreshAsync()
        {
            var errors = new System.Text.StringBuilder();

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                var flowers = await uow.Flowers.GetAllAsync();
                Flowers = new ObservableCollection<Flower>(flowers.OrderBy(f => f.Id));
            }
            catch (Exception ex)
            {
                Flowers = new ObservableCollection<Flower>();
                errors.AppendLine($"Цветы: {ex.Message}");
            }

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                Users = new ObservableCollection<User>(await uow.Users.GetAllAsync());
            }
            catch (Exception ex)
            {
                Users = new ObservableCollection<User>();
                errors.AppendLine($"Пользователи: {ex.Message}");
            }

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                Orders = new ObservableCollection<OrderTableRow>(await uow.Orders.GetAllOrderRowsAsync());
            }
            catch (Exception ex)
            {
                Orders = new ObservableCollection<OrderTableRow>();
                errors.AppendLine($"Заказы: {ex.Message}");
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(
                    "Не удалось загрузить часть данных:" + Environment.NewLine + errors,
                    "Администрирование БД",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try { await RefreshAsync(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void AddFlower_Click(object sender, RoutedEventArgs e)
        {
            var f = new Flower
            {
                Id = 0,
                ShortName = "Новый цветок",
                Category = "Цветы",
                Price = 0,
                Quantity = 0,
                Discount = 0,
                Rating = 0,
                InStock = false
            };
            Flowers.Add(f);
            SelectedFlower = f;
            UpdatePickPhotoButton();
        }

        private void PickFlowerPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFlower == null)
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

        private void UpdatePickPhotoButton()
        {
            if (PickPhotoButton != null)
                PickPhotoButton.IsEnabled = SelectedFlower != null;
        }

        private async void SaveFlower_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFlower == null)
                return;

            FlowersDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            FlowersDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var flowerErr = InputValidator.ValidateFlower(SelectedFlower);
            if (flowerErr != null)
            {
                MessageBox.Show(flowerErr, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int newId;
                await using (var uow = FlowerShopUnitOfWork.Create())
                {
                    if (SelectedFlower.Id == 0)
                        newId = await uow.Flowers.InsertAsync(SelectedFlower, SelectedFlower.MainImageBytes);
                    else
                    {
                        await uow.Flowers.UpdateAsync(SelectedFlower, SelectedFlower.MainImageBytes);
                        newId = SelectedFlower.Id;
                    }
                }

                await RefreshAsync();
                SelectedFlower = Flowers.FirstOrDefault(x => x.Id == newId);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteFlower_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFlower == null)
                return;

            if (MessageBox.Show($"Удалить '{SelectedFlower.ShortName}'?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (SelectedFlower.Id != 0)
                {
                    await using var uow = FlowerShopUnitOfWork.Create();
                    await uow.Flowers.DeleteAsync(SelectedFlower.Id);
                }

                await RefreshAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MarkOrderRegistered_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrdersDataGrid.SelectedItem as OrderTableRow;
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (selected.IsRegisteredNotice)
            {
                MessageBox.Show(
                    "Уведомление для этого заказа уже отправлено клиенту.",
                    OrderNoticeTexts.RegisteredTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                await uow.Orders.SetRegisteredNoticeAsync(selected.Id, isRegisteredNotice: true);
                await RefreshAsync();
                MessageBox.Show(
                    "Клиент увидит объявление «Заказ зарегистрирован» в личном кабинете.",
                    OrderNoticeTexts.RegisteredTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveOrderStatus_Click(object sender, RoutedEventArgs e)
        {
            OrdersDataGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            OrdersDataGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var selected = OrdersDataGrid.SelectedItem as OrderTableRow;
            if (selected == null)
            {
                MessageBox.Show("Выберите заказ для изменения статуса.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                await uow.Orders.UpdateStatusAsync(selected.Id, selected.Status);
                await RefreshAsync();
                MessageBox.Show("Статус заказа сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

