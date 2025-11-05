using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Interfaces;
using Reserva.Domain.Patrones.Observer;
using Reserva.Infrastructure.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace Reserva.Infrastructure.Servicios
{
    /// <summary>
    /// Servicio de notificaciones simple que encola eventos y los persiste en la tabla
    /// `notificacion`. Implementación por defecto en memoria con procesamiento en segundo plano.
    /// Usa DbContextFactory para crear contextos por unidad de trabajo.
    /// </summary>
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationService> _logger;
        private readonly BlockingCollection<IEventoReserva> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;

        public NotificationService(IServiceScopeFactory scopeFactory, ILogger<NotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _worker = Task.Run(ProcessQueueAsync);
        }

        public Task EnqueueAsync(IEventoReserva evt)
        {
            _queue.Add(evt);
            return Task.CompletedTask;
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                foreach (var evt in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ContextoReserva>();
                        var not = new Reserva.Domain.Entidades.Notificacion
                        {
                            ReservaId = evt.ReservaId,
                            UsuarioId = null,
                            Tipo = evt.Tipo,
                            Mensaje = evt.Datos ?? evt.Tipo,
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Notificaciones.Add(not);
                        await db.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando notificación {EventId}", evt.EventoId);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _queue.CompleteAdding();
            try { _worker.Wait(2000); } catch { }
            _cts.Dispose();
            _queue.Dispose();
        }
    }
}
