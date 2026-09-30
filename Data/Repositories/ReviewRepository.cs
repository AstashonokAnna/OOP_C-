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
    internal sealed class ReviewRepository : IReviewRepository
    {
        private readonly FlowerShopDbContext _context;

        public ReviewRepository(FlowerShopDbContext context)
        {
            _context = context;
        }

        public async Task<List<Review>> GetByFlowerIdAsync(int flowerId, CancellationToken ct = default)
        {
            return await _context.Reviews.AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.FlowerId == flowerId)
                .OrderByDescending(r => r.Id)
                .Select(r => EntityToModelMapper.ToReview(r))
                .ToListAsync(ct);
        }

        public Task<bool> HasUserReviewAsync(int flowerId, int userId, CancellationToken ct = default) =>
            _context.Reviews.AsNoTracking()
                .AnyAsync(r => r.FlowerId == flowerId && r.UserId == userId, ct);

        public async Task InsertAsync(int flowerId, int userId, int rating, string? comment, CancellationToken ct = default)
        {
            var nextId = await TableIdAllocator.NextReviewIdAsync(_context, ct);

            await TableIdAllocator.InsertWithExplicitIdAsync(
                _context,
                "Reviews",
                async () =>
                {
                    _context.Reviews.Add(new ShopReview
                    {
                        Id = nextId,
                        FlowerId = flowerId,
                        UserId = userId,
                        Rating = rating,
                        Comment = comment,
                        CreatedAt = DateTime.UtcNow.ToString("O")
                    });
                    await _context.SaveChangesAsync(ct);
                },
                ct: ct);
        }

    }
}
