using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowerShop;

namespace AdoreFlowerShop.Data.Repositories
{
    internal interface IReviewRepository
    {
        Task<List<Review>> GetByFlowerIdAsync(int flowerId, CancellationToken ct = default);
        Task<bool> HasUserReviewAsync(int flowerId, int userId, CancellationToken ct = default);
        Task InsertAsync(int flowerId, int userId, int rating, string? comment, CancellationToken ct = default);
    }
}
