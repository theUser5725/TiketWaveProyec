using System;

namespace Reserva.Domain.Patrones.Observer
{
    public class ReservaCreadaEvent : IEventoReserva
    {
        public Guid EventoId { get; } = Guid.NewGuid();
        public int ReservaId { get; set; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public string Tipo => "ReservaCreada";
        public string? Datos { get; set; }
    }
}
