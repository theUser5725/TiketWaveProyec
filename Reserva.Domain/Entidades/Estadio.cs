using System.Collections.Generic;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Estadio: mapea la tabla `estadio`.
    /// Columnas: idEstadio (PK), cantidad_asientos
    /// </summary>
    public class Estadio
    {
        public int Id { get; set; }
        public int CantidadAsientos { get; set; }

        // Navegación opcional a los asientos del estadio
        public List<Asiento>? Asientos { get; set; }
    }
}
