namespace Dune.Domain.Models;

public class Ronda
{
    public int Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public List<EventoSimulacion> Eventos { get; set; } = new();
}

public class EventoSimulacion
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Descripcion { get; set; } = string.Empty;
    public TipoEvento Tipo { get; set; }
    public string? EntidadId { get; set; } = null;
}

public enum TipoEvento
{
    CriaturaMovio,
    CriaturaAtaco,
    RecursoExtraido,
    EnclaveAtacado,
    InstalacionConstruida,
    RondaCompletada
}
