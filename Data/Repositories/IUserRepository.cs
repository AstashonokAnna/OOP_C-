using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowerShop;

namespace AdoreFlowerShop.Data.Repositories
{
    /// <summary>Паттерн Репозиторий: абстракция доступа к сущностям пользователей.</summary>
    internal interface IUserRepository
    {
        Task<List<User>> GetAllAsync(CancellationToken ct = default);
        Task UpdatePasswordAsync(string userName, string newPassword, CancellationToken ct = default);

        /// <summary>Возвращает текст ошибки или null при успехе.</summary>
        Task<string?> TryInsertClientAsync(string name, string password, string phone, CancellationToken ct = default);
    }
}
