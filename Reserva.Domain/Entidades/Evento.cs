using System;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Evento: mapea la tabla `evento`.
    /// Columnas: idEvento (PK), nombre, fechaInicio, idEstadio (FK)
    /// </summary>
    public class Evento
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; }

        public int EstadioId { get; set; }
        public Estadio? Estadio { get; set; }
    }
}
