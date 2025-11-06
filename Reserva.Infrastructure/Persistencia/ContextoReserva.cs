using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entidades;

namespace Reserva.Infrastructure.Persistencia
{
    /// <summary>
    /// DbContext de EF Core para la persistencia de reservas y asientos.
    /// Proporciona acceso a datos y configuración del modelo para Entity Framework Core.
    /// 
    /// Características principales:
    /// - Mapeo de entidades a tablas PostgreSQL
    /// - Configuración de relaciones y claves foráneas
    /// - Datos semilla para estados de reserva y asiento
    /// - Convenciones de nombres de columna sincronizadas con scripts/init.sql
    /// 
    /// IMPORTANTE: Cualquier cambio en los mappings debe sincronizarse con scripts/init.sql
    /// </summary>
    public class ContextoReserva : DbContext
    {
        public ContextoReserva(DbContextOptions<ContextoReserva> options) : base(options)
        {
        }

        // =============================================
        // DbSets - TODAS LAS TABLAS DEL ESQUEMA SQL
        // =============================================

        public DbSet<Reserva.Domain.Entidades.Reserva> Reservas { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Asiento> Asientos { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Usuario> Usuarios { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Estadio> Estadios { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.AsientoEstado> AsientoEstados { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.ReservaEstado> ReservaEstados { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Evento> Eventos { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Notificacion> Notificaciones { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =============================================
            // CONFIGURACIONES GLOBALES
            // =============================================

            // Configurar PKs como Identity (compatible con PostgreSQL serial/autoincrement)
            // Cada tabla usa su propia secuencia para generar IDs únicos
            modelBuilder.Entity<Reserva.Domain.Entidades.Reserva>()
                .Property<int>("Id")
                .UseIdentityColumn();

            modelBuilder.Entity<Reserva.Domain.Entidades.Asiento>()
                .Property<int>("Id")
                .UseIdentityColumn();

            // =============================================
            // MAPPINGS - COMPLETAMENTE SINCRONIZADO CON SQL
            // =============================================

            // Tabla: Asientos
            modelBuilder.Entity<Reserva.Domain.Entidades.Asiento>(b =>
            {
                b.ToTable("Asientos");
                b.Property(p => p.Id).HasColumnName("idAsiento");         // ← Sincronizado con SQL
                b.Property(p => p.EstadoId).HasColumnName("estado");      // ← Sincronizado con SQL
                b.Property(p => p.EstadioId).HasColumnName("idEstadio");  // ← Sincronizado con SQL
            });

            // Tabla: Reservas (PRINCIPAL)
            modelBuilder.Entity<Reserva.Domain.Entidades.Reserva>(b =>
            {
                b.ToTable("Reservas");
                b.Property(p => p.Id).HasColumnName("idReserva");         // ← Sincronizado con SQL
                b.Property(p => p.UsuarioId).HasColumnName("idUsuario");  // ← Sincronizado con SQL
                b.Property(p => p.AsientoId).HasColumnName("idasiento");  // ← Sincronizado con SQL
                b.Property(p => p.EstadioId).HasColumnName("idestadio");  // ← Sincronizado con SQL
                b.Property(p => p.EstadoId).HasColumnName("estado");      // ← Sincronizado con SQL
                b.Property(p => p.CreatedAt).HasColumnName("created_at"); // ← Sincronizado con SQL
            });

            // Tabla: Eventos
            modelBuilder.Entity<Reserva.Domain.Entidades.Evento>(b =>
            {
                b.ToTable("Eventos");
                b.Property(p => p.Id).HasColumnName("idEvento");          // ← Sincronizado con SQL
                b.Property(p => p.Nombre).HasColumnName("nombre");        // ← Sincronizado con SQL
                b.Property(p => p.FechaInicio).HasColumnName("fechaInicio"); // ← Sincronizado con SQL
                b.Property(p => p.EstadioId).HasColumnName("idEstadio");  // ← Sincronizado con SQL
            });

            // Tabla: Usuarios
            modelBuilder.Entity<Reserva.Domain.Entidades.Usuario>(b =>
            {
                b.ToTable("Usuarios");
                b.Property(p => p.Id).HasColumnName("idUsuario");         // ← Sincronizado con SQL
                // Nota: Las columnas Nombre y Apellido se mapean automáticamente
            });

            // Tabla: Estadios
            modelBuilder.Entity<Reserva.Domain.Entidades.Estadio>(b =>
            {
                b.ToTable("Estadios");
                b.Property(p => p.Id).HasColumnName("idEstadio");         // ← Sincronizado con SQL
                b.Property(p => p.CantidadAsientos).HasColumnName("cantidad_asientos"); // ← Sincronizado con SQL
            });

            // Tabla: AsientoEstados
            modelBuilder.Entity<Reserva.Domain.Entidades.AsientoEstado>(b =>
            {
                b.ToTable("AsientoEstados");
                b.Property(p => p.Id).HasColumnName("idEstadoAsiento");   // ← Sincronizado con SQL
                b.Property(p => p.Nombre).HasColumnName("nombre");        // ← Sincronizado con SQL
            });

            // Tabla: ReservaEstados
            modelBuilder.Entity<Reserva.Domain.Entidades.ReservaEstado>(b =>
            {
                b.ToTable("ReservaEstados");
                b.Property(p => p.Id).HasColumnName("idEstadoReserva");   // ← Sincronizado con SQL
                b.Property(p => p.Nombre).HasColumnName("nombre");        // ← Sincronizado con SQL
            });

            // Tabla: Notificaciones
            modelBuilder.Entity<Reserva.Domain.Entidades.Notificacion>(b =>
            {
                b.ToTable("notificacion");
                b.Property(p => p.Id).HasColumnName("idNotificacion");    // ← Sincronizado con SQL
                b.Property(p => p.ReservaId).HasColumnName("idReserva");  // ← Sincronizado con SQL
                b.Property(p => p.UsuarioId).HasColumnName("idUsuario");  // ← Sincronizado con SQL
                b.Property(p => p.Tipo).HasColumnName("tipo");            // ← Sincronizado con SQL
                b.Property(p => p.Mensaje).HasColumnName("mensaje");      // ← Sincronizado con SQL
                b.Property(p => p.CreatedAt).HasColumnName("created_at"); // ← Sincronizado con SQL
            });

            // =============================================
            // DATOS INICIALES - SINCRONIZADO CON SQL
            // =============================================

            // Estados de Asiento
            modelBuilder.Entity<Reserva.Domain.Entidades.AsientoEstado>().HasData(
                new Reserva.Domain.Entidades.AsientoEstado { Id = 1, Nombre = "Disponible" },
                new Reserva.Domain.Entidades.AsientoEstado { Id = 2, Nombre = "No Disponible" }
            );

            // Estados de Reserva (en minúsculas para coincidir con SQL)
            modelBuilder.Entity<Reserva.Domain.Entidades.ReservaEstado>().HasData(
                new Reserva.Domain.Entidades.ReservaEstado { Id = 1, Nombre = "aprobado" },
                new Reserva.Domain.Entidades.ReservaEstado { Id = 2, Nombre = "espera" },
                new Reserva.Domain.Entidades.ReservaEstado { Id = 3, Nombre = "cancelada" }
            );
        }
    }
}