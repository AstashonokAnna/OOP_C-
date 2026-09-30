using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AdoreFlowerShop.Data;
using AdoreFlowerShop.Data.EntityFramework;

namespace AdoreFlowerShop.Data.Repositories
{
    internal sealed class OrderRepository : IOrderRepository
    {
        private readonly FlowerShopDbContext _context;

        public OrderRepository(FlowerShopDbContext context)
        {
            _context = context;
        }

        public async Task InsertAsync(int userId, int flowerId, int quantity, double unitPrice, string? deliveryDate = null, string? address = null, CancellationToken ct = default)
        {
            var nextId = await TableIdAllocator.NextOrderIdAsync(_context, ct);

            await TableIdAllocator.InsertWithExplicitIdAsync(
                _context,
                "Orders",
                async () =>
                {
                    _context.Orders.Add(new ShopOrder
                    {
                        Id = nextId,
                        UserId = userId,
                        FlowerId = flowerId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Status = "Ожидает обработки",
                        DeliveryDate = deliveryDate,
                        Address = address ?? ""
                    });
                    await _context.SaveChangesAsync(ct);
                },
                ct: ct);
        }

        public async Task<int?> TryGetUserIdByNameAsync(string userName, CancellationToken ct = default)
        {
            return await _context.Users.AsNoTracking()
                .Where(u => u.Name == userName)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<OrderTableRow>> GetAllOrderRowsAsync(CancellationToken ct = default)
        {
            return await _context.Orders.AsNoTracking()
                .OrderBy(o => o.Id)
                .Select(o => new OrderTableRow
                {
                    Id = o.Id,
                    UserName = o.User!.Name,
                    FlowerName = o.Flower!.ShortName,
                    Quantity = o.Quantity,
                    UnitPrice = o.UnitPrice,
                    Status = o.Status,
                    DeliveryDate = o.DeliveryDate,
                    Address = o.Address,
                    IsRegisteredNotice = o.IsRegisteredNotice
                })
                .ToListAsync(ct);
        }

        public async Task<List<OrderTableRow>> GetOrdersByUserIdAsync(int userId, CancellationToken ct = default)
        {
            return await _context.Orders.AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderBy(o => o.Id)
                .Select(o => new OrderTableRow
                {
                    Id = o.Id,
                    UserName = o.User!.Name,
                    FlowerName = o.Flower!.ShortName,
                    Quantity = o.Quantity,
                    UnitPrice = o.UnitPrice,
                    Status = o.Status,
                    DeliveryDate = o.DeliveryDate,
                    Address = o.Address,
                    IsRegisteredNotice = o.IsRegisteredNotice
                })
                .ToListAsync(ct);
        }

        public async Task UpdateStatusAsync(int orderId, string status, CancellationToken ct = default)
        {
            var order = await _context.Orders.FindAsync(new object[] { orderId }, ct);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync(ct);
            }
        }

        public async Task SetRegisteredNoticeAsync(int orderId, bool isRegisteredNotice = true, CancellationToken ct = default)
        {
            var order = await _context.Orders.FindAsync(new object[] { orderId }, ct);
            if (order == null)
                throw new InvalidOperationException($"Заказ Id={orderId} не найден.");

            order.IsRegisteredNotice = isRegisteredNotice;
            await _context.SaveChangesAsync(ct);
        }
    }
}
