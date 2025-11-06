using System.Threading.Tasks;
namespace Reserva.Domain.Patrones.State
{
    /// <summary>
    /// Estado Disponible: placeholder para lógica cuando el asiento está disponible.
    /// </summary>
    public class DisponibleState : IReservaState
    {
        public string Name => "Disponible";

        public Task HandleAsync(Reserva.Domain.Entidades.Reserva reserva)
        {
            // Lógica de transición / rules cuando una reserva está en estado disponible.
            return Task.CompletedTask;
        }
    }
}
