using System;

namespace Reserva.Domain.Patrones.Observer
{
    public interface IEventoReserva
    {
        Guid EventoId { get; }
        Guid ReservaId { get; }
        DateTime Timestamp { get; }
        string Tipo { get; }
        string? Datos { get; }
    }
}
