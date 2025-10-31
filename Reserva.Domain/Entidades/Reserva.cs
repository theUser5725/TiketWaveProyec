using System;
using System.Collections.Generic;
using System.Linq;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Reserva: representa una reserva de un asiento.
    /// Implementa el dominio mínimo; aplicar patrón State para gestionar el ciclo de vida.
    /// </summary>
    
    public class Reserva
    {
        // La PK ahora es responsabilidad de la base de datos (int identity)
        public int Id { get; set; }
        public int AsientoId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Estado del ciclo de vida: Disponible, Temporal, Pagado, Expirado
        public string Estado { get; set; } = "Temporal";

        // RowVersion/Concurrency token recomendado para EF Core
        public byte[]? RowVersion { get; set; }
    }
}
