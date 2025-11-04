using System.Threading.Tasks;
namespace Reserva.Domain.Patrones.State
{
    public interface IReservaState
    {
        string Name { get; }
        Task HandleAsync(Reserva.Domain.Entidades.Reserva reserva);
    }
}
