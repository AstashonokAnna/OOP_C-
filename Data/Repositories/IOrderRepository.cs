using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdoreFlowerShop.Data.Repositories
{
    /// <summary>Паттерн Репозиторий: абстракция доступа к заказам.</summary>
    internal interface IOrderRepository
    {
        Task InsertAsync(int userId, int flowerId, int quantity, double unitPrice, string? deliveryDate = null, string? address = null, CancellationToken ct = default);
        Task<int?> TryGetUserIdByNameAsync(string userName, CancellationToken ct = default);
        Task<List<OrderTableRow>> GetAllOrderRowsAsync(CancellationToken ct = default);
        Task<List<OrderTableRow>> GetOrdersByUserIdAsync(int userId, CancellationToken ct = default);
        Task UpdateStatusAsync(int orderId, string status, CancellationToken ct = default);
        Task SetRegisteredNoticeAsync(int orderId, bool isRegisteredNotice = true, CancellationToken ct = default);
    }
}
