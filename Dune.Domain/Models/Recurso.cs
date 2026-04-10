namespace Dune.Domain.Models;

public enum TipoRecurso
{
    Especia,
    Agua,
    Materiales,
    Energia
}

public class Recurso
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TipoRecurso Tipo { get; set; }
    public int Cantidad { get; set; }
    public int PosicionX { get; set; }
    public int PosicionY { get; set; }
    public bool Extraido { get; set; }
}
