using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Interfaces;
using Reserva.Domain.Entidades;
using Reserva.Infrastructure.Persistencia;

namespace Reserva.Infrastructure.DAO
{
    /// <summary>
    /// Implementación concreta del DAO. Único punto de acceso para modificar inventario.
    /// Importante: aquí se deben implementar transacciones, control de concurrencia y locks distribuidos.
    /// </summary>
    public class ReservaRepository : IReservaRepository
    {
        private readonly ContextoReserva _db;

        public ReservaRepository(ContextoReserva db)
        {
            _db = db;
        }

        public async Task<Reserva.Domain.Entidades.Reserva> CreateAsync(Reserva.Domain.Entidades.Reserva reserva)
        {
            _db.Reservas.Add(reserva);
            await _db.SaveChangesAsync();
            return reserva;
        }

        public async Task<Reserva.Domain.Entidades.Reserva?> GetAsync(int id)
        {
            return await _db.Reservas.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> TryReserveAsync(int asientoId, int reservaId)
        {
            // Placeholder: implementar locking distribuido (RedLock) o SELECT ... FOR UPDATE
            var asiento = await _db.Asientos.FirstOrDefaultAsync(a => a.Id == asientoId);
            if (asiento == null || !asiento.Disponible) return false;

            asiento.Disponible = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task ExpireAsync(int reservaId)
        {
            var r = await _db.Reservas.FirstOrDefaultAsync(x => x.Id == reservaId);
            if (r == null) return;
            r.Estado = "Expirado";
            await _db.SaveChangesAsync();
        }
    }
}
