using System;
using System.Threading;
using System.Threading.Tasks;
using Reserva.Domain.Interfaces;

namespace Reserva.Infrastructure.Servicios
{
    /// <summary>
    /// Implementación simple del patrón Singleton para ejecutar bloques críticos de forma
    /// exclusiva dentro del proceso usando un SemaphoreSlim.
    /// </summary>
    public class ReservaSingletonService : IReservaSingletonService
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> work)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await work();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ExecuteAsync(Func<Task> work)
        {
            await _semaphore.WaitAsync();
            try
            {
                await work();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
