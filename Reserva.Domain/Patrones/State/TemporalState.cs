using System.Threading.Tasks;

namespace Reserva.Domain.Patrones.State
{
    /// <summary>
    /// Estado Temporal: reservado temporalmente, pendiente de pago; debe expirar automáticamente.
    /// </summary>
    public class TemporalState : IReservaState
    {
        public string Name => "Temporal";

        public Task HandleAsync(Domain.Entidades.Reserva reserva)
        {
            // Lógica de temporización/expiración.
            return Task.CompletedTask;
        }
    }
}
