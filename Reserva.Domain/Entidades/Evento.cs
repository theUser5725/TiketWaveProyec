using System;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Evento: mapea la tabla "Eventos".
    /// Sincronizado con SQL: "idEvento", "nombre", "fechaInicio", "idEstadio"
    /// </summary>
    public class Evento
    {
        // PK - Sincronizado con SQL: "idEvento"
        public int Id { get; set; }

        // Sincronizado con SQL: "nombre"
        public string Nombre { get; set; } = string.Empty;

        // Sincronizado con SQL: "fechaInicio"
        public DateTime FechaInicio { get; set; }

        // FK al estadio - Sincronizado con SQL: "idEstadio" (referencia a "Estadios"."idEstadio")
        public int EstadioId { get; set; }
        public Estadio? Estadio { get; set; }
    }
}