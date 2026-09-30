using System.Threading;
using System.Threading.Tasks;
using AdoreFlowerShop.Data.EntityFramework;
using AdoreFlowerShop.Data.Repositories;

namespace AdoreFlowerShop.Data.UnitOfWork
{
    internal sealed class FlowerShopUnitOfWork : IUnitOfWork
    {
        private readonly FlowerShopDbContext _context;
        private IUserRepository? _users;
        private IFlowerRepository? _flowers;
        private IOrderRepository? _orders;
        private IReviewRepository? _reviews;

        public FlowerShopUnitOfWork(FlowerShopDbContext context)
        {
            _context = context;
        }

        /// <summary>Создаёт новый экземпляр Unit of Work.</summary>
        public static IUnitOfWork Create() =>
            new FlowerShopUnitOfWork(new FlowerShopDbContext(FlowerShopDbContextOptionsFactory.Create()));

        public IUserRepository Users => _users ??= new UserRepository(_context);
        public IFlowerRepository Flowers => _flowers ??= new FlowerRepository(_context);
        public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
        public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_context);

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }
    }
}
