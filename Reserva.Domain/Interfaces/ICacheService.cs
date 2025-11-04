using System.Threading.Tasks;

namespace Reserva.Domain.Interfaces
{
    /// <summary>
    /// Abstracción para caché (Redis u otro) usada por el dominio/infrastructure.
    /// </summary>
    public interface ICacheService
    {
        Task SetAsync(string key, string value, int? ttlSeconds = null);
        Task<string?> GetAsync(string key);
        Task RemoveAsync(string key);
    }
}
