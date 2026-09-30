using System.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AdoreFlowerShop.Data.EntityFramework
{
    internal static class FlowerShopDbContextOptionsFactory
    {
        public static DbContextOptions<FlowerShopDbContext> Create()
        {
            var cs = DbPaths.GetConnectionString();
            var builder = new DbContextOptionsBuilder<FlowerShopDbContext>()
                .UseSqlServer(cs, sqlServer =>
                {
                    if (int.TryParse(ConfigurationManager.AppSettings["EfCommandTimeoutSeconds"], out var sec))
                        sqlServer.CommandTimeout(sec);
                });

            var detailed = ConfigurationManager.AppSettings["EfEnableDetailedErrors"];
            if (string.Equals(detailed, "true", StringComparison.OrdinalIgnoreCase))
                builder = builder.EnableDetailedErrors();

            return builder.Options;
        }
    }
}
