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
        private readonly IReservaSingletonService _singleton;

        private readonly Reserva.Domain.Interfaces.INotificationService? _notifier;

        public ReservaRepository(ContextoReserva db, IReservaSingletonService singleton, Reserva.Domain.Interfaces.INotificationService? notifier = null)
        {
            _db = db;
            _singleton = singleton;
            _notifier = notifier;
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

        public async Task<Reserva.Domain.Entidades.Asiento?> GetAsientoAsync(int id)
        {
            return await _db.Asientos.FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Reserva.Domain.Entidades.Evento?> GetEventoAsync(int id)
        {
            return await _db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<bool> TryReserveAsync(int asientoId, int reservaId)
        {
            // Ejecutar sección crítica en el singleton para evitar colisiones en proceso.
            return await _singleton.ExecuteAsync(async () =>
            {
                // Iniciar transacción explícita
                await using var tx = await _db.Database.BeginTransactionAsync();

                // Obtener la reserva para conocer el estadio esperado
                var reserva = await _db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId);
                if (reserva == null)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                // Usar SELECT ... FOR UPDATE para bloquear el registro a nivel DB (Postgres)
                // FromSqlRaw devolverá la entidad Asiento rastreada por el DbContext.
                var asiento = await _db.Asientos
                    .FromSqlRaw("SELECT * FROM asiento WHERE \"idAsiento\" = {0} FOR UPDATE", asientoId)
                    .FirstOrDefaultAsync();

                // Validaciones transaccionales: existencia, estado disponible y pertenencia al estadio
                if (asiento == null || asiento.EstadoId != 1 || asiento.EstadioId != reserva.EstadioId)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                // Marcar como no disponible (estado 2)
                asiento.EstadoId = 2;

                try
                {
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Conflicto de concurrencia: otro proceso actualizó el registro
                    await tx.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task ExpireAsync(int reservaId)
        {
            var r = await _db.Reservas.FirstOrDefaultAsync(x => x.Id == reservaId);
            if (r == null) return;
            // La tabla de estados de reserva contiene: aprobado(1), espera(2), cancelada(3)
            // Como 'Expirado' no existe en el catálogo original, marcamos como 'cancelada' (3).
            r.EstadoId = 3;
            await _db.SaveChangesAsync();
        }

        public async Task<bool> CancelAsync(int reservaId)
        {
            // Serializar en proceso y usar transacción
            return await _singleton.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                var reserva = await _db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId);
                if (reserva == null)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                // Marcar reserva como cancelada
                reserva.EstadoId = 3;

                // Liberar asiento
                var asiento = await _db.Asientos.FirstOrDefaultAsync(a => a.Id == reserva.AsientoId);
                if (asiento != null)
                {
                    asiento.EstadoId = 1; // Disponible
                }

                try
                {
                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    // Emitir notificación si hay un servicio configurado
                    if (_notifier != null)
                    {
                        var evt = new Reserva.Domain.Patrones.Observer.ReservaCanceladaEvent { ReservaId = reservaId };
                        await _notifier.EnqueueAsync(evt);
                    }

                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            });
        }
    }
}
