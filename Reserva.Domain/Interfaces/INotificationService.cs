using System.Threading.Tasks;
using Reserva.Domain.Patrones.Observer;

namespace Reserva.Domain.Interfaces
{
    public interface INotificationService
    {
        Task EnqueueAsync(IEventoReserva evt);
    }
}
