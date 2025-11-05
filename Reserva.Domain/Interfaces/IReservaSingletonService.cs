using System;
using System.Threading.Tasks;

namespace Reserva.Domain.Interfaces
{
    /// <summary>
    /// Servicio singleton que permite ejecutar bloques críticos de código de forma seriada
    /// dentro del proceso (útil para reducir colisiones en escenarios de alta concurrencia
    /// y para coordinar la ejecución de transacciones locales).
    /// NOTA: esto no sustituye locks distribuidos cuando hay múltiples instancias de la app.
    /// </summary>
    public interface IReservaSingletonService
    {
        /// <summary>
        /// Ejecuta una función asíncrona de forma exclusiva (serializada) dentro del proceso.
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> work);

        /// <summary>
        /// Ejecuta una acción asíncrona de forma exclusiva (serializada) dentro del proceso.
        /// </summary>
        Task ExecuteAsync(Func<Task> work);
    }
}
