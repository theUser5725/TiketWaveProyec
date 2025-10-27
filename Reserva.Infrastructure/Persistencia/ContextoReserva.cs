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

        public DbSet<Reserva.Domain.Entidades.Reserva> Reservas { get; set; } = null!;
        public DbSet<Reserva.Domain.Entidades.Asiento> Asientos { get; set; } = null!;

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
        }
    }
}
