namespace Dune.Domain.Models;

public enum TipoInstalacion
{
    Aclimatacion,
    Exhibicion
}

public enum TipoRecinto
{
    RocaSellada,
    EscudoEstatico,
    CupulaBlindada,
    PozoReforzado
}

public class Instalacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public TipoInstalacion Tipo { get; set; }
    public int CosteConstruccion { get; set; }
    public string Medio { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int AlimentacionInicial { get; set; }
    public int SuministrosIniciales { get; set; }
    public int Capacidad { get; set; }
    public int Hectareas { get; set; }
    public TipoRecinto Recinto { get; set; }
    public int Reservas { get; set; }
    public Guid? EnclaveId { get; set; } = null;
}
