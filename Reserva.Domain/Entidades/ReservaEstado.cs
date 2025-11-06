namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad ReservaEstado: mapea la tabla "ReservaEstados".
    /// Sincronizado con SQL: "idEstadoReserva", "nombre"
    /// </summary>
    public class ReservaEstado
    {
        // PK - Sincronizado con SQL: "idEstadoReserva"
        public int Id { get; set; }
        
        // Sincronizado con SQL: "nombre"
        public string Nombre { get; set; } = string.Empty;
    }
}