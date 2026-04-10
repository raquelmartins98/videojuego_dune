using Dune.Domain.Interfaces;
using Dune.Domain.Models;

namespace Dune.Simulation.Service;

public class SimulationService : ISimulationService
{
    private readonly Random _random = Random.Shared;

    public Ronda EjecutarRonda(Partida partida)
    {
        var ronda = new Ronda { Numero = partida.RondaActual + 1 };

        try
        {
            partida.RondaActual = ronda.Numero;

            foreach (var criatura in partida.Criaturas.Where(c => c.Activo).ToList())
            {
                try
                {
                    var evento = SimularComportamientoCriatura(partida, criatura);
                    if (evento != null)
                        ronda.Eventos.Add(evento);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error con criatura: {ex.Message}");
                }
            }

            foreach (var recurso in partida.Recursos.Where(r => !r.Extraido).ToList())
            {
                try
                {
                    if (recurso.Tipo == TipoRecurso.Especia && _random.Next(100) < 10)
                    {
                        recurso.Cantidad += 10;
                        ronda.Eventos.Add(new EventoSimulacion
                        {
                            Tipo = TipoEvento.RecursoExtraido,
                            Descripcion = $"Especia regenero +10 unidades"
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error con recurso: {ex.Message}");
                }
            }

            SimularInstalacionesAclimatacion(partida, ronda);
            SimularVisitantesEnclaves(partida, ronda);

            partida.HistorialRondas.Add(ronda);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en EjecutarRonda: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }

        return ronda;
    }

    private void SimularInstalacionesAclimatacion(Partida partida, Ronda ronda)
    {
        foreach (var instalacion in partida.Instalaciones.Where(i => i.Tipo == TipoInstalacion.Aclimatacion))
        {
            int capacidadUsada = partida.Criaturas.Count(c => c.Activo);
            int capacidadLibre = instalacion.Capacidad - capacidadUsada;

            if (capacidadLibre > 0 && _random.Next(100) < 20)
            {
                var nuevaCriatura = GenerarCriaturaCompatible(instalacion);
                if (nuevaCriatura != null)
                {
                    partida.Criaturas.Add(nuevaCriatura);
                    ronda.Eventos.Add(new EventoSimulacion
                    {
                        Tipo = TipoEvento.InstalacionConstruida,
                        Descripcion = $"Nueva criatura nacida en {instalacion.Codigo}: {nuevaCriatura.Nombre}",
                        EntidadId = nuevaCriatura.Id.ToString()
                    });
                }
            }
        }
    }

    private Criatura? GenerarCriaturaCompatible(Instalacion instalacion)
    {
        var tiposDisponibles = new List<(TipoCriatura Tipo, string Nombre, MedioCriatura Medio)>
        {
            (TipoCriatura.GusanoArenaJuvenil, "Gusano de arena juvenil", MedioCriatura.Subterraneo),
            (TipoCriatura.TigreLaza, "Tigre laza", MedioCriatura.Desierto),
            (TipoCriatura.MuadDib, "Muad'Dib", MedioCriatura.Desierto),
            (TipoCriatura.HalconDesierto, "Halcon del desierto", MedioCriatura.Aereo),
            (TipoCriatura.TruchaArena, "Trucha de arena", MedioCriatura.Subterraneo)
        };

        var candidatos = tiposDisponibles
            .Where(t => instalacion.Medio.Contains(t.Medio.ToString().ToUpper()) || 
                       (instalacion.Rol == "RECOLECTOR" && t.Tipo == TipoCriatura.MuadDib))
            .OrderBy(_ => _random.Next())
            .ToList();

        if (candidatos.Count == 0) return null;
        
        var tipoCompatible = candidatos.First();
        if (tipoCompatible.Tipo == 0) return null;

        var infoCriatura = ObtenerInfoCriatura(tipoCompatible.Tipo);

        return new Criatura
        {
            Nombre = $"{tipoCompatible.Nombre} (clon)",
            Tipo = tipoCompatible.Tipo,
            Medio = tipoCompatible.Medio,
            Rol = instalacion.Rol == "RECOLECTOR" ? RolCriatura.Recolector : RolCriatura.Depredador,
            EdadAdulta = infoCriatura.EdadAdulta,
            ApetitoBase = infoCriatura.ApetitoBase,
            Vida = infoCriatura.Vida,
            VidaMaxima = infoCriatura.Vida,
            Ataque = infoCriatura.Ataque,
            PosicionX = 10,
            PosicionY = 10,
            Activo = true
        };
    }

    private (int EdadAdulta, int ApetitoBase, int Vida, int Ataque) ObtenerInfoCriatura(TipoCriatura tipo)
    {
        return tipo switch
        {
            TipoCriatura.GusanoArenaJuvenil => (24, 5, 100, 25),
            TipoCriatura.TigreLaza => (38, 8, 80, 30),
            TipoCriatura.MuadDib => (12, 2, 50, 15),
            TipoCriatura.HalconDesierto => (16, 2, 40, 20),
            TipoCriatura.TruchaArena => (42, 10, 60, 10),
            _ => (20, 5, 50, 20)
        };
    }

    private void SimularVisitantesEnclaves(Partida partida, Ronda ronda)
    {
        double saludMedia = CalcularSaludMediaCriaturas(partida.Criaturas);

        foreach (var enclave in partida.Enclaves.Where(e => e.Tipo == TipoEnclave.Exhibicion))
        {
            int llegan = enclave.VisitantesLlegan(partida.Instalaciones, saludMedia);
            int abandonan = enclave.VisitantesAbandonan(partida.Instalaciones, saludMedia);

            enclave.ActualizarVisitantes(partida.Instalaciones, saludMedia);

            int donacionTotal = 0;
            foreach (var criatura in partida.Criaturas.Where(c => c.Activo))
            {
                int visitantesFavoritos = _random.Next(0, enclave.VisitantesActuales / 10 + 1);
                if (visitantesFavoritos > 0)
                {
                    double sigma = enclave.Nivel switch
                    {
                        NivelEnclave.Bajo => 1,
                        NivelEnclave.Medio => 15,
                        NivelEnclave.Alto => 30,
                        _ => 1
                    };

                    double saludRatio = (double)criatura.Vida / criatura.VidaMaxima;
                    double edadRatio = (double)criatura.EdadAdulta > 0 ? 10.0 / criatura.EdadAdulta : 1;
                    int donacion = (int)(10 * saludRatio * edadRatio * sigma * visitantesFavoritos);
                    donacionTotal += donacion;
                }
            }

            if (llegan > 0 || abandonan > 0 || donacionTotal > 0)
            {
                string descripcion = $"{enclave.Nombre}: {llegan} llegan, {abandonan} abandonan. Total: {enclave.VisitantesActuales}";
                if (donacionTotal > 0)
                {
                    partida.InventarioGlobal.Especia += donacionTotal;
                    descripcion += $" | Donaciones: +{donacionTotal} especias";
                }

                ronda.Eventos.Add(new EventoSimulacion
                {
                    Tipo = TipoEvento.RecursoExtraido,
                    Descripcion = descripcion,
                    EntidadId = enclave.Id.ToString()
                });
            }
        }
    }

    private double CalcularSaludMediaCriaturas(List<Criatura> criaturas)
    {
        if (!criaturas.Any()) return 100.0;
        
        double sumaSalud = criaturas
            .Where(c => c.VidaMaxima > 0)
            .Sum(c => (double)c.Vida / c.VidaMaxima * 100);
        
        return sumaSalud / criaturas.Count;
    }

    private EventoSimulacion? SimularComportamientoCriatura(Partida partida, Criatura criatura)
    {
        int movX = _random.Next(-1, 2);
        int movY = _random.Next(-1, 2);
        
        int nuevoX = Math.Clamp(criatura.PosicionX + movX, 0, partida.Mapa.Ancho - 1);
        int nuevoY = Math.Clamp(criatura.PosicionY + movY, 0, partida.Mapa.Alto - 1);

        criatura.PosicionX = nuevoX;
        criatura.PosicionY = nuevoY;

        var criaturasCercanas = partida.Criaturas
            .Where(c => c.Id != criatura.Id && c.Activo && 
                   Math.Abs(c.PosicionX - criatura.PosicionX) <= 1 &&
                   Math.Abs(c.PosicionY - criatura.PosicionY) <= 1)
            .ToList();

        if (criaturasCercanas.Count > 0 && _random.Next(100) < 30)
        {
            var objetivo = criaturasCercanas.First();
            ProcesarCombate(partida, criatura.Id, objetivo.Id);
            return new EventoSimulacion
            {
                Tipo = TipoEvento.CriaturaAtaco,
                Descripcion = $"{criatura.Nombre} ataco a {objetivo.Nombre}",
                EntidadId = criatura.Id.ToString()
            };
        }

        return new EventoSimulacion
        {
            Tipo = TipoEvento.CriaturaMovio,
            Descripcion = $"{criatura.Nombre} se movio a ({nuevoX},{nuevoY})",
            EntidadId = criatura.Id.ToString()
        };
    }

    public void MoverCriatura(Partida partida, Guid criaturaId, int nuevoX, int nuevoY)
    {
        var criatura = partida.Criaturas.FirstOrDefault(c => c.Id == criaturaId);
        if (criatura == null) return;

        if (nuevoX >= 0 && nuevoX < partida.Mapa.Ancho && 
            nuevoY >= 0 && nuevoY < partida.Mapa.Alto)
        {
            criatura.PosicionX = nuevoX;
            criatura.PosicionY = nuevoY;
        }
    }

    public void ProcesarCombate(Partida partida, Guid atacanteId, Guid objetivoId)
    {
        var atacante = partida.Criaturas.FirstOrDefault(c => c.Id == atacanteId);
        if (atacante == null) return;
        
        var objetivo = partida.Criaturas.FirstOrDefault(c => c.Id == objetivoId);
        if (objetivo == null) return;

        objetivo.Vida -= atacante.Ataque;
        if (objetivo.Vida <= 0)
        {
            objetivo.Activo = false;
        }
    }

    public void ExtraerRecurso(Partida partida, Guid recursoId)
    {
        var recurso = partida.Recursos.FirstOrDefault(r => r.Id == recursoId);
        if (recurso == null || recurso.Extraido) return;

        recurso.Extraido = true;
        
        switch (recurso.Tipo)
        {
            case TipoRecurso.Especia:
                partida.InventarioGlobal.Especia += recurso.Cantidad;
                break;
            case TipoRecurso.Agua:
                partida.InventarioGlobal.Agua += recurso.Cantidad;
                break;
            case TipoRecurso.Materiales:
                partida.InventarioGlobal.Materiales += recurso.Cantidad;
                break;
            case TipoRecurso.Energia:
                partida.InventarioGlobal.Energia += recurso.Cantidad;
                break;
        }
    }
}
