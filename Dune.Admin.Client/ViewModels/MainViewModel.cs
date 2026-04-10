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

public class RecursoAgrupado
{
    public TipoRecurso Tipo { get; set; }
    public int CantidadTotal { get; set; }
    public int CantidadYacimientos { get; set; }
    public int Regeneracion { get; set; }
    public string Rareza { get; set; } = "";
}

public class MainViewModel : ViewModelBase
{
    private readonly ISimulationService _simulationService;
    private readonly IPersistenceService _persistenceService;
    private Partida? _partidaActual;
    private string _mensajeEstado = "Sin partida cargada";
    private readonly string _directorioGuardado;
    private readonly string _archivoAutoGuardado;
    
    private readonly List<EventoSimulacion> _todosLosEventos = new();
    private TipoEvento? _filtroActual;
    private int _paginaActual = 1;
    private const int EventosPorPagina = 8;
    
    public MainViewModel()
    {
        _simulationService = new SimulationService();
        _persistenceService = new PersistenceService();
        _directorioGuardado = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Partidas");
        _archivoAutoGuardado = Path.Combine(_directorioGuardado, "ultima_partida.json");

        Criaturas = new ObservableCollection<Criatura>();
        Enclaves = new ObservableCollection<Enclave>();
        Recursos = new ObservableCollection<Recurso>();
        RecursosAgrupados = new ObservableCollection<RecursoAgrupado>();
        RecursosPaginados = new ObservableCollection<RecursoAgrupado>();
        Instalaciones = new ObservableCollection<Instalacion>();
        Eventos = new ObservableCollection<EventoSimulacion>();

        NuevaPartidaCommand = new RelayCommand(_ => { SoundManager.PlayClick(); CrearNuevaPartida(_); });
        CargarPartidaCommand = new RelayCommand(_ => { SoundManager.PlayClick(); CargarPartida(_); });
        GuardarPartidaCommand = new RelayCommand(async _ => { SoundManager.PlayClick(); await GuardarPartida(); }, _ => PartidaActual != null);
        EjecutarRondaCommand = new RelayCommand(_ => { EjecutarRonda(_); }, _ => PartidaActual != null);
        SalirCommand = new RelayCommand(_ => { SoundManager.PlayClick(); Salir(_); });
        
        PaginaAnteriorCommand = new RelayCommand(_ => { SoundManager.PlayClick(); IrPaginaAnterior(); }, _ => PuedeIrPaginaAnterior);
        PaginaSiguienteCommand = new RelayCommand(_ => { SoundManager.PlayClick(); IrPaginaSiguiente(); }, _ => PuedeIrPaginaSiguiente);
        CambiarFiltroCommand = new RelayCommand(_ => { SoundManager.PlayClick(); CambiarFiltro(_); });

        CargarPartidaAutomatica();
    }

    public ObservableCollection<EventoSimulacion> Eventos { get; }
    
    public ICommand PaginaAnteriorCommand { get; }
    public ICommand PaginaSiguienteCommand { get; }
    public ICommand CambiarFiltroCommand { get; }

    private async void CargarPartidaAutomatica()
    {
        if (File.Exists(_archivoAutoGuardado))
        {
            var (partida, mensaje) = await _persistenceService.CargarPartidaAsync(_archivoAutoGuardado);
            if (partida != null)
            {
                PartidaActual = partida;
                ActualizarColecciones();
                MensajeEstado = $"Partida '{PartidaActual.Nombre}' cargada - Ronda {PartidaActual.RondaActual}";
                return;
            }
        }
        MensajeEstado = "Sin partida cargada - Crea una nueva";
    }

