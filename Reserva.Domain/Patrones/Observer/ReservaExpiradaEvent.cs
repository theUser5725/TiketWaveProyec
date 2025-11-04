using System;

namespace Reserva.Domain.Patrones.Observer
{
    public class ReservaExpiradaEvent : IEventoReserva
    {
    public Guid EventoId { get; } = Guid.NewGuid();
    public int ReservaId { get; set; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public string Tipo => "ReservaExpirada";
        public string? Datos { get; set; }
    }
}
