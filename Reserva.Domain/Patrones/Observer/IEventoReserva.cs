using System;

namespace Reserva.Domain.Patrones.Observer
{
    public interface IEventoReserva
    {
    Guid EventoId { get; }
    int ReservaId { get; }
        DateTime Timestamp { get; }
        string Tipo { get; }
        string? Datos { get; }
    }
}
