using System;
using System.Windows;
using AdoreFlowerShop.Data.UnitOfWork;
using AdoreFlowerShop.Services;

namespace AdoreFlowerShop.Views
{
    public partial class ReviewDialogWindow : Window
    {
        private readonly int _flowerId;
        private readonly int _userId;

        public ReviewDialogWindow(int flowerId, int userId)
        {
            _flowerId = flowerId;
            _userId = userId;
            InitializeComponent();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            var rating = RatingCombo.SelectedIndex + 1;
            if (rating < 1 || rating > 5)
            {
                MessageBox.Show("Пожалуйста, выберите оценку от 1 до 5.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var comment = CommentBox.Text.Trim();
            var commentErr = InputValidator.ValidateReviewComment(comment);
            if (commentErr != null)
            {
                MessageBox.Show(commentErr, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (BadWordsFilter.ContainsBadWords(comment))
            {
                MessageBox.Show("Отзыв содержит недопустимые выражения. Пожалуйста, напишите корректный отзыв.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await using var uow = FlowerShopUnitOfWork.Create();
                if (await uow.Reviews.HasUserReviewAsync(_flowerId, _userId))
                {
                    MessageBox.Show(
                        "Вы уже оставляли отзыв на этот цветок. Можно оставить отзыв только один раз.",
                        "Внимание",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                await uow.Reviews.InsertAsync(_flowerId, _userId, rating, comment);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                if (msg.Contains("UQ_Review_Flower_User", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "Вы уже оставляли отзыв на этот цветок. Можно оставить отзыв только один раз.",
                        "Внимание",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                MessageBox.Show($"Не удалось сохранить отзыв: {msg}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
