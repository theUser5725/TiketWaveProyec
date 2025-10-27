using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Reserva.Domain.Patrones.Observer;

namespace Reserva.Infrastructure.Servicios
{
    /// <summary>
    /// Observador que reacciona a eventos de reserva (ej. reserva expirada) y registra / notifica.
    /// En producción puede enviar a un EventBus o registrar en una tabla de auditoría.
    /// </summary>
    public class NotificacionObserver
    {
        private readonly ILogger<NotificacionObserver> _logger;

        public NotificacionObserver(ILogger<NotificacionObserver> logger)
        {
            _logger = logger;
        }

        public Task OnReservaExpirada(ReservaExpiradaEvent evt)
        {
            // Ejemplo: escribir en logs y en tabla de auditoría.
            _logger.LogInformation("Reserva expirada: {ReservaId} at {Time}", evt.ReservaId, evt.Timestamp);
            return Task.CompletedTask;
        }
    }
}
