namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Usuario: mapea la tabla "Usuarios".
    /// Sincronizado con SQL: "idUsuario", "Nombre", "Apellido"
    /// </summary>
    public class Usuario
    {
        // PK - Sincronizado con SQL: "idUsuario"
        public int Id { get; set; }
        
        // Sincronizado con SQL: "Nombre"
        public string Nombre { get; set; } = string.Empty;
        
        // Sincronizado con SQL: "Apellido"
        public string Apellido { get; set; } = string.Empty;

        public string NombreCompleto => string.IsNullOrWhiteSpace(Apellido) ? Nombre : $"{Nombre} {Apellido}";
        
        // Navegación a reservas del usuario
        public List<Reserva>? Reservas { get; set; }
        
        // Navegación a notificaciones del usuario
        public List<Notificacion>? Notificaciones { get; set; }
    }
}