    private async Task AutoGuardarAsync()
    {
        if (PartidaActual == null) return;
        Directory.CreateDirectory(_directorioGuardado);
        await _persistenceService.GuardarPartidaAsync(PartidaActual, _archivoAutoGuardado);
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
    public ObservableCollection<RecursoAgrupado> RecursosAgrupados { get; }
    public ObservableCollection<RecursoAgrupado> RecursosPaginados { get; private set; }
    public ObservableCollection<Instalacion> Instalaciones { get; }
    
    private int _paginaRecursosActual = 1;
    private int _recursosTotalPaginas = 1;
    private const int RecursosPorPagina = 6;
    
    public string PaginaRecursosTexto => $"Pagina {_paginaRecursosActual} de {_recursosTotalPaginas}";

    public ICommand NuevaPartidaCommand { get; }
    public ICommand CargarPartidaCommand { get; }
    public ICommand GuardarPartidaCommand { get; }
    public ICommand EjecutarRondaCommand { get; }
    public ICommand SalirCommand { get; }
    
    public int PaginaActual
    {
        get => _paginaActual;
        set
        {
            if (SetProperty(ref _paginaActual, value))
            {
                OnPropertyChanged(nameof(PaginaTexto));
                OnPropertyChanged(nameof(PuedeIrPaginaAnterior));
                OnPropertyChanged(nameof(PuedeIrPaginaSiguiente));
            }
        }
    }
    
    public int TotalPaginas
    {
        get
        {
            var filtrados = _filtroActual == null 
                ? _todosLosEventos.Count 
                : _todosLosEventos.Count(e => e.Tipo == _filtroActual);
            return filtrados == 0 ? 0 : (int)Math.Ceiling(filtrados / (double)EventosPorPagina);
        }
    }
    
    public string PaginaTexto => TotalPaginas == 0 ? "Sin eventos" : $"Pagina {_paginaActual} de {TotalPaginas}";
    
    public bool PuedeIrPaginaAnterior => _paginaActual > 1 && TotalPaginas > 0;
    public bool PuedeIrPaginaSiguiente => _paginaActual < TotalPaginas && TotalPaginas > 0;
    
    public int TotalEventosFiltrados => _filtroActual == null 
        ? _todosLosEventos.Count 
        : _todosLosEventos.Count(e => e.Tipo == _filtroActual);

    public event Action? RondaCompletada;
    public event Action? PartidaActualizada;
    
    private bool HayEventosEnPaginaActual()
    {
        if (TotalEventosFiltrados == 0) return false;
        
        int inicio = (_paginaActual) * EventosPorPagina;
        return inicio < TotalEventosFiltrados;
    }
    
    public string FiltroActualTexto => _filtroActual == null ? "TODOS" : _filtroActual.ToString().ToUpper();
    
    public int TotalEventos => _filtroActual == null 
        ? _todosLosEventos.Count 
        : _todosLosEventos.Count(e => e.Tipo == _filtroActual);
    public string TotalEventosTexto => $"Total: {TotalEventos} eventos";
    
    private void IrPaginaAnterior()
    {
        if (PaginaActual > 1)
        {
            PaginaActual--;
            ActualizarEventosVisibles();
        }
    }
    
    private void IrPaginaSiguiente()
    {
        if (PaginaActual < TotalPaginas && HayEventosEnPaginaSiguiente())
        {
            PaginaActual++;
            ActualizarEventosVisibles();
        }
    }
    
    private bool HayEventosEnPaginaSiguiente()
    {
        int inicio = (_paginaActual + 1 - 1) * EventosPorPagina;
        return inicio < TotalEventosFiltrados;
    }
    
    private void CambiarFiltro(object? parameter)
    {
        if (parameter is string filtro)
        {
            if (filtro == "TODOS")
            {
                _filtroActual = null;
            }
            else if (Enum.TryParse<TipoEvento>(filtro, out var tipo))
            {
                _filtroActual = tipo;
            }
            
            PaginaActual = 1;
            ActualizarEventosVisibles();
            OnPropertyChanged(nameof(FiltroActualTexto));
            OnPropertyChanged(nameof(TotalPaginas));
            OnPropertyChanged(nameof(TotalEventosTexto));
        }
    }
    
    private void ActualizarEventosFiltrados()
    {
        Eventos.Clear();
        var filtrados = _filtroActual == null 
            ? _todosLosEventos 
            : _todosLosEventos.Where(e => e.Tipo == _filtroActual).ToList();
        
        int inicio = (PaginaActual - 1) * EventosPorPagina;
        var pagina = filtrados.Skip(inicio).Take(EventosPorPagina).ToList();
        
        foreach (var evt in pagina)
        {
            Eventos.Add(evt);
        }
    }
    
    private void ActualizarEventosVisibles()
    {
        var filtrados = _filtroActual == null 
            ? _todosLosEventos.ToList()
            : _todosLosEventos.Where(e => e.Tipo == _filtroActual).ToList();
        
        Eventos.Clear();
        int inicio = (PaginaActual - 1) * EventosPorPagina;
        var pagina = filtrados.Skip(inicio).Take(EventosPorPagina).ToList();
        
        foreach (var evt in pagina)
        {
            Eventos.Add(evt);
        }
        
        OnPropertyChanged(nameof(PaginaTexto));
        OnPropertyChanged(nameof(TotalPaginas));
        OnPropertyChanged(nameof(TotalEventos));
        OnPropertyChanged(nameof(TotalEventosTexto));
        OnPropertyChanged(nameof(PuedeIrPaginaAnterior));
        OnPropertyChanged(nameof(PuedeIrPaginaSiguiente));
    }

    private async void CrearNuevaPartida(object? parameter)
    {
        try
        {
            LimpiarTodo();
            
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
                await AutoGuardarAsync();

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

    public void CrearNuevaPartidaDirecta()
    {
        CrearPartidaConNombre("Nueva Partida");
    }

    public void CrearPartidaConNombre(string nombre)
    {
        try
        {
            LimpiarTodo();
            
            var partida = new Partida
            {
                Nombre = string.IsNullOrWhiteSpace(nombre) ? "Partida sin nombre" : nombre,
                Mapa = GenerarMapaAleatorio(20, 20)
            };

            PartidaActual = partida;

            foreach (var cri in GenerarCriaturasIniciales())
            {
                var pos = AjustarPosicionSiEsRoca(partida.Mapa, cri.PosicionX, cri.PosicionY);
                cri.PosicionX = pos.x;
                cri.PosicionY = pos.y;
                partida.Criaturas.Add(cri);
            }
            foreach (var enc in GenerarEnclavesIniciales())
            {
                var pos = AjustarPosicionSiEsRoca(partida.Mapa, enc.PosicionX, enc.PosicionY);
                enc.PosicionX = pos.x;
                enc.PosicionY = pos.y;
                partida.Enclaves.Add(enc);
            }
            partida.Recursos.AddRange(GenerarRecursosIniciales());
            partida.Instalaciones.AddRange(GenerarInstalacionesIniciales(partida.Enclaves));

            ActualizarColecciones();
            ActualizarPaginacion();

            MensajeEstado = $"Partida '{PartidaActual.Nombre}' creada - Ronda {PartidaActual.RondaActual}";
            System.Diagnostics.Debug.WriteLine($"CrearPartidaConNombre: Partida '{nombre}' creada correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CrearPartidaConNombre ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            MensajeEstado = $"Error al crear partida: {ex.Message}";
        }
    }

    private (int x, int y) AjustarPosicionSiEsRoca(Mapa mapa, int x, int y)
    {
        if (mapa.Celdas[y - 1][x - 1] != TipoTerreno.Roca)
        {
            return (x, y);
        }
        for (int ny = 1; ny <= mapa.Alto; ny++)
        {
            for (int nx = 1; nx <= mapa.Ancho; nx++)
            {
                if (mapa.Celdas[ny - 1][nx - 1] != TipoTerreno.Roca)
                {
                    return (nx, ny);
                }
            }
        }
        return (x, y);
    }

    private void LimpiarTodo()
    {
        Eventos.Clear();
        _todosLosEventos.Clear();
        _filtroActual = null;
        _paginaActual = 1;
        Criaturas.Clear();
        Enclaves.Clear();
        Recursos.Clear();
        Instalaciones.Clear();
        if (_partidaActual != null)
        {
            _partidaActual.HistorialRondas.Clear();
        }
        _partidaActual = null;
        ActualizarPaginacion();
        OnPropertyChanged(nameof(FiltroActualTexto));
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
                    < 8 => TipoTerreno.Roca,
                    < 15 => TipoTerreno.ArenaMovida,
                    < 23 => TipoTerreno.Melange,
                    < 28 => TipoTerreno.EspeciaRosa,
                    < 30 => TipoTerreno.EspeciaNegra,
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
        return new List<Criatura>
        {
            new()
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
                PosicionX = 6,
                PosicionY = 6
            },
            new()
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
                PosicionX = 16,
                PosicionY = 9
            },
            new()
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
                PosicionX = 11,
                PosicionY = 13
            },
            new()
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
                PosicionX = 4,
                PosicionY = 16
            },
            new()
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
                PosicionX = 19,
                PosicionY = 4
            }
        };
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
                PosicionX = 4,
                PosicionY = 11
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
                PosicionX = 11,
                PosicionY = 6
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
                PosicionX = 18,
                PosicionY = 6
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
                PosicionX = 11,
                PosicionY = 16
            }
        };
    }

    private List<Recurso> GenerarRecursosIniciales()
    {
        var random = Random.Shared;
        var recursos = new List<Recurso>();
        var mapa = _partidaActual!.Mapa;

        for (int i = 0; i < 12; i++)
        {
            var pos = EncontrarCeldaTerreno(mapa, TipoTerreno.Melange, recursos);
            recursos.Add(new Recurso
            {
                Tipo = TipoRecurso.Melange,
                Cantidad = random.Next(50, 150),
                PosicionX = pos.x,
                PosicionY = pos.y
            });
        }

        for (int i = 0; i < 6; i++)
        {
            var pos = EncontrarCeldaTerreno(mapa, TipoTerreno.EspeciaRosa, recursos);
            recursos.Add(new Recurso
            {
                Tipo = TipoRecurso.EspeciaRosa,
                Cantidad = random.Next(30, 100),
                PosicionX = pos.x,
                PosicionY = pos.y
            });
        }

        for (int i = 0; i < 3; i++)
        {
            var pos = EncontrarCeldaTerreno(mapa, TipoTerreno.EspeciaNegra, recursos);
            recursos.Add(new Recurso
            {
                Tipo = TipoRecurso.EspeciaNegra,
                Cantidad = random.Next(10, 50),
                PosicionX = pos.x,
                PosicionY = pos.y
            });
        }

        for (int i = 0; i < 5; i++)
        {
            var pos = EncontrarCeldaTerreno(mapa, TipoTerreno.Desierto, recursos);
            recursos.Add(new Recurso
            {
                Tipo = TipoRecurso.Agua,
                Cantidad = random.Next(20, 100),
                PosicionX = pos.x,
                PosicionY = pos.y
            });
        }

        return recursos;
    }

    private (int x, int y) EncontrarCeldaTerreno(Mapa mapa, TipoTerreno tipoBuscado, List<Recurso> recursosExistentes)
    {
        var random = Random.Shared;
        var posicionesUsadas = recursosExistentes.Select(r => (r.PosicionX - 1, r.PosicionY - 1)).ToHashSet();

        var celdasValidas = new List<(int x, int y)>();
        for (int y = 0; y < mapa.Alto; y++)
        {
            for (int x = 0; x < mapa.Ancho; x++)
            {
                if (mapa.Celdas[y][x] == tipoBuscado && !posicionesUsadas.Contains((x, y)))
                {
                    celdasValidas.Add((x + 1, y + 1));
                }
            }
        }

        if (celdasValidas.Count > 0)
        {
            return celdasValidas[random.Next(celdasValidas.Count)];
        }

        for (int y = 0; y < mapa.Alto; y++)
        {
            for (int x = 0; x < mapa.Ancho; x++)
            {
                if (mapa.Celdas[y][x] != TipoTerreno.Roca && !posicionesUsadas.Contains((x, y)))
                {
                    return (x + 1, y + 1);
                }
            }
        }

        return (random.Next(1, mapa.Ancho + 1), random.Next(1, mapa.Alto + 1));
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
            LimpiarTodo();
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
    
    public Partida? ObtenerPartidaActual()
    {
        return PartidaActual;
    }
    
    public void CargarPartidaDesde(Partida partida)
    {
        PartidaActual = partida;
        ActualizarColecciones();
        ActualizarPaginacion();
        ActualizarInventario();
        _todosLosEventos.Clear();
        Eventos.Clear();
        foreach (var ronda in partida.HistorialRondas)
        {
            foreach (var evt in ronda.Eventos)
            {
                _todosLosEventos.Insert(0, evt);
            }
        }
        ActualizarEventosVisibles();
        OnPropertyChanged(nameof(PaginaTexto));
        OnPropertyChanged(nameof(TotalPaginas));
        OnPropertyChanged(nameof(TotalEventos));
        OnPropertyChanged(nameof(TotalEventosTexto));
        MensajeEstado = $"Partida '{PartidaActual.Nombre}' cargada - Ronda {PartidaActual.RondaActual}";
    }

    private async void EjecutarRonda(object? parameter)
    {
        if (PartidaActual == null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine("EjecutarRonda: Iniciando...");
            
            var ronda = _simulationService.EjecutarRonda(PartidaActual);
            System.Diagnostics.Debug.WriteLine($"EjecutarRonda: Ronda {ronda.Numero} completada, Eventos en ronda: {ronda.Eventos.Count}");
            
            foreach (var evt in ronda.Eventos)
            {
                _todosLosEventos.Insert(0, evt);
            }

            ActualizarColecciones();
            RondaCompletada?.Invoke();
            PartidaActualizada?.Invoke();
            ActualizarInventario();
            ActualizarEventosFiltrados();
            ActualizarPaginacion();
            await AutoGuardarAsync();
            
            MensajeEstado = $"Ronda {ronda.Numero} completada - Eventos: {ronda.Eventos.Count}";
            System.Diagnostics.Debug.WriteLine("EjecutarRonda: Finalizado correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"EjecutarRonda ERROR: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            MessageBox.Show($"Error al ejecutar ronda: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            MensajeEstado = $"Error: {ex.Message}";
        }
    }
    
    private void ActualizarPaginacion()
    {
        _paginaActual = 1;
        OnPropertyChanged(nameof(PaginaActual));
        OnPropertyChanged(nameof(TotalPaginas));
        OnPropertyChanged(nameof(PaginaTexto));
        OnPropertyChanged(nameof(TotalEventos));
        OnPropertyChanged(nameof(TotalEventosTexto));
        OnPropertyChanged(nameof(PuedeIrPaginaAnterior));
        OnPropertyChanged(nameof(PuedeIrPaginaSiguiente));
    }

    public string EspeciaTotal => PartidaActual?.InventarioGlobal.Especia.ToString() ?? "0";
    public string AguaTotal => PartidaActual?.Recursos.Where(r => r.Tipo == TipoRecurso.Agua && !r.Extraido).Sum(r => r.Cantidad).ToString() ?? "0";
    public string CriaturasVivas => PartidaActual?.Criaturas.Count(c => c.Activo).ToString() ?? "0";

    private void ActualizarInventario()
    {
        OnPropertyChanged(nameof(EspeciaTotal));
        OnPropertyChanged(nameof(AguaTotal));
        OnPropertyChanged(nameof(CriaturasVivas));
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
            
            ActualizarRecursosAgrupados();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en ActualizarColecciones: {ex.Message}");
            System.Diagnostics.Debug.WriteLine(ex.StackTrace);
        }
    }
    
    private void ActualizarRecursosAgrupados()
    {
        RecursosAgrupados.Clear();
        
        if (PartidaActual == null) return;
        
        var recursosNoExtraidos = PartidaActual.Recursos.Where(r => !r.Extraido).ToList();
        
        var agrupados = recursosNoExtraidos
            .GroupBy(r => r.Tipo)
            .Select(g => new RecursoAgrupado
            {
                Tipo = g.Key,
                CantidadTotal = g.Sum(r => r.Cantidad),
                CantidadYacimientos = g.Count(),
                Regeneracion = g.Key switch
                {
                    TipoRecurso.Melange => 10,
                    TipoRecurso.EspeciaRosa => 8,
                    TipoRecurso.EspeciaNegra => 5,
                    _ => 0
                },
                Rareza = g.Key switch
                {
                    TipoRecurso.Melange => "Comun",
                    TipoRecurso.EspeciaRosa => "Rara",
                    TipoRecurso.EspeciaNegra => "Legendaria",
                    TipoRecurso.Agua => "Escasa",
                    TipoRecurso.Materiales => "Comun",
                    TipoRecurso.Energia => "Comun",
                    _ => ""
                }
            })
            .OrderBy(r => r.Tipo)
            .ToList();
        
        foreach (var recurso in agrupados)
            RecursosAgrupados.Add(recurso);
        
        _recursosTotalPaginas = RecursosAgrupados.Count == 0 ? 1 : (int)Math.Ceiling(RecursosAgrupados.Count / (double)RecursosPorPagina);
        if (_paginaRecursosActual > _recursosTotalPaginas) _paginaRecursosActual = _recursosTotalPaginas;
        if (_paginaRecursosActual < 1) _paginaRecursosActual = 1;
        
        ActualizarRecursosPaginados();
        OnPropertyChanged(nameof(PaginaRecursosTexto));
    }
    
    private void ActualizarRecursosPaginados()
    {
        RecursosPaginados.Clear();
        
        int inicio = (_paginaRecursosActual - 1) * RecursosPorPagina;
        var pagina = RecursosAgrupados.Skip(inicio).Take(RecursosPorPagina);
        
        foreach (var recurso in pagina)
            RecursosPaginados.Add(recurso);
    }
    
    public void PaginaAnteriorRecursos()
    {
        if (_paginaRecursosActual > 1)
        {
            _paginaRecursosActual--;
            ActualizarRecursosPaginados();
            OnPropertyChanged(nameof(PaginaRecursosTexto));
        }
    }
    
    public void PaginaSiguienteRecursos()
    {
        if (_paginaRecursosActual < _recursosTotalPaginas)
        {
            _paginaRecursosActual++;
            ActualizarRecursosPaginados();
            OnPropertyChanged(nameof(PaginaRecursosTexto));
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

    private async void Salir(object? parameter)
    {
        if (PartidaActual != null)
        {
            await AutoGuardarAsync();
        }
        Application.Current.Shutdown();
    }
}
