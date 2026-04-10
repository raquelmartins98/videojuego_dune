using Dune.Domain.Models;

namespace Dune.Domain.Interfaces;

public interface ISimulationService
{
    Ronda EjecutarRonda(Partida partida);
    void MoverCriatura(Partida partida, Guid criaturaId, int nuevoX, int nuevoY);
    void ProcesarCombate(Partida partida, Guid atacanteId, Guid objetivoId);
    void ExtraerRecurso(Partida partida, Guid recursoId);
}
