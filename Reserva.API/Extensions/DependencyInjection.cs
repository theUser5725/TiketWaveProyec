using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reserva.Domain.Interfaces;
using Reserva.Infrastructure.DAO;
using Reserva.Infrastructure.Persistencia;
using Reserva.Infrastructure.Servicios;

namespace Reserva.API.Extensions
{
    /// <summary>
    /// Extensiones para centralizar la configuración de dependencias de Reserva.API.
    /// Registrar aquí DbContext, repositorios, cache, MediatR, Polly, etc.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddReservaServices(this IServiceCollection services, IConfiguration config)
        {
            // Configurar DbContext de EF Core (PostgreSQL)
            var cs = config.GetConnectionString("ReservaPostgres") ?? "Host=localhost;Database=ReservaDb;Username=postgres;Password=postgres";
            // Registrar DbContext: la factory no se registra aquí para evitar problemas de validación
            // durante la construcción del provider. Si se necesita crear DbContext desde un singleton,
            // se usará `IServiceScopeFactory` para crear un scope y resolver `ContextoReserva`.
            services.AddDbContext<ContextoReserva>(options =>
                options.UseNpgsql(cs));

            // Cache/Redis service (implementación en Infraestructura)
            services.AddScoped<ICacheService, RedisCacheService>();

            // Repositorio DAO (único punto de acceso a inventario)
            services.AddScoped<IReservaRepository, ReservaRepository>();

            // Servicio singleton para serializar secciones críticas en proceso
            services.AddSingleton<Reserva.Domain.Interfaces.IReservaSingletonService, ReservaSingletonService>();

            // Servicio de notificaciones (procesamiento en background)
            services.AddSingleton<Reserva.Domain.Interfaces.INotificationService, Reserva.Infrastructure.Servicios.NotificationService>();

            // Health checks - básico
            services.AddHealthChecks();

            // Aquí podrías añadir MediatR, Polly, HttpClients, etc.

            return services;
        }
    }
}
