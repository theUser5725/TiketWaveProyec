using System;
using System.Threading.Tasks;
using Reserva.Domain.Entidades;

namespace Reserva.Domain.Interfaces
{
    /// <summary>
    /// Contrato del DAO: único punto de acceso para modificar inventario/reservas.
    /// Implementaciones deben garantizar consistencia y concurrencia segura.
    /// Completamente sincronizado con scripts/init.sql, ContextoReserva.cs y ReservaRepository.cs
    /// </summary>
    public interface IReservaRepository
    {
        /// <summary>
        /// Crea una nueva reserva en la base de datos
        /// </summary>
        Task<Reserva.Domain.Entidades.Reserva> CreateAsync(Reserva.Domain.Entidades.Reserva reserva);
        
        /// <summary>
        /// Obtiene una reserva por su ID (sincronizado con columna "idReserva")
        /// </summary>
        Task<Reserva.Domain.Entidades.Reserva?> GetAsync(int id);
        
        /// <summary>
        /// Intenta reservar un asiento específico para una reserva
        /// Usa bloqueo FOR UPDATE en tabla "Asientos" sincronizado con SQL
        /// </summary>
        Task<bool> TryReserveAsync(int asientoId, int reservaId);
        
        /// <summary>
        /// Expira una reserva (marca como cancelada - estado 3)
        /// Sincronizado con estado "cancelada" en tabla ReservaEstados
        /// </summary>
        Task ExpireAsync(int reservaId);
        
        /// <summary>
        /// Cancela una reserva y libera el asiento
        /// Sincronizado con estados en AsientoEstados y ReservaEstados
        /// </summary>
        Task<bool> CancelAsync(int reservaId);
        
        /// <summary>
        /// Obtiene un asiento por su ID (sincronizado con columna "idAsiento")
        /// </summary>
        Task<Reserva.Domain.Entidades.Asiento?> GetAsientoAsync(int id);
        
        /// <summary>
        /// Obtiene un evento por su ID (sincronizado con columna "idEvento")
        /// </summary>
        Task<Reserva.Domain.Entidades.Evento?> GetEventoAsync(int id);
    }
}