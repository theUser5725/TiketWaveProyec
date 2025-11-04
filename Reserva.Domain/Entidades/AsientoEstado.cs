namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad AsientoEstado: mapea la tabla `asientoEstado`.
    /// Columnas: idEstadoAsiento (PK), nombre (varchar)
    /// </summary>
    public class AsientoEstado
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
