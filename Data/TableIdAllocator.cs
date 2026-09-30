using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AdoreFlowerShop.Data.EntityFramework;

namespace AdoreFlowerShop.Data
{
    /// <summary>Назначает первый свободный Id и вставляет строку с явным Id в одной транзакции.</summary>
    internal static class TableIdAllocator
    {
        public static int GetNextId(IEnumerable<int> existingIds)
        {
            var next = 1;
            foreach (var id in existingIds.OrderBy(x => x))
            {
                if (id > next)
                    return next;
                if (id == next)
                    next++;
            }

            return next;
        }

        public static Task<int> NextFlowerIdAsync(FlowerShopDbContext ctx, CancellationToken ct = default) =>
            NextIdFromQueryAsync(ctx.Flowers.AsNoTracking().Select(f => f.Id), ct);

        public static Task<int> NextUserIdAsync(FlowerShopDbContext ctx, CancellationToken ct = default) =>
            NextIdFromQueryAsync(ctx.Users.AsNoTracking().Select(u => u.Id), ct);

        public static Task<int> NextOrderIdAsync(FlowerShopDbContext ctx, CancellationToken ct = default) =>
            NextIdFromQueryAsync(ctx.Orders.AsNoTracking().Select(o => o.Id), ct);

        public static Task<int> NextReviewIdAsync(FlowerShopDbContext ctx, CancellationToken ct = default) =>
            NextIdFromQueryAsync(ctx.Reviews.AsNoTracking().Select(r => r.Id), ct);

        private static async Task<int> NextIdFromQueryAsync(IQueryable<int> ids, CancellationToken ct)
        {
            var list = await ids.ToListAsync(ct);
            return GetNextId(list);
        }

        /// <summary>Вставка с явным Id: IDENTITY_INSERT и SaveChanges в одной транзакции.</summary>
        public static async Task InsertWithExplicitIdAsync(
            FlowerShopDbContext ctx,
            string tableName,
            Func<Task> insertAction,
            IDbContextTransaction? existingTransaction = null,
            CancellationToken ct = default)
        {
            var ownsTransaction = existingTransaction == null;
            var transaction = existingTransaction ?? await ctx.Database.BeginTransactionAsync(ct);

            try
            {
                await SetIdentityInsertAsync(ctx, tableName, true, ct);
                await insertAction();
                await SetIdentityInsertAsync(ctx, tableName, false, ct);
                await ReseedIdentityAsync(ctx, tableName, ct);

                if (ownsTransaction)
                    await transaction.CommitAsync(ct);
            }
            catch
            {
                if (ownsTransaction)
                    await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public static async Task SetIdentityInsertAsync(
            FlowerShopDbContext ctx,
            string tableName,
            bool enabled,
            CancellationToken ct = default)
        {
            if (!IsKnownTable(tableName))
                throw new ArgumentException($"Unknown table: {tableName}", nameof(tableName));

            var state = enabled ? "ON" : "OFF";
            await ctx.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tableName}] {state}", ct);
        }

        public static async Task ReseedIdentityAsync(
            FlowerShopDbContext ctx,
            string tableName,
            CancellationToken ct = default)
        {
            if (!IsKnownTable(tableName))
                throw new ArgumentException($"Unknown table: {tableName}", nameof(tableName));

            var max = tableName switch
            {
                "Flowers" => await ctx.Flowers.AsNoTracking().MaxAsync(f => (int?)f.Id, ct) ?? 0,
                "Users" => await ctx.Users.AsNoTracking().MaxAsync(u => (int?)u.Id, ct) ?? 0,
                "Orders" => await ctx.Orders.AsNoTracking().MaxAsync(o => (int?)o.Id, ct) ?? 0,
                "Reviews" => await ctx.Reviews.AsNoTracking().MaxAsync(r => (int?)r.Id, ct) ?? 0,
                _ => 0
            };

            await ctx.Database.ExecuteSqlRawAsync($"DBCC CHECKIDENT ('{tableName}', RESEED, {max})", ct);
        }

        private static bool IsKnownTable(string tableName) =>
            tableName is "Flowers" or "Users" or "Orders" or "Reviews";
    }
}
