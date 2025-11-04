using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Reserva.API.Extensions;
using Reserva.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configuración básica de logging con Serilog (opcional: ajustar sinks en appsettings)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Registrar servicios de la aplicación (DbContext, repositorios, cache, MediatR, etc.)
// La extensión AddReservaServices se define en Reserva.API/Extensions/DependencyInjection.cs
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddReservaServices(builder.Configuration);

var app = builder.Build();

// Middleware y pipeline mínimo
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

// Proxy/validaciones antes de procesar (placeholder)
app.UseMiddleware<ProxyValidationMiddleware>();

app.MapControllers();

app.Run();
