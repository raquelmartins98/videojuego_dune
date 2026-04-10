namespace Dune.Domain.Models;

public enum TipoEnclave
{
    Aclimatacion,
    Exhibicion
}

public enum NivelEnclave
{
    Alto,
    Medio,
    Bajo
}

public class Enclave
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public TipoEnclave Tipo { get; set; }
    public int Hectareas { get; set; }
    public int Capacidad { get; set; }
    public int HabitadoresIniciales { get; set; }
    public int VisitantesActuales { get; set; }
    public int VisitantesMesEnclave { get; set; }
    public int PrecioEntrada { get; set; }
    public int PrecioSalida { get; set; }
    public NivelEnclave Nivel { get; set; }
    public int PosicionX { get; set; }
    public int PosicionY { get; set; }
    public List<Guid> Instalaciones { get; set; } = new();
    
    public int HectareasInstalaciones(List<Instalacion> todasInstalaciones)
    {
        return todasInstalaciones
            .Where(i => i.EnclaveId == Id)
            .Sum(i => i.Hectareas);
    }
    
    public int VisitantesLlegan(List<Instalacion> todasInstalaciones, double saludMediaCriaturas)
    {
        if (Tipo != TipoEnclave.Exhibicion) return 0;
        
        int hectareasInst = HectareasInstalaciones(todasInstalaciones);
        if (hectareasInst == 0) hectareasInst = 1;
        
        int baseVisitantes = VisitantesMesEnclave / 20;
        int visitantesPorHectarea = (int)((double)VisitantesMesEnclave * hectareasInst / Hectareas * saludMediaCriaturas / 100.0);
        
        return Math.Max(baseVisitantes, visitantesPorHectarea);
    }
    
    public int VisitantesAbandonan(List<Instalacion> todasInstalaciones, double saludMediaCriaturas)
    {
        if (Tipo != TipoEnclave.Exhibicion) return 0;
        
        if (VisitantesActuales == 0) return 0;
        
        int hectareasInst = HectareasInstalaciones(todasInstalaciones);
        if (hectareasInst == 0) hectareasInst = 1;
        
        int baseAbandonos = Math.Max(1, VisitantesActuales / 20);
        int abandonosPorHectarea = (int)((double)VisitantesActuales * hectareasInst / Hectareas * (100 - saludMediaCriaturas) / 100.0);
        
        return Math.Min(VisitantesActuales, baseAbandonos + abandonosPorHectarea);
    }
    
    public void ActualizarVisitantes(List<Instalacion> todasInstalaciones, double saludMediaCriaturas)
    {
        if (Tipo != TipoEnclave.Exhibicion) return;
        
        int llegan = VisitantesLlegan(todasInstalaciones, saludMediaCriaturas);
        int abandonan = VisitantesAbandonan(todasInstalaciones, saludMediaCriaturas);
        
        int nuevoVisitantes = VisitantesActuales + llegan - abandonan;
        VisitantesActuales = Math.Max(VisitantesMesEnclave / 2, nuevoVisitantes);
    }
}
