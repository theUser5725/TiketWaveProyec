using System.Collections.Generic;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Asiento: mapea la tabla `asiento`.
    /// Columnas relevantes: idAsiento (PK), estado (FK a asientoEstado), idEstadio (FK a estadio).
    /// </summary>
    public class Asiento
    {
        // PK identity
        public int Id { get; set; }


        // FK al estado del asiento (asientoEstado.idEstadoAsiento)
        public int EstadoId { get; set; }
        // Navegación renombrada para compatibilidad y evitar colisiones con propiedades legacy.
        public AsientoEstado? EstadoEntity { get; set; }

        // Propiedad legacy: algunos lugares del código tratan `Asiento.Estado` como integer.
        // Proporcionamos un alias que mapea a EstadoId para compatibilidad.
        public int Estado
        {
            get => EstadoId;
            set => EstadoId = value;
        }

        // FK al estadio que contiene el asiento
        public int EstadioId { get; set; }
        public Estadio? Estadio { get; set; }

    // Concurrency token opcional (para EF Core)
    public byte[]? RowVersion { get; set; }
    }
}
