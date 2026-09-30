using System.Configuration;

namespace AdoreFlowerShop.Data
{
    internal static class DbPaths
    {
        public static string GetConnectionString()
        {
            var cs = ConfigurationManager.ConnectionStrings["FlowerShopDb"]?.ConnectionString;
            return cs ?? "Server=(LocalDB)\\MSSQLLocalDB;Database=FlowerShopDb;Integrated Security=true;Trust Server Certificate=true;";
        }
    }
}
