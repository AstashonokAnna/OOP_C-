using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AdoreFlowerShop.Data.EntityFramework
{
    /// <summary>Code First: схема создаётся через EnsureCreatedAsync + T-SQL в DbInitializer.</summary>
    public sealed class FlowerShopDbContext : DbContext
    {
        public FlowerShopDbContext(DbContextOptions<FlowerShopDbContext> options)
            : base(options)
        {
        }

        public DbSet<ShopUser> Users => Set<ShopUser>();
        public DbSet<ShopFlower> Flowers => Set<ShopFlower>();
        public DbSet<ShopFlowerImage> FlowerImages => Set<ShopFlowerImage>();
        public DbSet<ShopOrder> Orders => Set<ShopOrder>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<ShopReview> Reviews => Set<ShopReview>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShopFlowerImage>()
                .HasOne(i => i.Flower)
                .WithMany(f => f.FlowerImages)
                .HasForeignKey(i => i.FlowerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShopOrder>(entity =>
            {
                entity.ToTable(tb =>
                {
                    // Триггер trg_Orders_AfterInsert_UpdateStock — без этого EF Core 8 падает на INSERT.
                    tb.HasTrigger("trg_Orders_AfterInsert_UpdateStock");
                    tb.UseSqlOutputClause(false);
                });

                entity.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Flower)
                    .WithMany(f => f.Orders)
                    .HasForeignKey(o => o.FlowerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Имена FK для связующей таблицы FlowerTags совпадают с SQLite-схемой.
            modelBuilder.Entity<ShopFlower>()
                .HasMany(f => f.Tags)
                .WithMany(t => t.Flowers)
                .UsingEntity<Dictionary<string, object>>(
                    "FlowerTags",
                    r => r.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                    l => l.HasOne<ShopFlower>().WithMany().HasForeignKey("FlowerId"),
                    je =>
                    {
                        je.ToTable("FlowerTags");
                        je.HasKey("FlowerId", "TagId");
                    });
        }
    }
}
