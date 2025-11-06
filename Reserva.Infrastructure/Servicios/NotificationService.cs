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
    /// Sincronizado con la nueva entidad Notificacion sin navegaciones opcionales.
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
                        
                        // =============================================
                        // Ajuste: usar la entidad `Notificacion` sin propiedades de navegación.
                        // Mapeo y seed sincronizados con `scripts/init.sql`.
                        // =============================================
                        var notificacion = new Reserva.Domain.Entidades.Notificacion
                        {
                            ReservaId = evt.ReservaId,
                            
                            Tipo = evt.Tipo,
                            Mensaje = evt.Datos ?? evt.Tipo,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        db.Notificaciones.Add(notificacion);
                        await db.SaveChangesAsync();
                        
                        _logger.LogInformation("Notificación guardada: ReservaId={ReservaId}, Tipo={Tipo}", 
                            evt.ReservaId, evt.Tipo);
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Error de base de datos al guardar notificación para ReservaId={ReservaId}", evt.ReservaId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando notificación {EventId}", evt.EventoId);
                    }
                }
            }
            catch (OperationCanceledException) 
            {
                _logger.LogInformation("Procesamiento de notificaciones cancelado");
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _queue.CompleteAdding();
            try 
            { 
                _worker.Wait(2000); 
            } 
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException) 
            {
                _logger.LogInformation("Worker de notificaciones terminado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al esperar que el worker termine");
            }
            finally
            {
                _cts.Dispose();
                _queue.Dispose();
            }
        }
    }
}