using System.Collections.Generic;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Estadio: mapea la tabla "Estadios".
    /// Sincronizado con SQL: "idEstadio", "cantidad_asientos"
    /// </summary>
    public class Estadio
    {
        // PK - Sincronizado con SQL: "idEstadio"
        public int Id { get; set; }
        
        // Sincronizado con SQL: "cantidad_asientos"
        public int CantidadAsientos { get; set; }

        // Navegación opcional a los asientos del estadio
        public List<Asiento>? Asientos { get; set; }
        
        // Navegación opcional a los eventos del estadio
        public List<Evento>? Eventos { get; set; }
    }
}