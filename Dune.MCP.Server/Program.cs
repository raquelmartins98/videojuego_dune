using Dune.Domain;
using Dune.Simulation.Service;
using Dune.Persistence.Service;
using Dune.Domain.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add game services
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<PersistenceService>();
builder.Services.AddSingleton<Partida>();

var app = builder.Build();

// MCP-like endpoints
app.MapPost("/tools/simulate_turn", async (HttpContext context) =>
{
    var simulation = context.RequestServices.GetRequiredService<SimulationService>();
    var gameState = context.RequestServices.GetRequiredService<Partida>();
    var ronda = simulation.EjecutarRonda(gameState);
    await context.Response.WriteAsJsonAsync(ronda);
});

app.MapPost("/tools/save_game", async (HttpContext context) =>
{
    var persistence = context.RequestServices.GetRequiredService<PersistenceService>();
    var gameState = context.RequestServices.GetRequiredService<Partida>();
    var ruta = "game.json"; // Default path
    var result = await persistence.GuardarPartidaAsync(gameState, ruta);
    await context.Response.WriteAsJsonAsync(new { success = result.Success, message = result.Message });
});

app.MapPost("/tools/load_game", async (HttpContext context) =>
{
    var persistence = context.RequestServices.GetRequiredService<PersistenceService>();
    var ruta = context.Request.Query["ruta"].ToString() ?? "game.json";
    var result = await persistence.CargarPartidaAsync(ruta);
    await context.Response.WriteAsJsonAsync(new { partida = result.Partida, message = result.Message });
});

app.Run();
