using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AdoreFlowerShop.Data.EntityFramework;

namespace AdoreFlowerShop.Data
{
    internal static class DbInitializer
    {
        public static async Task EnsureCreatedAsync(CancellationToken ct = default)
        {
            await using var context = new FlowerShopDbContext(FlowerShopDbContextOptionsFactory.Create());

            await context.Database.EnsureCreatedAsync(ct);

            await EnsureDatabaseObjectsAsync(context, ct);

            // Seed minimal data if empty
            if (!await context.Flowers.AnyAsync(ct))
                await SeedAsync(context, ct);

            await EnsureAdoreCatalogAsync(context, ct);

            // Maintenance
            await EnsureFlowerCountriesAsync(context, ct);
            await EnsureFlowerSortOrderAsync(context, ct);
            await EnsureLiliesUnavailableAsync(context, ct);
            await EnsureRosesAndTulipsInStockAsync(context, ct);
            await RemoveDemoUserAnnaAsync(context, ct);
            await IdCompactionService.CompactAllAsync(context, ct);
        }

        private static async Task EnsureDatabaseObjectsAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_UserId')
    CREATE INDEX IX_Orders_UserId ON Orders(UserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_FlowerId')
    CREATE INDEX IX_Orders_FlowerId ON Orders(FlowerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FlowerImages_FlowerId')
    CREATE INDEX IX_FlowerImages_FlowerId ON FlowerImages(FlowerId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FlowerTags_TagId')
    CREATE INDEX IX_FlowerTags_TagId ON FlowerTags(TagId);", ct);

            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name='Status' AND object_id=OBJECT_ID('Orders'))
    ALTER TABLE Orders ADD Status nvarchar(50) NOT NULL DEFAULT N'Ожидает обработки';", ct);

            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name='DeliveryDate' AND object_id=OBJECT_ID('Orders'))
    ALTER TABLE Orders ADD DeliveryDate nvarchar(50) NULL;", ct);

            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name='Address' AND object_id=OBJECT_ID('Orders'))
    ALTER TABLE Orders ADD Address nvarchar(500) NULL;", ct);

            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name='IsRegisteredNotice' AND object_id=OBJECT_ID('Orders'))
    ALTER TABLE Orders ADD IsRegisteredNotice bit NOT NULL DEFAULT 0;", ct);

            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name='SortOrder' AND object_id=OBJECT_ID('Flowers'))
    ALTER TABLE Flowers ADD SortOrder int NOT NULL DEFAULT 0;", ct);

            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE name='SoldCount' AND object_id=OBJECT_ID('Flowers'))
    ALTER TABLE Flowers ADD SoldCount int NOT NULL DEFAULT 0;", ct);

            // Удаляем второе название у всех букетов: FullName = ShortName
            await context.Database.ExecuteSqlRawAsync(@"
UPDATE Flowers SET FullName = ShortName WHERE FullName IS NOT NULL AND FullName <> ShortName;", ct);

            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Reviews')
    CREATE TABLE Reviews (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FlowerId int NOT NULL FOREIGN KEY REFERENCES Flowers(Id) ON DELETE CASCADE,
        UserId int NOT NULL FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
        Rating int NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
        Comment nvarchar(max) NULL,
        CreatedAt nvarchar(50) NOT NULL
    );", ct);

            // Отдельный batch: иначе при ошибке парсинга CREATE не выполнится DROP.
            await context.Database.ExecuteSqlRawAsync(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE name = N'UQ_Review_Flower_User' AND type = 'UQ')
    ALTER TABLE Reviews DROP CONSTRAINT UQ_Review_Flower_User;", ct);

            await context.Database.ExecuteSqlRawAsync(@"
CREATE OR ALTER TRIGGER trg_Orders_AfterInsert_UpdateStock
ON Orders
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE f
    SET Quantity = CASE WHEN f.Quantity - i.Quantity < 0 THEN 0 ELSE f.Quantity - i.Quantity END,
        SoldCount = f.SoldCount + i.Quantity
    FROM Flowers f
    INNER JOIN inserted i ON f.Id = i.FlowerId
END", ct);
        }

        private static async Task SeedAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            context.Users.Add(new ShopUser
            {
                Name = "admin",
                Password = "admin",
                Role = "Admin"
            });
            await context.SaveChangesAsync(ct);

            var rose = new ShopFlower
            {
                ShortName = "Роза",
                FullName = "Роза",
                Description = "Красивый красный цветок с приятным ароматом",
                Category = "Классические",
                Price = 10,
                Quantity = 100000,
                Rating = 4.5,
                Color = "Красный",
                Country = "Россия",
                Discount = 0,
                InStock = true,
                SoldCount = 0
            };
            context.Flowers.Add(rose);
            await context.SaveChangesAsync(ct);

            var lily = new ShopFlower
            {
                ShortName = "Лилия",
                FullName = "Лилия",
                Description = "Элегантный цветок с изысканным ароматом",
                Category = "Классические",
                Price = 20,
                Quantity = 0,
                Rating = 4.8,
                Color = "Белый",
                Country = "Франция",
                Discount = 0,
                InStock = false,
                SoldCount = 0
            };
            context.Flowers.Add(lily);
            await context.SaveChangesAsync(ct);

            await TrySeedImageAsync(context, rose.Id, "розы.jpg", ct);
            await TrySeedImageAsync(context, lily.Id, "лилии.jpg", ct);
        }

        private static async Task EnsureAdoreCatalogAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            foreach (var item in AdoreCatalogItems)
            {
                if (await context.Flowers.AnyAsync(f => f.ShortName == item.ShortName, ct))
                    continue;

                var flower = new ShopFlower
                {
                    ShortName = item.ShortName,
                    FullName = item.FullName,
                    Description = item.Description,
                    Category = item.Category,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    Rating = item.Rating,
                    Color = item.Color,
                    Country = item.Country,
                    Discount = item.Discount,
                    InStock = item.Quantity > 0,
                    SoldCount = 0
                };
                context.Flowers.Add(flower);
                await context.SaveChangesAsync(ct);

                if (!string.IsNullOrEmpty(item.ImageFile))
                    await TrySeedImageAsync(context, flower.Id, item.ImageFile, ct);
            }

            await EnsureFlowerCategoriesAsync(context, ct);
        }

        private static async Task EnsureFlowerCategoriesAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            await context.Database.ExecuteSqlRawAsync(@"
UPDATE Flowers SET Category=N'Классические' WHERE ShortName=N'Роза';
UPDATE Flowers SET Category=N'Весеннее настроение' WHERE ShortName=N'Тюльпан';
UPDATE Flowers SET Category=N'Классические' WHERE ShortName=N'Лилия';
UPDATE Flowers SET Category=N'Весеннее настроение' WHERE ShortName=N'Алоэ';
UPDATE Flowers SET Category=N'Классические' WHERE ShortName=N'Гортензия';
UPDATE Flowers SET Category=N'Весеннее настроение' WHERE ShortName=N'Лаванда';
UPDATE Flowers SET Category=N'Классические' WHERE ShortName=N'Орхидея';
UPDATE Flowers SET Category=N'Классические' WHERE ShortName=N'Пион';
UPDATE Flowers SET Category=N'Весеннее настроение' WHERE ShortName=N'Подсолнух';
UPDATE Flowers SET Category=N'Весеннее настроение' WHERE ShortName=N'Стрелиция';
UPDATE Flowers SET Category=N'Классические' WHERE ShortName=N'Хризантема';", ct);
        }

        private static async Task EnsureFlowerCountriesAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            await context.Database.ExecuteSqlRawAsync(@"
UPDATE Flowers SET Country=N'Россия' WHERE ShortName=N'Роза';
UPDATE Flowers SET Country=N'Голландия' WHERE ShortName=N'Тюльпан';
UPDATE Flowers SET Country=N'Голландия' WHERE ShortName=N'Лилия';
UPDATE Flowers SET Country=N'ЮАР' WHERE ShortName=N'Алоэ';
UPDATE Flowers SET Country=N'Россия' WHERE ShortName=N'Гортензия';
UPDATE Flowers SET Country=N'Россия' WHERE ShortName=N'Лаванда';
UPDATE Flowers SET Country=N'ЮАР' WHERE ShortName=N'Орхидея';
UPDATE Flowers SET Country=N'Голландия' WHERE ShortName=N'Пион';
UPDATE Flowers SET Country=N'Россия' WHERE ShortName=N'Подсолнух';
UPDATE Flowers SET Country=N'ЮАР' WHERE ShortName=N'Стрелиция';
UPDATE Flowers SET Country=N'Россия' WHERE ShortName=N'Хризантема';", ct);
        }

        private static async Task EnsureFlowerSortOrderAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            await context.Database.ExecuteSqlRawAsync(@"
UPDATE Flowers SET SortOrder=1 WHERE ShortName=N'Роза';
UPDATE Flowers SET SortOrder=2 WHERE ShortName=N'Тюльпан';
UPDATE Flowers SET SortOrder=3 WHERE ShortName=N'Лилия';
UPDATE Flowers SET SortOrder=4 WHERE ShortName=N'Алоэ';
UPDATE Flowers SET SortOrder=5 WHERE ShortName=N'Гортензия';
UPDATE Flowers SET SortOrder=6 WHERE ShortName=N'Лаванда';
UPDATE Flowers SET SortOrder=7 WHERE ShortName=N'Орхидея';
UPDATE Flowers SET SortOrder=8 WHERE ShortName=N'Пион';
UPDATE Flowers SET SortOrder=9 WHERE ShortName=N'Подсолнух';
UPDATE Flowers SET SortOrder=10 WHERE ShortName=N'Стрелиция';
UPDATE Flowers SET SortOrder=11 WHERE ShortName=N'Хризантема';", ct);
        }

        private static async Task TrySeedImageAsync(FlowerShopDbContext context, int flowerId, string fileName, CancellationToken ct)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDir, "CatalogPhotos", fileName);
                if (!File.Exists(path))
                    return;

                var bytes = await File.ReadAllBytesAsync(path, ct);
                context.FlowerImages.Add(new ShopFlowerImage
                {
                    FlowerId = flowerId,
                    Image = bytes,
                    IsMain = true
                });
                await context.SaveChangesAsync(ct);
            }
            catch
            {
                // seeding image is best-effort
            }
        }

        private static async Task EnsureLiliesUnavailableAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE Flowers SET Quantity = 0, InStock = 0 WHERE ShortName = N'Лилия'", ct);
        }

        private static async Task EnsureRosesAndTulipsInStockAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            await context.Database.ExecuteSqlRawAsync(@"
UPDATE Flowers
SET InStock = 1,
    Quantity = CASE
        WHEN ShortName LIKE N'%Роза%' THEN 100000
        WHEN ShortName LIKE N'%Тюльпан%' THEN 200000
        ELSE Quantity
    END
WHERE (ShortName LIKE N'%Роза%' OR ShortName LIKE N'%Тюльпан%')
  AND (InStock = 0 OR Quantity = 0)", ct);
        }

        /// <summary>Удаляет демо-пользователя anna и связанные заказы/отзывы (однократно при старте, идемпотентно).</summary>
        private static async Task RemoveDemoUserAnnaAsync(FlowerShopDbContext context, CancellationToken ct)
        {
            var anna = await context.Users.FirstOrDefaultAsync(u => u.Name == "anna", ct);
            if (anna == null)
                return;

            var orders = await context.Orders.Where(o => o.UserId == anna.Id).ToListAsync(ct);
            if (orders.Count > 0)
                context.Orders.RemoveRange(orders);

            var reviews = await context.Reviews.Where(r => r.UserId == anna.Id).ToListAsync(ct);
            if (reviews.Count > 0)
                context.Reviews.RemoveRange(reviews);

            context.Users.Remove(anna);
            await context.SaveChangesAsync(ct);
        }

        private static readonly (string ShortName, string FullName, string Description, string Category, double Price, int Quantity, double Rating, string Color, string Country, double Discount, string ImageFile)[] AdoreCatalogItems =
        {
            // Классические
            ("Гортензия", "Гортензия", "Пышные шарообразные соцветия — объём и нежность в одном букете.", "Классические", 32, 20, 4.7, "Голубой", "Япония", 5, "гортензия.jpg"),
            ("Орхидея", "Орхидея", "Долго цветущая элегантность — премиальный подарок.", "Классические", 45, 10, 4.9, "Белый", "Таиланд", 0, "орхидея.jpg"),
            ("Пион", "Пион", "Нежные лепестки и сезонное настроение сада.", "Классические", 38, 15, 4.8, "Розовый", "Голландия", 0, "пион.jpg"),
            ("Хризантема", "Хризантема", "Долго стоит в воде, богатая палитра лепестков.", "Классические", 12, 40, 4.3, "Белый", "Китай", 15, "хризантема.jpg"),
            // Весеннее настроение
            ("Тюльпан", "Тюльпан", "Весенний цветок, символ радости и счастья.", "Весеннее настроение", 5, 200000, 4.2, "Желтый", "Голландия", 10, "тюльпаны.jpg"),
            ("Алоэ", "Алоэ", "Суккулент с плотными листьями — спокойный зелёный акцент для дома.", "Весеннее настроение", 18, 12, 4.4, "Зелёный", "ЮАР", 0, "алоэ.jpg"),
            ("Лаванда", "Лаванда", "Сухие и свежие веточки с узнаваемым ароматом Прованса.", "Весеннее настроение", 24, 30, 4.6, "Фиолетовый", "Франция", 0, "лаванда.jpg"),
            ("Подсолнух", "Подсолнух", "Яркое солнце в букете — летнее настроение.", "Весеннее настроение", 15, 25, 4.5, "Жёлтый", "Россия", 10, "подсолнух.jpg"),
            ("Стрелиция", "Стрелиция", "Тропическая «птица рая» для смелых композиций.", "Весеннее настроение", 42, 8, 4.7, "Оранжевый", "ЮАР", 0, "стрелиция.jpg"),
        };
    }
}
