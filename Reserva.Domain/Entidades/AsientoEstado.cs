namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad AsientoEstado: mapea la tabla "AsientoEstados".
    /// Sincronizado con SQL: "idEstadoAsiento", "nombre"
    /// </summary>
    public class AsientoEstado
    {
        // PK - Sincronizado con SQL: "idEstadoAsiento"
        public int Id { get; set; }
        
        // Sincronizado con SQL: "nombre"
        public string Nombre { get; set; } = string.Empty;
    }
}