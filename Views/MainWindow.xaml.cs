using System;
using System.Windows;
using AdoreFlowerShop.ViewModels;

namespace AdoreFlowerShop.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;

        public MainWindowViewModel ViewModel => _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainWindowViewModel();
            DataContext = _vm;
        }
    }
}