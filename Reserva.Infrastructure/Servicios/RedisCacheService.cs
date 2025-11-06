using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Reserva.Domain.Interfaces;

namespace Reserva.Infrastructure.Servicios
{
    /// <summary>
    /// Implementación placeholder de caché. Sustituir por Redis real (StackExchange.Redis) en producción.
    /// Mantiene pares clave/valor en memoria para desarrollo y pruebas rápidas.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private static readonly ConcurrentDictionary<string, (string value, DateTime? expires)> _cache = new();

        public Task<string?> GetAsync(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.expires.HasValue && DateTime.UtcNow > entry.expires.Value)
                {
                    _cache.TryRemove(key, out _);
                    return Task.FromResult<string?>(null);
                }

                return Task.FromResult<string?>(entry.value);
            }
            return Task.FromResult<string?>(null);
        }

        public Task RemoveAsync(string key)
        {
            _cache.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task SetAsync(string key, string value, int? ttlSeconds = null)
        {
            DateTime? expires = null;
            if (ttlSeconds.HasValue)
                expires = DateTime.UtcNow.AddSeconds(ttlSeconds.Value);

            _cache[key] = (value, expires);
            return Task.CompletedTask;
        }
    }
}
