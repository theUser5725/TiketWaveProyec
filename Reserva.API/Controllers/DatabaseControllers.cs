using Microsoft.AspNetCore.Mvc;
using Reserva.Domain.Entidades;
using Reserva.Infrastructure.Persistencia;

namespace Reserva.API.Controllers
{
    [ApiController]
    [Route("api/asiento")]
    public class AsientoController : ControllerBase
    {
        private readonly ContextoReserva _context;

        public AsientoController(ContextoReserva context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAsientos()
        {
            var asientos = _context.Asientos.ToList();
            return Ok(asientos);
        }

        [HttpPost]
        public IActionResult CreateAsiento([FromBody] Asiento asiento)
        {
            _context.Asientos.Add(asiento);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetAsientos), new { id = asiento.Id }, asiento);
        }
    }

    [ApiController]
    [Route("api/reserva-db")]
    public class ReservaDbController : ControllerBase
    {
        private readonly ContextoReserva _context;

        public ReservaDbController(ContextoReserva context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetReservas()
        {
            var reservas = _context.Reservas.ToList();
            return Ok(reservas);
        }

        [HttpPost]
        public IActionResult CreateReserva([FromBody] Reserva.Domain.Entidades.Reserva reserva)
        {
            _context.Reservas.Add(reserva);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetReservas), new { id = reserva.Id }, reserva);
        }
    }
}
