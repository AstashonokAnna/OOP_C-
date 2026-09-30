using System.Threading;
using System.Threading.Tasks;
using AdoreFlowerShop.Data.Repositories;

namespace AdoreFlowerShop.Data.UnitOfWork
{
    /// <summary>Единица работы с общим DbContext для репозиториев.</summary>
    internal interface IUnitOfWork : IAsyncDisposable
    {
        IUserRepository Users { get; }
        IFlowerRepository Flowers { get; }
        IOrderRepository Orders { get; }
        IReviewRepository Reviews { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
