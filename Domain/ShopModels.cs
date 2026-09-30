using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowerShop
{
    public enum UserRole { Admin, Client }

    public class Flower : INotifyPropertyChanged
    {
        private string? shortName;
        private string? fullName;
        private string? description;
        private string? category;
        private double price;
        private int quantity;
        private double rating;
        private string? color;
        private string? country;
        private double discount;
        private bool inStock;
        private int soldCount;
        private int sortOrder;
        private byte[]? mainImageBytes;
        public int Id { get; set; }

        public string? ShortName
        {
            get => shortName;
            set { shortName = value; OnPropertyChanged(); }
        }

        public string? FullName
        {
            get => fullName;
            set { fullName = value; OnPropertyChanged(); }
        }

        public string? Description
        {
            get => description;
            set { description = value; OnPropertyChanged(); }
        }

        public string? Category
        {
            get => category;
            set { category = value; OnPropertyChanged(); }
        }

        public double Price
        {
            get => price;
            set
            {
                var newValue = value;
                if (newValue < 0) newValue = 0;
                if (newValue > 1_000_000) newValue = 1_000_000;

                if (Math.Abs(price - newValue) < 0.0000001)
                    return;

                price = newValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PriceWithDiscount));
            }
        }

        public int Quantity
        {
            get => quantity;
            set
            {
                var newValue = value;
                if (newValue < 0) newValue = 0;
                if (newValue > 200_000) newValue = 200_000;

                if (quantity == newValue)
                    return;

                quantity = newValue;
                OnPropertyChanged();

                if (inStock != quantity > 0)
                {
                    inStock = quantity > 0;
                    OnPropertyChanged(nameof(InStock));
                }
            }
        }

        public double Rating
        {
            get => rating;
            set
            {
                var newValue = value;
                if (newValue < 0) newValue = 0;
                if (newValue > 5) newValue = 5;
                if (Math.Abs(rating - newValue) < 0.0001)
                    return;
                rating = newValue;
                OnPropertyChanged();
            }
        }

        public string? Color
        {
            get => color;
            set
            {
                var newValue = value;
                if (newValue != null && newValue.Length > 12)
                    newValue = newValue[..12];
                if (string.Equals(color, newValue, StringComparison.Ordinal))
                    return;
                color = newValue;
                OnPropertyChanged();
            }
        }

        public string? Country
        {
            get => country;
            set { country = value; OnPropertyChanged(); }
        }

        public double Discount
        {
            get => discount;
            set
            {
                var newValue = value;
                if (newValue < 0) newValue = 0;
                if (newValue > 100) newValue = 100;

                if (Math.Abs(discount - newValue) < 0.0000001)
                    return;

                discount = newValue;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PriceWithDiscount));
            }
        }

        public bool InStock
        {
            get => inStock;
            set { inStock = value; OnPropertyChanged(); }
        }

        public int SoldCount
        {
            get => soldCount;
            set { soldCount = value; OnPropertyChanged(); }
        }

        public int SortOrder
        {
            get => sortOrder;
            set { sortOrder = value; OnPropertyChanged(); }
        }

        public byte[]? MainImageBytes
        {
            get => mainImageBytes;
            set { mainImageBytes = value; OnPropertyChanged(); }
        }

        public double PriceWithDiscount => Price * (1 - Discount / 100);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public UserRole Role { get; set; }
        public string? Password { get; set; }
        public string? Phone { get; set; }
    }

    public class Review
    {
        public int Id { get; set; }
        public int FlowerId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    public class DataModel
    {
        public ObservableCollection<Flower> Flowers { get; set; } = new ObservableCollection<Flower>();
        public ObservableCollection<User> Users { get; set; } = new ObservableCollection<User>();
        public User? CurrentUser { get; set; }
    }
}