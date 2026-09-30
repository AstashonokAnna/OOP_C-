using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AdoreFlowerShop.Data.EntityFramework;

namespace AdoreFlowerShop.Data
{
    /// <summary>Перенумеровывает Id подряд: 1, 2, 3… (без пропусков в таблице).</summary>
    internal static class IdCompactionService
    {
        public static async Task CompactAllAsync(FlowerShopDbContext ctx, CancellationToken ct = default)
        {
            await CompactUsersAsync(ctx, ct);
            await CompactOrdersAsync(ctx, ct);
            await CompactReviewsAsync(ctx, ct);
        }

        private static bool IsSequential(IOrderedEnumerable<int> ids)
        {
            var i = 1;
            foreach (var id in ids)
            {
                if (id != i++)
                    return false;
            }

            return true;
        }

        public static async Task CompactUsersAsync(FlowerShopDbContext ctx, CancellationToken ct = default)
        {
            var users = await ctx.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync(ct);
            if (users.Count == 0 || IsSequential(users.Select(u => u.Id).OrderBy(x => x)))
                return;

            await using var tx = await ctx.Database.BeginTransactionAsync(ct);

            await ctx.Database.ExecuteSqlRawAsync("ALTER TABLE [Orders] NOCHECK CONSTRAINT ALL", ct);
            await ctx.Database.ExecuteSqlRawAsync("ALTER TABLE [Reviews] NOCHECK CONSTRAINT ALL", ct);

            const int tempBase = 500_000;
            foreach (var u in users)
            {
                var tempId = tempBase + u.Id;
                await ctx.Database.ExecuteSqlRawAsync(
                    "UPDATE [Orders] SET [UserId] = {0} WHERE [UserId] = {1}", tempId, u.Id);
                await ctx.Database.ExecuteSqlRawAsync(
                    "UPDATE [Reviews] SET [UserId] = {0} WHERE [UserId] = {1}", tempId, u.Id);
            }

            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM [Users]", ct);
            await ctx.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Users', RESEED, 0)", ct);

            await TableIdAllocator.SetIdentityInsertAsync(ctx, "Users", true, ct);
            try
            {
                for (var i = 0; i < users.Count; i++)
                {
                    var u = users[i];
                    var newId = i + 1;
                    var tempId = tempBase + u.Id;

                    ctx.Users.Add(new ShopUser
                    {
                        Id = newId,
                        Name = u.Name,
                        Password = u.Password,
                        Role = u.Role,
                        Phone = u.Phone
                    });
                    await ctx.SaveChangesAsync(ct);

                    await ctx.Database.ExecuteSqlRawAsync(
                        "UPDATE [Orders] SET [UserId] = {0} WHERE [UserId] = {1}", newId, tempId);
                    await ctx.Database.ExecuteSqlRawAsync(
                        "UPDATE [Reviews] SET [UserId] = {0} WHERE [UserId] = {1}", newId, tempId);
                }
            }
            finally
            {
                await TableIdAllocator.SetIdentityInsertAsync(ctx, "Users", false, ct);
            }

            await ctx.Database.ExecuteSqlRawAsync("ALTER TABLE [Orders] CHECK CONSTRAINT ALL", ct);
            await ctx.Database.ExecuteSqlRawAsync("ALTER TABLE [Reviews] CHECK CONSTRAINT ALL", ct);
            await TableIdAllocator.ReseedIdentityAsync(ctx, "Users", ct);

            ctx.ChangeTracker.Clear();
            await tx.CommitAsync(ct);
        }

        public static async Task CompactOrdersAsync(FlowerShopDbContext ctx, CancellationToken ct = default)
        {
            var orders = await ctx.Orders.AsNoTracking().OrderBy(o => o.Id).ToListAsync(ct);
            if (orders.Count == 0 || IsSequential(orders.Select(o => o.Id).OrderBy(x => x)))
                return;

            await using var tx = await ctx.Database.BeginTransactionAsync(ct);

            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM [Orders]", ct);
            await ctx.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Orders', RESEED, 0)", ct);

            await TableIdAllocator.SetIdentityInsertAsync(ctx, "Orders", true, ct);
            try
            {
                for (var i = 0; i < orders.Count; i++)
                {
                    var o = orders[i];
                    var newId = i + 1;

                    ctx.Orders.Add(new ShopOrder
                    {
                        Id = newId,
                        UserId = o.UserId,
                        FlowerId = o.FlowerId,
                        Quantity = o.Quantity,
                        UnitPrice = o.UnitPrice,
                        Status = o.Status,
                        DeliveryDate = o.DeliveryDate,
                        Address = o.Address,
                        IsRegisteredNotice = o.IsRegisteredNotice
                    });
                    await ctx.SaveChangesAsync(ct);
                }
            }
            finally
            {
                await TableIdAllocator.SetIdentityInsertAsync(ctx, "Orders", false, ct);
            }

            await TableIdAllocator.ReseedIdentityAsync(ctx, "Orders", ct);
            ctx.ChangeTracker.Clear();
            await tx.CommitAsync(ct);
        }

        public static async Task CompactReviewsAsync(FlowerShopDbContext ctx, CancellationToken ct = default)
        {
            var reviews = await ctx.Reviews.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
            if (reviews.Count == 0 || IsSequential(reviews.Select(r => r.Id).OrderBy(x => x)))
                return;

            await using var tx = await ctx.Database.BeginTransactionAsync(ct);

            await ctx.Database.ExecuteSqlRawAsync("DELETE FROM [Reviews]", ct);
            await ctx.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Reviews', RESEED, 0)", ct);

            await TableIdAllocator.SetIdentityInsertAsync(ctx, "Reviews", true, ct);
            try
            {
                for (var i = 0; i < reviews.Count; i++)
                {
                    var r = reviews[i];
                    var newId = i + 1;

                    ctx.Reviews.Add(new ShopReview
                    {
                        Id = newId,
                        FlowerId = r.FlowerId,
                        UserId = r.UserId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    });
                    await ctx.SaveChangesAsync(ct);
                }
            }
            finally
            {
                await TableIdAllocator.SetIdentityInsertAsync(ctx, "Reviews", false, ct);
            }

            await TableIdAllocator.ReseedIdentityAsync(ctx, "Reviews", ct);
            ctx.ChangeTracker.Clear();
            await tx.CommitAsync(ct);
        }
    }
}
