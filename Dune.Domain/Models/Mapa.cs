namespace Dune.Domain.Models;

public class Mapa
{
    public int Ancho { get; set; } = 20;
    public int Alto { get; set; } = 20;
    public List<List<TipoTerreno>> Celdas { get; set; } = new();
}

public enum TipoTerreno
{
    Desierto,
    Roca,
    ArenaMovida,
    Melange,
    EspeciaRosa,
    EspeciaNegra
}
