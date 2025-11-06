using System;

namespace Reserva.Domain.Entidades
{
    public class Reserva
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public int AsientoId { get; set; }
        public Asiento? Asiento { get; set; }
        public int EstadioId { get; set; }
        public Estadio? Estadio { get; set; }
        public int EstadoId { get; set; }      // ← SOLO el FK
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}