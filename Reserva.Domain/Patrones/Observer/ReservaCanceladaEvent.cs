using System;

namespace Reserva.Domain.Patrones.Observer
{
    public class ReservaCanceladaEvent : IEventoReserva
    {
        public Guid EventoId { get; } = Guid.NewGuid();
        public int ReservaId { get; set; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public string Tipo => "ReservaCancelada";
        public string? Datos { get; set; }
    }
}
