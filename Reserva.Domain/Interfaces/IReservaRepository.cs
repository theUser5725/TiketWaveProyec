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
        Task<Reserva.Domain.Entidades.Reserva?> GetAsync(Guid id);
        Task<bool> TryReserveAsync(Guid asientoId, Guid reservaId);
        Task ExpireAsync(Guid reservaId);
    }
}
