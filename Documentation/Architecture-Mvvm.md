## MVVM в этом проекте (кратко)

### Что такое MVVM
- **Model**: данные и бизнес‑сущности (в проекте это типы вроде `Flower`, `User`, `DataModel` в `Models.cs`, плюс доступ к БД через `Data/*`).
- **View**: XAML + минимальный `code-behind` только для чисто UI‑событий (маршрутизация событий, открытие окон и т.п.).
- **ViewModel**: состояние экрана, команды (`ICommand`), фильтрация/загрузка данных, уведомления UI через `INotifyPropertyChanged`.

### Принцип построения
- **View не содержит бизнес‑логики**: в идеале только разметка и биндинги.
- **ViewModel не знает про конкретные контролы**, но может вызывать сервисы/репозитории/UnitOfWork и показывать диалоги (в учебных проектах это допустимо; в “боевом” коде обычно выносят в интерфейсы).
- **Связь View ↔ ViewModel**: `DataContext` + `{Binding ...}` + команды.

### Где это лежит в репозитории
- **Инфраструктура MVVM**: `Mvvm/ViewModelBase.cs`, `Mvvm/RelayCommand.cs`, `Mvvm/PasswordBoxAssistant.cs`
- **Главный экран**:
  - View: `MainWindow.xaml`
  - ViewModel: `ViewModels/MainWindowViewModel.cs`
  - View code-behind: `MainWindow.xaml.cs` (оставлены обработчики демо routed events)
- **Окно входа**:
  - View: `LoginWindow.xaml`
  - ViewModel: `ViewModels/LoginViewModel.cs`
  - View code-behind: `LoginWindow.xaml.cs` (закрытие диалога по событию из VM)

### Примечание про `PasswordBox`
WPF не поддерживает безопасный `Password` binding “из коробки”, поэтому используется **attached property** `PasswordBoxAssistant.BoundPassword` для MVVM без логики в code-behind.
