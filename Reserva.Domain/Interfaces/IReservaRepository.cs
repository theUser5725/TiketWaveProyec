using System;
using System.Threading.Tasks;
using Reserva.Domain.Entidades;

namespace Reserva.Domain.Interfaces
{
    /// <summary>
    /// Contrato del DAO: único punto de acceso para modificar inventario/reservas.
    /// Implementaciones deben garantizar consistencia y concurrencia segura.
    /// </summary>
    public interface IReservaRepository
    {
        Task<Reserva.Domain.Entidades.Reserva> CreateAsync(Reserva.Domain.Entidades.Reserva reserva);
    Task<Reserva.Domain.Entidades.Reserva?> GetAsync(int id);
    Task<bool> TryReserveAsync(int asientoId, int reservaId);
    Task ExpireAsync(int reservaId);
    Task<bool> CancelAsync(int reservaId);
    Task<Reserva.Domain.Entidades.Asiento?> GetAsientoAsync(int id);
    Task<Reserva.Domain.Entidades.Evento?> GetEventoAsync(int id);
    }
}
