using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowerShop;

namespace AdoreFlowerShop.Data.Repositories
{
    /// <summary>Паттерн Репозиторий: абстракция доступа к каталогу цветов.</summary>
    internal interface IFlowerRepository
    {
        Task<List<Flower>> GetAllAsync(CancellationToken ct = default);
        Task<int> InsertAsync(Flower flower, byte[]? mainImageBytes, CancellationToken ct = default);
        Task UpdateAsync(Flower flower, byte[]? newMainImageBytesOrNullToKeep, CancellationToken ct = default);
        Task DeleteAsync(int flowerId, CancellationToken ct = default);
    }
}
