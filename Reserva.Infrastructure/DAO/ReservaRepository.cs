using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Interfaces;
using Reserva.Domain.Entidades;
using Reserva.Infrastructure.Persistencia;

namespace Reserva.Infrastructure.DAO
{
    /// <summary>
    /// Repositorio principal para gestionar reservas y asientos.
    /// Implementa la lógica de persistencia y control de concurrencia.
    /// 
    /// Características principales:
    /// - Transacciones atómicas para reserva/cancelación
    /// - Control de concurrencia mediante bloqueos explícitos (SELECT FOR UPDATE)
    /// - Notificaciones asíncronas de eventos (creación/cancelación)
    /// - Singleton para serializar operaciones críticas
    /// 
    /// IMPORTANTE: Las constantes de estado deben mantenerse sincronizadas con:
    /// - scripts/init.sql (datos semilla)
    /// - ContextoReserva.cs (HasData en OnModelCreating)
    /// </summary>
    public class ReservaRepository : IReservaRepository
    {
        private readonly ContextoReserva _db;
        private readonly IReservaSingletonService _singleton;
        private readonly Reserva.Domain.Interfaces.INotificationService? _notifier;

        // =============================================
        // CONSTANTES SINCRONIZADAS CON SQL
        // =============================================
        
        // Estados de Asiento (sincronizado con scripts/init.sql)
        private const int ASIENTO_DISPONIBLE = 1;    // 'Disponible' en AsientoEstados
        private const int ASIENTO_NO_DISPONIBLE = 2; // 'No Disponible' en AsientoEstados
        
        // Estados de Reserva (sincronizado con scripts/init.sql y HasData)
        private const int RESERVA_APROBADO = 1;      // 'aprobado' en ReservaEstados  
        private const int RESERVA_ESPERA = 2;        // 'espera' en ReservaEstados
        private const int RESERVA_CANCELADA = 3;     // 'cancelada' en ReservaEstados

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

                // =============================================
                // CORREGIDO: Usar nombre correcto de tabla "Asientos"
                // Sincronizado con scripts/init.sql
                // =============================================
                var asiento = await _db.Asientos
                    .FromSqlRaw("SELECT * FROM \"Asientos\" WHERE \"idAsiento\" = {0} FOR UPDATE", asientoId)
                    .FirstOrDefaultAsync();

                // =============================================
                // CORREGIDO: Usar constantes sincronizadas con SQL
                // =============================================
                if (asiento == null || asiento.EstadoId != ASIENTO_DISPONIBLE || asiento.EstadioId != reserva.EstadioId)
                {
                    await tx.RollbackAsync();
                    return false;
                }

                // Marcar como no disponible usando constante sincronizada
                asiento.EstadoId = ASIENTO_NO_DISPONIBLE;

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
            var reserva = await _db.Reservas.FirstOrDefaultAsync(x => x.Id == reservaId);
            if (reserva == null) return;
            
            // =============================================
            // CORREGIDO: Usar constante sincronizada
            // 'Expirado' no existe en catálogo, usar 'cancelada'
            // =============================================
            reserva.EstadoId = RESERVA_CANCELADA;
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

                // =============================================
                // CORREGIDO: Usar constantes sincronizadas
                // =============================================
                
                // Marcar reserva como cancelada
                reserva.EstadoId = RESERVA_CANCELADA;

                // Liberar asiento (marcar como disponible)
                var asiento = await _db.Asientos.FirstOrDefaultAsync(a => a.Id == reserva.AsientoId);
                if (asiento != null)
                {
                    asiento.EstadoId = ASIENTO_DISPONIBLE;
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

        // =============================================
        // MÉTODOS AUXILIARES PARA VERIFICACIÓN
        // =============================================

        /// <summary>
        /// Verifica que los estados en la base de datos coincidan con las constantes
        /// </summary>
        public async Task<bool> VerifyEstadoConsistencyAsync()
        {
            try
            {
                var asientoDisponible = await _db.AsientoEstados
                    .FirstOrDefaultAsync(a => a.Id == ASIENTO_DISPONIBLE && a.Nombre == "Disponible");
                
                var asientoNoDisponible = await _db.AsientoEstados
                    .FirstOrDefaultAsync(a => a.Id == ASIENTO_NO_DISPONIBLE && a.Nombre == "No Disponible");
                
                var reservaAprobado = await _db.ReservaEstados
                    .FirstOrDefaultAsync(r => r.Id == RESERVA_APROBADO && r.Nombre == "aprobado");
                
                var reservaCancelada = await _db.ReservaEstados
                    .FirstOrDefaultAsync(r => r.Id == RESERVA_CANCELADA && r.Nombre == "cancelada");

                return asientoDisponible != null && 
                       asientoNoDisponible != null && 
                       reservaAprobado != null && 
                       reservaCancelada != null;
            }
            catch
            {
                return false;
            }
        }
    }
}