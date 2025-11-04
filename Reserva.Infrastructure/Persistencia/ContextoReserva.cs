using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Entidades;

namespace Reserva.Infrastructure.Persistencia
{
    /// <summary>
    /// DbContext de EF Core para la persistencia de reservas y asientos.
    /// Contiene las DbSets y la configuración mínima.
    /// </summary>
    public class ContextoReserva : DbContext
    {
        public ContextoReserva(DbContextOptions<ContextoReserva> options) : base(options)
        {
        }

        // DbSets para todas las tablas del esquema proporcionado
        public DbSet<Reserva.Domain.Entidades.Reserva> Reservas { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Asiento> Asientos { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Usuario> Usuarios { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Estadio> Estadios { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.AsientoEstado> AsientoEstados { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.ReservaEstado> ReservaEstados { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones sugeridas: RowVersion para concurrencia optimista
            modelBuilder.Entity<Reserva.Domain.Entidades.Reserva>()
                .Property<byte[]?>("RowVersion")
                .IsRowVersion();

            modelBuilder.Entity<Reserva.Domain.Entidades.Asiento>()
                .Property<byte[]?>("RowVersion")
                .IsRowVersion();

            // Configurar que la PK int sea manejada como Identity por la base de datos.
            // Esto es compatible con PostgreSQL (serial/identity) cuando EF Core aplica migraciones.
            modelBuilder.Entity<Reserva.Domain.Entidades.Reserva>()
                .Property<int>("Id")
                .UseIdentityColumn();

            modelBuilder.Entity<Reserva.Domain.Entidades.Asiento>()
                .Property<int>("Id")
                .UseIdentityColumn();

            // Mappings: nombres de tablas/columnas para coincidir con el esquema SQL proporcionado
            modelBuilder.Entity<Reserva.Domain.Entidades.Asiento>(b =>
            {
                b.ToTable("asiento");
                b.Property(p => p.Id).HasColumnName("idAsiento");
                b.Property(p => p.EstadoId).HasColumnName("estado");
                b.Property(p => p.EstadioId).HasColumnName("idEstadio");
            });

            modelBuilder.Entity<Reserva.Domain.Entidades.Reserva>(b =>
            {
                b.ToTable("reserva");
                b.Property(p => p.Id).HasColumnName("idReserva");
                b.Property(p => p.UsuarioId).HasColumnName("idUsuario");
                b.Property(p => p.AsientoId).HasColumnName("idasiento");
                b.Property(p => p.EstadioId).HasColumnName("idestadio");
                b.Property(p => p.EstadoId).HasColumnName("estado");
                b.Property(p => p.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Reserva.Domain.Entidades.Usuario>(b =>
            {
                b.ToTable("usuario");
                b.Property(p => p.Id).HasColumnName("idUsuario");
            });

            modelBuilder.Entity<Reserva.Domain.Entidades.Estadio>(b =>
            {
                b.ToTable("estadio");
                b.Property(p => p.Id).HasColumnName("idEstadio");
                b.Property(p => p.CantidadAsientos).HasColumnName("cantidad_asientos");
            });

            modelBuilder.Entity<Reserva.Domain.Entidades.AsientoEstado>(b =>
            {
                b.ToTable("asientoEstado");
                b.Property(p => p.Id).HasColumnName("idEstadoAsiento");
                b.Property(p => p.Nombre).HasColumnName("nombre");
            });

            modelBuilder.Entity<Reserva.Domain.Entidades.ReservaEstado>(b =>
            {
                b.ToTable("reservaEstado");
                b.Property(p => p.Id).HasColumnName("idEstadoReserva");
                b.Property(p => p.Nombre).HasColumnName("nombre");
            });

            // Seed básico para los estados (coincide con la querry suministrada)
            modelBuilder.Entity<Reserva.Domain.Entidades.AsientoEstado>().HasData(
                new Reserva.Domain.Entidades.AsientoEstado { Id = 1, Nombre = "Disponible" },
                new Reserva.Domain.Entidades.AsientoEstado { Id = 2, Nombre = "No Disponible" }
            );

            modelBuilder.Entity<Reserva.Domain.Entidades.ReservaEstado>().HasData(
                new Reserva.Domain.Entidades.ReservaEstado { Id = 1, Nombre = "aprobado" },
                new Reserva.Domain.Entidades.ReservaEstado { Id = 2, Nombre = "espera" },
                new Reserva.Domain.Entidades.ReservaEstado { Id = 3, Nombre = "cancelada" }
            );
        }
    }
}
