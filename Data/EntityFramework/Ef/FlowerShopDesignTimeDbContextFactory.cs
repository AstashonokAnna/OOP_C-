using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AdoreFlowerShop.Data.EntityFramework
{
    /// <summary>Фабрика для dotnet ef migrations / scaffold при разработке.</summary>
    public sealed class FlowerShopDesignTimeDbContextFactory : IDesignTimeDbContextFactory<FlowerShopDbContext>
    {
        public FlowerShopDbContext CreateDbContext(string[] args)
        {
            return new FlowerShopDbContext(FlowerShopDbContextOptionsFactory.Create());
        }
    }
}
