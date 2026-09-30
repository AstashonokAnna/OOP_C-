using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlowerShop;
using Microsoft.EntityFrameworkCore;
using AdoreFlowerShop.Data;
using AdoreFlowerShop.Data.EntityFramework;

namespace AdoreFlowerShop.Data.Repositories
{
    internal sealed class FlowerRepository : IFlowerRepository
    {
        private readonly FlowerShopDbContext _context;

        public FlowerRepository(FlowerShopDbContext context)
        {
            _context = context;
        }

        public async Task<List<Flower>> GetAllAsync(CancellationToken ct = default)
        {
            var rows = await _context.Flowers.AsNoTracking()
                .Include(f => f.FlowerImages)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Id)
                .ToListAsync(ct);

            var reviewAverages = await _context.Reviews.AsNoTracking()
                .GroupBy(r => r.FlowerId)
                .Select(g => new { FlowerId = g.Key, AvgRating = g.Average(r => (double)r.Rating) })
                .ToDictionaryAsync(g => g.FlowerId, g => g.AvgRating, ct);

            return rows.Select(f =>
            {
                var flower = EntityToModelMapper.ToFlower(f, EntityToModelMapper.MainImageBytes(f.FlowerImages));
                if (reviewAverages.TryGetValue(f.Id, out var avg))
                    flower.Rating = Math.Round(avg, 1);
                else
                    flower.Rating = 0;
                return flower;
            }).ToList();
        }

        public async Task<int> InsertAsync(Flower flower, byte[]? mainImageBytes, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var nextId = await TableIdAllocator.NextFlowerIdAsync(_context, ct);
                var entity = EntityToModelMapper.NewShopFlowerFrom(flower);
                entity.Id = nextId;

                await TableIdAllocator.InsertWithExplicitIdAsync(
                    _context,
                    "Flowers",
                    async () =>
                    {
                        _context.Flowers.Add(entity);
                        await _context.SaveChangesAsync(ct);
                    },
                    tx,
                    ct);

                if (mainImageBytes is { Length: > 0 })
                {
                    _context.FlowerImages.Add(new ShopFlowerImage
                    {
                        FlowerId = entity.Id,
                        Image = mainImageBytes,
                        IsMain = true
                    });
                    await _context.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);
                return entity.Id;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task UpdateAsync(Flower flower, byte[]? newMainImageBytesOrNullToKeep, CancellationToken ct = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var tracked = await _context.Flowers
                    .Include(f => f.FlowerImages)
                    .FirstOrDefaultAsync(f => f.Id == flower.Id, ct);

                if (tracked == null)
                    throw new InvalidOperationException($"Цветок Id={flower.Id} не найден.");

                EntityToModelMapper.ApplyFlower(flower, tracked);
                await _context.SaveChangesAsync(ct);

                if (newMainImageBytesOrNullToKeep is { Length: > 0 })
                {
                    foreach (var img in tracked.FlowerImages.Where(i => i.IsMain).ToList())
                        _context.FlowerImages.Remove(img);

                    _context.FlowerImages.Add(new ShopFlowerImage
                    {
                        FlowerId = tracked.Id,
                        Image = newMainImageBytesOrNullToKeep,
                        IsMain = true
                    });
                    await _context.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task DeleteAsync(int flowerId, CancellationToken ct = default)
        {
            var entity = await _context.Flowers.FindAsync(new object[] { flowerId }, ct);
            if (entity == null)
                return;
            _context.Flowers.Remove(entity);
            await _context.SaveChangesAsync(ct);
        }

    }
}
