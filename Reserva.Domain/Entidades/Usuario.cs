namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Usuario: mapea la tabla `usuario`.
    /// Columnas: idUsuario (PK), nombre, apellido
    /// </summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;

        public string NombreCompleto => string.IsNullOrWhiteSpace(Apellido) ? Nombre : $"{Nombre} {Apellido}";
    }
}
