using Dune.Domain.Models;

namespace Dune.Domain.Interfaces;

public interface IPersistenceService
{
    Task<(bool Success, string Message)> GuardarPartidaAsync(Partida partida, string ruta);
    Task<(Partida? Partida, string Message)> CargarPartidaAsync(string ruta);
    Task<List<string>> ListarPartidasAsync(string directorio);
}
