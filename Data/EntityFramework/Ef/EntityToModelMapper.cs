using System.Linq;
using FlowerShop;

namespace AdoreFlowerShop.Data.EntityFramework
{
    internal static class EntityToModelMapper
    {
        public static User ToUser(ShopUser u) =>
            new User
            {
                Id = u.Id,
                Name = u.Name,
                Password = u.Password,
                Phone = u.Phone,
                Role = u.Role.Equals("Admin", System.StringComparison.OrdinalIgnoreCase)
                    ? UserRole.Admin
                    : UserRole.Client
            };

        public static Flower ToFlower(ShopFlower f, byte[]? mainImage)
        {
            return new Flower
            {
                Id = f.Id,
                ShortName = f.ShortName,
                FullName = f.FullName,
                Description = f.Description,
                Category = f.Category,
                Price = f.Price,
                Quantity = f.Quantity,
                Rating = f.Rating,
                Color = f.Color,
                Country = f.Country,
                Discount = f.Discount,
                InStock = f.InStock,
                SoldCount = f.SoldCount,
                SortOrder = f.SortOrder,
                MainImageBytes = mainImage
            };
        }

        public static void ApplyFlower(Flower src, ShopFlower dst)
        {
            dst.ShortName = src.ShortName ?? "";
            dst.FullName = src.FullName;
            dst.Description = src.Description;
            dst.Category = src.Category;
            dst.Price = src.Price;
            dst.Quantity = src.Quantity;
            // Рейтинг формируется отзывами клиентов, админ не меняет его при сохранении.
            dst.Color = src.Color;
            dst.Country = src.Country;
            dst.Discount = src.Discount;
            dst.InStock = src.InStock;
            dst.SoldCount = src.SoldCount;
            dst.SortOrder = src.SortOrder;
        }

        public static ShopFlower NewShopFlowerFrom(Flower src)
        {
            var e = new ShopFlower();
            ApplyFlower(src, e);
            return e;
        }

        public static Review ToReview(ShopReview r) =>
            new Review
            {
                Id = r.Id,
                FlowerId = r.FlowerId,
                UserId = r.UserId,
                UserName = r.User?.Name ?? "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            };

        public static byte[]? MainImageBytes(System.Collections.Generic.IEnumerable<ShopFlowerImage> images)
        {
            var main = images.FirstOrDefault(i => i.IsMain);
            return main?.Image;
        }
    }
}
