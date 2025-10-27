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
            services.AddDbContext<ContextoReserva>(options =>
                options.UseNpgsql(cs));

            // Cache/Redis service (implementación en Infraestructura)
            services.AddScoped<ICacheService, RedisCacheService>();

            // Repositorio DAO (único punto de acceso a inventario)
            services.AddScoped<IReservaRepository, ReservaRepository>();

            // Health checks - básico
            services.AddHealthChecks();

            // Aquí podrías añadir MediatR, Polly, HttpClients, etc.

            return services;
        }
    }
}
