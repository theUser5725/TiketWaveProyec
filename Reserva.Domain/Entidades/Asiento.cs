using System.Collections.Generic;

namespace Reserva.Domain.Entidades
{
    public class Asiento
    {
        public int Id { get; set; }
        public int EstadoId { get; set; }      // ← SOLO el FK
        public int EstadioId { get; set; }
    public Estadio? Estadio { get; set; }
    }
}