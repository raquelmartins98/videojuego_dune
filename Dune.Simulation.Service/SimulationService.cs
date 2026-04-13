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

            var eventosCombate = new List<EventoSimulacion>();
            var eventosMovimiento = new List<EventoSimulacion>();
            var eventosRecursos = new List<EventoSimulacion>();
            var eventosEnclaves = new List<EventoSimulacion>();
            var eventosNacimiento = new List<EventoSimulacion>();

            foreach (var criatura in partida.Criaturas.Where(c => c.Activo).ToList())
            {
                try
                {
                    var (evento, esCombate) = SimularComportamientoCriatura(partida, criatura);
                    if (evento != null)
                    {
                        if (esCombate)
                            eventosCombate.Add(evento);
                        else
                            eventosMovimiento.Add(evento);
                    }
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
                    if (recurso.Tipo == TipoRecurso.Melange && _random.Next(100) < 10)
                    {
                        recurso.Cantidad += 10;
                        eventosRecursos.Add(new EventoSimulacion
                        {
                            Tipo = TipoEvento.RecursoExtraido,
                            Descripcion = $"🟠 Melange regenero +10 en ({recurso.PosicionX},{recurso.PosicionY})"
                        });
                    }
                    else if (recurso.Tipo == TipoRecurso.EspeciaRosa && _random.Next(100) < 8)
                    {
                        recurso.Cantidad += 8;
                        eventosRecursos.Add(new EventoSimulacion
                        {
                            Tipo = TipoEvento.RecursoExtraido,
                            Descripcion = $"🌸 Especia Rosa regenero +8 en ({recurso.PosicionX},{recurso.PosicionY})"
                        });
                    }
                    else if (recurso.Tipo == TipoRecurso.EspeciaNegra && _random.Next(100) < 5)
                    {
                        recurso.Cantidad += 5;
                        eventosRecursos.Add(new EventoSimulacion
                        {
                            Tipo = TipoEvento.RecursoExtraido,
                            Descripcion = $"⬛ Especia Negra regenero +5 en ({recurso.PosicionX},{recurso.PosicionY})"
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error con recurso: {ex.Message}");
                }
            }

            SimularInstalacionesAclimatacion(partida, eventosNacimiento);
            SimularVisitantesEnclaves(partida, eventosEnclaves);

            ronda.Eventos.AddRange(eventosCombate);
            ronda.Eventos.AddRange(eventosMovimiento);
            ronda.Eventos.AddRange(eventosRecursos);
            ronda.Eventos.AddRange(eventosNacimiento);
            ronda.Eventos.AddRange(eventosEnclaves);

            partida.HistorialRondas.Add(ronda);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en EjecutarRonda: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }

        return ronda;
    }

    private void SimularInstalacionesAclimatacion(Partida partida, List<EventoSimulacion> eventos)
    {
        foreach (var instalacion in partida.Instalaciones.Where(i => i.Tipo == TipoInstalacion.Aclimatacion))
        {
            int capacidadUsada = partida.Criaturas.Count(c => c.Activo);
            int capacidadLibre = instalacion.Capacidad - capacidadUsada;

            if (capacidadLibre > 0 && _random.Next(100) < 20)
            {
                var nuevaCriatura = GenerarCriaturaCompatible(instalacion, partida);
                if (nuevaCriatura != null)
                {
                    partida.Criaturas.Add(nuevaCriatura);
                    eventos.Add(new EventoSimulacion
                    {
                        Tipo = TipoEvento.InstalacionConstruida,
                        Descripcion = $"🐣 {nuevaCriatura.Nombre} nacio en {instalacion.Codigo}",
                        EntidadId = nuevaCriatura.Id.ToString()
                    });
                }
            }
        }
    }

    private Criatura? GenerarCriaturaCompatible(Instalacion instalacion, Partida partida)
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

        int nuevaX, nuevaY;
        var posicionesOcupadas = partida.Criaturas
            .Select(c => (c.PosicionX, c.PosicionY))
            .ToHashSet();
        
        do
        {
            nuevaX = _random.Next(1, partida.Mapa.Ancho + 1);
            nuevaY = _random.Next(1, partida.Mapa.Alto + 1);
        } while (posicionesOcupadas.Contains((nuevaX, nuevaY)) || 
                 partida.Mapa.Celdas[nuevaY - 1][nuevaX - 1] == TipoTerreno.Roca);
        
        return new Criatura
        {
            Nombre = $"{tipoCompatible.Nombre} (hijo)",
            Tipo = tipoCompatible.Tipo,
            Medio = tipoCompatible.Medio,
            Rol = instalacion.Rol == "RECOLECTOR" ? RolCriatura.Recolector : RolCriatura.Depredador,
            EdadAdulta = infoCriatura.EdadAdulta,
            ApetitoBase = infoCriatura.ApetitoBase,
            Vida = infoCriatura.Vida,
            VidaMaxima = infoCriatura.Vida,
            Ataque = infoCriatura.Ataque,
            PosicionX = nuevaX,
            PosicionY = nuevaY,
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

    private void SimularVisitantesEnclaves(Partida partida, List<EventoSimulacion> eventos)
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

            string descripcion = $"🏛️ {enclave.Nombre}: +{llegan} / -{abandonan} | Visitantes: {enclave.VisitantesActuales}";
            if (donacionTotal > 0)
            {
                partida.InventarioGlobal.Especia += donacionTotal;
                descripcion += $" | 🟠 +{donacionTotal} especias";
            }

            eventos.Add(new EventoSimulacion
            {
                Tipo = TipoEvento.Ganancias,
                Descripcion = descripcion,
                EntidadId = enclave.Id.ToString()
            });
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

    private (EventoSimulacion? Evento, bool EsCombate) SimularComportamientoCriatura(Partida partida, Criatura criatura)
    {
        int posAnteriorX = criatura.PosicionX;
        int posAnteriorY = criatura.PosicionY;

        int movX = _random.Next(-1, 2);
        int movY = _random.Next(-1, 2);
        
        int nuevoX = Math.Clamp(criatura.PosicionX + movX, 1, partida.Mapa.Ancho);
        int nuevoY = Math.Clamp(criatura.PosicionY + movY, 1, partida.Mapa.Alto);

        int mapaX = nuevoX - 1;
        int mapaY = nuevoY - 1;
        var terrenoDestino = partida.Mapa.Celdas[mapaY][mapaX];
        if (terrenoDestino == TipoTerreno.Roca)
        {
            return (new EventoSimulacion
            {
                Tipo = TipoEvento.CriaturaMovio,
                Descripcion = $"🚫 {criatura.Nombre} no puede atravesar roca en ({nuevoX},{nuevoY})",
                EntidadId = criatura.Id.ToString()
            }, false);
        }

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
            
            string icono = objetivo.Vida <= 0 ? "💀" : "⚔️";
            
            return (new EventoSimulacion
            {
                Tipo = TipoEvento.CriaturaAtaco,
                Descripcion = $"{icono} {criatura.Nombre} ataca a {objetivo.Nombre}. Dano: -{criatura.Ataque}",
                EntidadId = criatura.Id.ToString()
            }, true);
        }

        if (posAnteriorX != nuevoX || posAnteriorY != nuevoY)
        {
            return (new EventoSimulacion
            {
                Tipo = TipoEvento.CriaturaMovio,
                Descripcion = $"➡️ {criatura.Nombre}: ({posAnteriorX},{posAnteriorY}) → ({nuevoX},{nuevoY}) | Vida: {criatura.Vida}",
                EntidadId = criatura.Id.ToString()
            }, false);
        }

        return (new EventoSimulacion
        {
            Tipo = TipoEvento.CriaturaMovio,
            Descripcion = $"⏸️ {criatura.Nombre} espera en ({nuevoX},{nuevoY})",
            EntidadId = criatura.Id.ToString()
        }, false);
    }

    public void MoverCriatura(Partida partida, Guid criaturaId, int nuevoX, int nuevoY)
    {
        var criatura = partida.Criaturas.FirstOrDefault(c => c.Id == criaturaId);
        if (criatura == null) return;

        if (nuevoX >= 1 && nuevoX <= partida.Mapa.Ancho && 
            nuevoY >= 1 && nuevoY <= partida.Mapa.Alto)
        {
            int mapaX = nuevoX - 1;
            int mapaY = nuevoY - 1;
            var terrenoDestino = partida.Mapa.Celdas[mapaY][mapaX];
            if (terrenoDestino == TipoTerreno.Roca)
            {
                return;
            }
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
            case TipoRecurso.Melange:
            case TipoRecurso.EspeciaRosa:
            case TipoRecurso.EspeciaNegra:
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
