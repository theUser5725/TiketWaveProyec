using System;

namespace Reserva.Domain.Entidades
{
    public class Notificacion
    {
        public int Id { get; set; }
        public int ReservaId { get; set; }
        public int UsuarioId { get; set; }
        public string Tipo { get; set; } = null!;
        public string Mensaje { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}