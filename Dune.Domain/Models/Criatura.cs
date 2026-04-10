namespace Dune.Domain.Models;

public enum TipoCriatura
{
    GusanoArenaJuvenil,
    TigreLaza,
    MuadDib,
    HalconDesierto,
    TruchaArena
}

public enum MedioCriatura
{
    Subterraneo,
    Desierto,
    Aereo
}

public enum RolCriatura
{
    Depredador,
    Recolector
}

public class Criatura
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public TipoCriatura Tipo { get; set; }
    public MedioCriatura Medio { get; set; }
    public RolCriatura Rol { get; set; }
    public int EdadAdulta { get; set; }
    public int ApetitoBase { get; set; }
    public int Vida { get; set; }
    public int VidaMaxima { get; set; }
    public int Ataque { get; set; }
    public int PosicionX { get; set; }
    public int PosicionY { get; set; }
    public bool Activo { get; set; } = true;
}
