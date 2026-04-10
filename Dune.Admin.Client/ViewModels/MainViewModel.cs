using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Dune.Admin.Client.Views;
using Dune.Domain.Interfaces;
using Dune.Domain.Models;
using Dune.Persistence.Service;
using Dune.Simulation.Service;

namespace Dune.Admin.Client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ISimulationService _simulationService;
    private readonly IPersistenceService _persistenceService;
    private Partida? _partidaActual;
    private string _mensajeEstado = "Sin partida cargada";
    private readonly string _directorioGuardado;

    public MainViewModel()
    {
        _simulationService = new SimulationService();
        _persistenceService = new PersistenceService();
        _directorioGuardado = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Partidas");

        Criaturas = new ObservableCollection<Criatura>();
        Enclaves = new ObservableCollection<Enclave>();
        Recursos = new ObservableCollection<Recurso>();
        Instalaciones = new ObservableCollection<Instalacion>();
        Eventos = new ObservableCollection<EventoSimulacion>();

        NuevaPartidaCommand = new RelayCommand(CrearNuevaPartida);
        CargarPartidaCommand = new RelayCommand(CargarPartida);
        GuardarPartidaCommand = new RelayCommand(async _ => await GuardarPartida(), _ => PartidaActual != null);
        EjecutarRondaCommand = new RelayCommand(EjecutarRonda, _ => PartidaActual != null);
        SalirCommand = new RelayCommand(Salir);
    }

    public Partida? PartidaActual
    {
        get => _partidaActual;
        set
        {
            if (SetProperty(ref _partidaActual, value))
            {
                OnPropertyChanged(nameof(HayPartida));
                OnPropertyChanged(nameof(TienePartida));
            }
        }
    }

    public bool HayPartida => PartidaActual != null;
    public bool TienePartida => PartidaActual != null;

    public string MensajeEstado
    {
        get => _mensajeEstado;
        set => SetProperty(ref _mensajeEstado, value);
    }

    public ObservableCollection<Criatura> Criaturas { get; }
    public ObservableCollection<Enclave> Enclaves { get; }
    public ObservableCollection<Recurso> Recursos { get; }
    public ObservableCollection<Instalacion> Instalaciones { get; }
    public ObservableCollection<EventoSimulacion> Eventos { get; }

    public ICommand NuevaPartidaCommand { get; }
    public ICommand CargarPartidaCommand { get; }
    public ICommand GuardarPartidaCommand { get; }
    public ICommand EjecutarRondaCommand { get; }
    public ICommand SalirCommand { get; }

    private void CrearNuevaPartida(object? parameter)
    {
        try
        {
            var dialog = new CrearPartidaWindow();
            if (dialog.ShowDialog() == true)
            {
                System.Diagnostics.Debug.WriteLine("CrearNuevaPartida: Creando partida...");
                var partida = new Partida
                {
                    Nombre = dialog.NombrePartida,
                    Mapa = GenerarMapaAleatorio(20, 20)
                };

                partida.Criaturas.AddRange(GenerarCriaturasIniciales());
                partida.Enclaves.AddRange(GenerarEnclavesIniciales());
                partida.Recursos.AddRange(GenerarRecursosIniciales());
                partida.Instalaciones.AddRange(GenerarInstalacionesIniciales(partida.Enclaves));

                PartidaActual = partida;
                ActualizarColecciones();

                MensajeEstado = $"Partida '{PartidaActual.Nombre}' creada - Ronda {PartidaActual.RondaActual}";
                System.Diagnostics.Debug.WriteLine("CrearNuevaPartida: Partida creada correctamente");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CrearNuevaPartida ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            MensajeEstado = $"Error al crear partida: {ex.Message}";
        }
    }

    private Mapa GenerarMapaAleatorio(int ancho, int alto)
    {
        var mapa = new Mapa { Ancho = ancho, Alto = alto };
        var random = Random.Shared;

        for (int y = 0; y < alto; y++)
        {
            var fila = new List<TipoTerreno>();
            for (int x = 0; x < ancho; x++)
            {
                var tipo = random.Next(100) switch
                {
                    < 10 => TipoTerreno.Roca,
                    < 20 => TipoTerreno.ArenaMovida,
                    < 30 => TipoTerreno.Especia,
                    _ => TipoTerreno.Desierto
                };
                fila.Add(tipo);
            }
            mapa.Celdas.Add(fila);
        }
        return mapa;
    }

    private List<Criatura> GenerarCriaturasIniciales()
    {
        var random = Random.Shared;
        var criaturas = new List<Criatura>();
        var posicionesXUsadas = new HashSet<int>();
        var posicionesYUsadas = new HashSet<int>();

        int ObtenerPosicionX()
        {
            int pos;
            do { pos = random.Next(20); } while (posicionesXUsadas.Contains(pos));
            posicionesXUsadas.Add(pos);
            return pos;
        }

        int ObtenerPosicionY()
        {
            int pos;
            do { pos = random.Next(20); } while (posicionesYUsadas.Contains(pos));
            posicionesYUsadas.Add(pos);
            return pos;
        }

        criaturas.Add(new Criatura
        {
            Nombre = "Gusano de arena juvenil",
            Tipo = TipoCriatura.GusanoArenaJuvenil,
            Medio = MedioCriatura.Subterraneo,
            Rol = RolCriatura.Depredador,
            EdadAdulta = 24,
            ApetitoBase = 5,
            Vida = 100,
            VidaMaxima = 100,
            Ataque = 25,
            PosicionX = ObtenerPosicionX(),
            PosicionY = ObtenerPosicionY()
        });

        criaturas.Add(new Criatura
        {
            Nombre = "Tigre laza",
            Tipo = TipoCriatura.TigreLaza,
            Medio = MedioCriatura.Desierto,
            Rol = RolCriatura.Depredador,
            EdadAdulta = 38,
            ApetitoBase = 8,
            Vida = 80,
            VidaMaxima = 80,
            Ataque = 30,
            PosicionX = ObtenerPosicionX(),
            PosicionY = ObtenerPosicionY()
        });

        criaturas.Add(new Criatura
        {
            Nombre = "Muad'Dib",
            Tipo = TipoCriatura.MuadDib,
            Medio = MedioCriatura.Desierto,
            Rol = RolCriatura.Recolector,
            EdadAdulta = 12,
            ApetitoBase = 2,
            Vida = 50,
            VidaMaxima = 50,
            Ataque = 15,
            PosicionX = ObtenerPosicionX(),
            PosicionY = ObtenerPosicionY()
        });

        criaturas.Add(new Criatura
        {
            Nombre = "Halcon del desierto",
            Tipo = TipoCriatura.HalconDesierto,
            Medio = MedioCriatura.Aereo,
            Rol = RolCriatura.Depredador,
            EdadAdulta = 16,
            ApetitoBase = 2,
            Vida = 40,
            VidaMaxima = 40,
            Ataque = 20,
            PosicionX = ObtenerPosicionX(),
            PosicionY = ObtenerPosicionY()
        });

        criaturas.Add(new Criatura
        {
            Nombre = "Trucha de arena",
            Tipo = TipoCriatura.TruchaArena,
            Medio = MedioCriatura.Subterraneo,
            Rol = RolCriatura.Recolector,
            EdadAdulta = 42,
            ApetitoBase = 10,
            Vida = 60,
            VidaMaxima = 60,
            Ataque = 10,
            PosicionX = ObtenerPosicionX(),
            PosicionY = ObtenerPosicionY()
        });

        return criaturas;
    }

    private List<Enclave> GenerarEnclavesIniciales()
    {
        return new List<Enclave>
        {
            new()
            {
                Nombre = "Cuenca Experimental de Arrakis",
                Tipo = TipoEnclave.Aclimatacion,
                Hectareas = 100,
                Capacidad = 20000,
                HabitadoresIniciales = 5000,
                VisitantesActuales = 5000,
                VisitantesMesEnclave = 5000,
                PrecioEntrada = 0,
                PrecioSalida = 0,
                Nivel = NivelEnclave.Medio,
                PosicionX = 3,
                PosicionY = 10
            },
            new()
            {
                Nombre = "Arrakeen",
                Tipo = TipoEnclave.Exhibicion,
                Hectareas = 50,
                Capacidad = 10000,
                HabitadoresIniciales = 7700,
                VisitantesActuales = 7700,
                VisitantesMesEnclave = 10000,
                PrecioEntrada = 1000,
                PrecioSalida = 0,
                Nivel = NivelEnclave.Alto,
                PosicionX = 10,
                PosicionY = 5
            },
            new()
            {
                Nombre = "Giedi Prime",
                Tipo = TipoEnclave.Exhibicion,
                Hectareas = 30,
                Capacidad = 5000,
                HabitadoresIniciales = 100,
                VisitantesActuales = 100,
                VisitantesMesEnclave = 2000,
                PrecioEntrada = 2000,
                PrecioSalida = 0,
                Nivel = NivelEnclave.Bajo,
                PosicionX = 17,
                PosicionY = 5
            },
            new()
            {
                Nombre = "Caladan",
                Tipo = TipoEnclave.Exhibicion,
                Hectareas = 80,
                Capacidad = 25000,
                HabitadoresIniciales = 10000,
                VisitantesActuales = 10000,
                VisitantesMesEnclave = 15000,
                PrecioEntrada = 3000,
                PrecioSalida = 0,
                Nivel = NivelEnclave.Medio,
                PosicionX = 10,
                PosicionY = 15
            }
        };
    }

    private List<Recurso> GenerarRecursosIniciales()
    {
        var random = Random.Shared;
        var recursos = new List<Recurso>();

        for (int i = 0; i < 10; i++)
        {
            recursos.Add(new Recurso
            {
                Tipo = TipoRecurso.Especia,
                Cantidad = random.Next(50, 200),
                PosicionX = random.Next(20),
                PosicionY = random.Next(20)
            });
        }

        for (int i = 0; i < 5; i++)
        {
            recursos.Add(new Recurso
            {
                Tipo = TipoRecurso.Agua,
                Cantidad = random.Next(20, 100),
                PosicionX = random.Next(20),
                PosicionY = random.Next(20)
            });
        }

        return recursos;
    }

    private List<Instalacion> GenerarInstalacionesIniciales(List<Enclave> enclaves)
    {
        var instalaciones = new List<Instalacion>();

        var instalacionesFijas = new List<Instalacion>
        {
            // Aclimatación
            new() { Codigo = "ADR05", Nombre = "Aclimatacion ADR05", Tipo = TipoInstalacion.Aclimatacion, CosteConstruccion = 1000, Medio = "DESIERTO", Rol = "RECOLECTOR", AlimentacionInicial = 200, SuministrosIniciales = 5, Capacidad = 10, Hectareas = 10, Recinto = TipoRecinto.RocaSellada },
            new() { Codigo = "ADP03", Nombre = "Aclimatacion ADP03", Tipo = TipoInstalacion.Aclimatacion, CosteConstruccion = 2500, Medio = "DESIERTO", Rol = "DEPREDADOR", AlimentacionInicial = 300, SuministrosIniciales = 3, Capacidad = 50, Hectareas = 50, Recinto = TipoRecinto.EscudoEstatico },
            new() { Codigo = "AAV02", Nombre = "Aclimatacion AAV02", Tipo = TipoInstalacion.Aclimatacion, CosteConstruccion = 5000, Medio = "AEREO", Rol = "DEPREDADOR", AlimentacionInicial = 500, SuministrosIniciales = 2, Capacidad = 100, Hectareas = 100, Recinto = TipoRecinto.CupulaBlindada },
            new() { Codigo = "ASU04", Nombre = "Aclimatacion ASU04", Tipo = TipoInstalacion.Aclimatacion, CosteConstruccion = 3500, Medio = "SUBTERRANEO", Rol = "DEPREDADOR", AlimentacionInicial = 100, SuministrosIniciales = 4, Capacidad = 25, Hectareas = 25, Recinto = TipoRecinto.PozoReforzado },
            // Exhibición
            new() { Codigo = "EDR02", Nombre = "Exhibicion EDR02", Tipo = TipoInstalacion.Exhibicion, CosteConstruccion = 21000, Medio = "DESIERTO", Rol = "RECOLECTOR", AlimentacionInicial = 0, SuministrosIniciales = 2, Capacidad = 200, Hectareas = 200, Recinto = TipoRecinto.RocaSellada },
            new() { Codigo = "EDP03", Nombre = "Exhibicion EDP03", Tipo = TipoInstalacion.Exhibicion, CosteConstruccion = 12500, Medio = "DESIERTO", Rol = "DEPREDADOR", AlimentacionInicial = 0, SuministrosIniciales = 3, Capacidad = 300, Hectareas = 300, Recinto = TipoRecinto.EscudoEstatico },
            new() { Codigo = "EAV02", Nombre = "Exhibicion EAV02", Tipo = TipoInstalacion.Exhibicion, CosteConstruccion = 15000, Medio = "AEREO", Rol = "DEPREDADOR", AlimentacionInicial = 0, SuministrosIniciales = 2, Capacidad = 200, Hectareas = 200, Recinto = TipoRecinto.CupulaBlindada },
            new() { Codigo = "ESU03", Nombre = "Exhibicion ESU03", Tipo = TipoInstalacion.Exhibicion, CosteConstruccion = 25000, Medio = "SUBTERRANEO", Rol = "DEPREDADOR", AlimentacionInicial = 0, SuministrosIniciales = 3, Capacidad = 400, Hectareas = 400, Recinto = TipoRecinto.PozoReforzado }
        };

        foreach (var enclave in enclaves.Where(e => e.Tipo == TipoEnclave.Aclimatacion))
        {
            foreach (var inst in instalacionesFijas.Where(i => i.Tipo == TipoInstalacion.Aclimatacion))
            {
                var nueva = new Instalacion
                {
                    Codigo = inst.Codigo,
                    Nombre = inst.Nombre,
                    Tipo = inst.Tipo,
                    CosteConstruccion = inst.CosteConstruccion,
                    Medio = inst.Medio,
                    Rol = inst.Rol,
                    AlimentacionInicial = inst.AlimentacionInicial,
                    SuministrosIniciales = inst.SuministrosIniciales,
                    Capacidad = inst.Capacidad,
                    Hectareas = inst.Hectareas,
                    Recinto = inst.Recinto,
                    Reservas = inst.SuministrosIniciales,
                    EnclaveId = enclave.Id
                };
                instalaciones.Add(nueva);
            }
        }

        foreach (var enclave in enclaves.Where(e => e.Tipo == TipoEnclave.Exhibicion))
        {
            foreach (var inst in instalacionesFijas.Where(i => i.Tipo == TipoInstalacion.Exhibicion))
            {
                var nueva = new Instalacion
                {
                    Codigo = inst.Codigo,
                    Nombre = inst.Nombre,
                    Tipo = inst.Tipo,
                    CosteConstruccion = inst.CosteConstruccion,
                    Medio = inst.Medio,
                    Rol = inst.Rol,
                    AlimentacionInicial = inst.AlimentacionInicial,
                    SuministrosIniciales = inst.SuministrosIniciales,
                    Capacidad = inst.Capacidad,
                    Hectareas = inst.Hectareas,
                    Recinto = inst.Recinto,
                    Reservas = inst.SuministrosIniciales,
                    EnclaveId = enclave.Id
                };
                instalaciones.Add(nueva);
            }
        }

        return instalaciones;
    }

    private async void CargarPartida(object? parameter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            InitialDirectory = _directorioGuardado,
            Filter = "Archivos JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog() == true)
        {
            var (partida, mensaje) = await _persistenceService.CargarPartidaAsync(dialog.FileName);
            if (partida != null)
            {
                PartidaActual = partida;
                ActualizarColecciones();
                ActualizarEventos();
                MensajeEstado = $"Partida '{PartidaActual.Nombre}' cargada - Ronda {PartidaActual.RondaActual}";
            }
            else
            {
                MensajeEstado = mensaje;
            }
        }
    }

    private async Task GuardarPartida()
    {
        if (PartidaActual == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            InitialDirectory = _directorioGuardado,
            Filter = "Archivos JSON (*.json)|*.json",
            FileName = $"{PartidaActual.Nombre}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            var (exito, mensaje) = await _persistenceService.GuardarPartidaAsync(PartidaActual, dialog.FileName);
            MensajeEstado = mensaje;
        }
    }

    private void EjecutarRonda(object? parameter)
    {
        if (PartidaActual == null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine("EjecutarRonda: Iniciando...");
            var ronda = _simulationService.EjecutarRonda(PartidaActual);
            System.Diagnostics.Debug.WriteLine($"EjecutarRonda: Ronda {ronda.Numero} completada");
            
            Eventos.Clear();
            foreach (var evt in ronda.Eventos)
            {
                Eventos.Add(evt);
            }

            ActualizarColecciones();
            MensajeEstado = $"Ronda {ronda.Numero} completada - Eventos: {ronda.Eventos.Count}";
            System.Diagnostics.Debug.WriteLine("EjecutarRonda: Finalizado correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EjecutarRonda ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            MensajeEstado = $"Error: {ex.Message}";
        }
    }

    private void ActualizarColecciones()
    {
        try
        {
            Criaturas.Clear();
            Enclaves.Clear();
            Recursos.Clear();
            Instalaciones.Clear();

            if (PartidaActual == null) return;

            foreach (var c in PartidaActual.Criaturas)
                Criaturas.Add(c);
            foreach (var e in PartidaActual.Enclaves)
                Enclaves.Add(e);
            foreach (var r in PartidaActual.Recursos.Where(x => !x.Extraido))
                Recursos.Add(r);
            foreach (var i in PartidaActual.Instalaciones)
                Instalaciones.Add(i);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en ActualizarColecciones: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }
    }

    private void ActualizarEventos()
    {
        try
        {
            Eventos.Clear();

            if (PartidaActual?.HistorialRondas == null) return;

            foreach (var ronda in PartidaActual.HistorialRondas)
            {
                foreach (var evt in ronda.Eventos)
                {
                    Eventos.Add(evt);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en ActualizarEventos: {ex.Message}");
        }
    }

    private void Salir(object? parameter)
    {
        Application.Current.Shutdown();
    }
}
