namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad ReservaEstado: mapea la tabla `reservaEstado`.
    /// Columnas: idEstadoReserva (PK), nombre (varchar)
    /// </summary>
    public class ReservaEstado
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
