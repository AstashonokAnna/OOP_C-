using System.Linq;
using System.Windows;
using System.Windows.Input;
using AdoreFlowerShop.Services;
using AdoreFlowerShop.ViewModels;

namespace AdoreFlowerShop.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.SyncSecretsFromView = () =>
            {
                viewModel.Password = PwdBox.Password;
                viewModel.ConfirmPassword = PwdConfirmBox.Password;
            };
            viewModel.RequestClose += (_, result) =>
            {
                DialogResult = result;
                Close();
            };

            UsernameBox.PreviewTextInput += UsernameBox_PreviewTextInput;
            DataObject.AddPastingHandler(UsernameBox, UsernameBox_Pasting);
        }

        private void UsernameBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Length > 0 && e.Text.Any(c => !InputValidator.IsUsernameChar(c));
        }

        private void UsernameBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
                return;

            var pasted = e.DataObject.GetData(typeof(string)) as string;
            if (pasted != null && pasted.Any(c => !InputValidator.IsUsernameChar(c)))
                e.CancelCommand();
        }

        public LoginWindow() : this(new LoginViewModel())
        {
        }
    }
}
