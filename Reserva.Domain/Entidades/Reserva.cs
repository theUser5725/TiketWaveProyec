using System;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Reserva: representa una reserva de un asiento.
    /// Implementa el dominio mínimo; aplicar patrón State para gestionar el ciclo de vida.
    /// </summary>
    public class Reserva
    {
        public Guid Id { get; set; }
        public Guid AsientoId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Estado del ciclo de vida: Disponible, Temporal, Pagado, Expirado
        public string Estado { get; set; } = "Temporal";

        // RowVersion/Concurrency token recomendado para EF Core
        public byte[]? RowVersion { get; set; }
    }
}
