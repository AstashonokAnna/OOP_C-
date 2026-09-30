using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AdoreFlowerShop.Mvvm
{
    // Позволяет привязывать PasswordBox.Password через attached property (для MVVM).
    public static class PasswordBoxAssistant
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxAssistant),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject obj) => (string)obj.GetValue(BoundPasswordProperty);
        public static void SetBoundPassword(DependencyObject obj, string value) => obj.SetValue(BoundPasswordProperty, value);

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));

        private static bool GetIsUpdating(DependencyObject obj) => (bool)obj.GetValue(IsUpdatingProperty);
        private static void SetIsUpdating(DependencyObject obj, bool value) => obj.SetValue(IsUpdatingProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox pb)
                return;

            pb.PasswordChanged -= OnPasswordChanged;
            pb.LostFocus -= OnPasswordLostFocus;
            if (!GetIsUpdating(pb))
                pb.Password = e.NewValue as string ?? string.Empty;
            pb.PasswordChanged += OnPasswordChanged;
            pb.LostFocus += OnPasswordLostFocus;
        }

        private static void OnPasswordLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox pb)
                return;

            SetIsUpdating(pb, true);
            SetBoundPassword(pb, pb.Password);
            SetIsUpdating(pb, false);
        }

        private static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is not PasswordBox pb)
                return;

            SetIsUpdating(pb, true);
            SetBoundPassword(pb, pb.Password);
            SetIsUpdating(pb, false);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
