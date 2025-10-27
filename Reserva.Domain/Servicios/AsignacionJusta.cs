using System;
using System.Collections.Concurrent;

namespace Reserva.Domain.Servicios
{
    /// <summary>
    /// Servicio simple (esqueleto) para ilustrar asignación justa.
    /// En un sistema real puede usar colas, round-robin, prioridad o locks distribuidos.
    /// </summary>
    public class AsignacionJusta
    {
        // Ejemplo de singleton in-memory para coordinación local.
        private static readonly ConcurrentDictionary<Guid, byte> _reservas = new();

        public bool TryClaim(Guid asientoId)
        {
            return _reservas.TryAdd(asientoId, 0);
        }

        public void Release(Guid asientoId)
        {
            _reservas.TryRemove(asientoId, out _);
        }
    }
}
