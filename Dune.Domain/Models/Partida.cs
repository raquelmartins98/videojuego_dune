namespace Dune.Domain.Models;

public class Partida
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime UltimoGuardado { get; set; } = DateTime.Now;
    public int RondaActual { get; set; } = 0;
    public Mapa Mapa { get; set; } = new();
    public List<Criatura> Criaturas { get; set; } = new();
    public List<Enclave> Enclaves { get; set; } = new();
    public List<Instalacion> Instalaciones { get; set; } = new();
    public List<Recurso> Recursos { get; set; } = new();
    public List<Ronda> HistorialRondas { get; set; } = new();
    public Inventario InventarioGlobal { get; set; } = new();
    public AlmacenGeneral Almacen { get; set; } = new();
}

public class Inventario
{
    public int Especia { get; set; }
    public int Agua { get; set; }
    public int Materiales { get; set; }
    public int Energia { get; set; }
}

public class AlmacenGeneral
{
    public int UnidadesSuministro { get; set; }
    public const int CosteUnidadFijo = 5;
    
    public int CapacidadMaxima(Enclave enclave) => enclave.Hectareas * 3;
    
    public bool ComprarUnidades(int cantidad, Inventario inventario)
    {
        if (cantidad <= 0) return false;
        int costeTotal = cantidad * CosteUnidadFijo;
        if (costeTotal > inventario.Especia) return false;
        
        inventario.Especia -= costeTotal;
        UnidadesSuministro += cantidad;
        return true;
    }
    
    public bool MoverAInstalacion(Instalacion instalacion, int cantidad)
    {
        if (cantidad <= 0 || cantidad > UnidadesSuministro) return false;
        if (instalacion == null) return false;
        
        int maximoPermitido = instalacion.CosteConstruccion;
        if (instalacion.Reservas + cantidad > maximoPermitido) return false;
        
        UnidadesSuministro -= cantidad;
        instalacion.Reservas += cantidad;
        return true;
    }
}
