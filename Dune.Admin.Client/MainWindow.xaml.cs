using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dune.Domain.Models;
using Dune.Admin.Client.Views;

namespace Dune.Admin.Client;

public class Configuracion
{
    public double VolumenMusica { get; set; } = 0.3;
    public double VolumenEfectos { get; set; } = 0.5;
    public int ResolucionIndex { get; set; } = 0;
    public bool PantallaCompleta { get; set; } = false;
}

public partial class MainWindow : Window
{
    private readonly string[] _slotPaths;
    private Partida?[] _partidasGuardadas = new Partida?[3];
    private JsonSerializerOptions _jsonOptions;
    private string _musicaActual = "menu";
    private Grid _paginaAnterior = null!;
    private readonly string _configPath;
    private Configuracion _config;
    private bool _creandoPartida = false;

    public MainWindow()
    {
        _configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        _config = CargarConfiguracion();
        
        InitializeComponent();
        
        Loaded += MainWindow_Loaded;
        
        SoundManager.Initialize();
        SoundManager.PlayMenuMusic();
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
        
        var savesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Partidas");
        System.IO.Directory.CreateDirectory(savesPath);
        
        _slotPaths = new[]
        {
            System.IO.Path.Combine(savesPath, "slot1.json"),
            System.IO.Path.Combine(savesPath, "slot2.json"),
            System.IO.Path.Combine(savesPath, "slot3.json")
        };
        
        CargarSlots();
        
        Closing += MainWindow_Closing;
    }

    private void SuscribirEventosViewModel(ViewModels.MainViewModel vm)
    {
        vm.RondaCompletada -= OnRondaCompletada;
        vm.PartidaActualizada -= OnPartidaActualizada;
        
        vm.RondaCompletada += OnRondaCompletada;
        vm.PartidaActualizada += OnPartidaActualizada;
    }
    
    private void DesuscribirEventosViewModel(ViewModels.MainViewModel vm)
    {
        vm.RondaCompletada -= OnRondaCompletada;
        vm.PartidaActualizada -= OnPartidaActualizada;
    }
    
    private void OnRondaCompletada()
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.PartidaActual != null)
                {
                    DibujarMapa(vm.PartidaActual);
                    VerificarGameOver(vm);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnRondaCompletada error: {ex.Message}");
        }
    }
    
    private void OnPartidaActualizada()
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.PartidaActual != null)
                {
                    DibujarMapa(vm.PartidaActual);
                    VerificarGameOver(vm);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnPartidaActualizada error: {ex.Message}");
        }
    }

    private void VerificarGameOver(ViewModels.MainViewModel vm)
    {
        if (vm.PartidaActual == null) return;

        var criaturasVivas = vm.PartidaActual.Criaturas.Count(c => c.Activo);
        if (criaturasVivas == 0)
        {
            MostrarGameOver("Todas las criaturas han perecido.\nLa doma de Arrakis ha fracasado.");
        }
    }
    
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        MusicVolumeSlider.ValueChanged -= MusicVolumeSlider_ValueChanged;
        EffectsVolumeSlider.ValueChanged -= EffectsVolumeSlider_ValueChanged;
        ResolutionCombo.SelectionChanged -= ResolutionCombo_SelectionChanged;
        
        MusicVolumeSlider.Value = _config.VolumenMusica * 100;
        EffectsVolumeSlider.Value = _config.VolumenEfectos * 100;
        SoundManager.SetMusicVolume(_config.VolumenMusica);
        SoundManager.SetEffectsVolume(_config.VolumenEfectos);
        ResolutionCombo.SelectedIndex = _config.ResolucionIndex;
        
        ModoPantallaCompleta_Click(null!, null!);
        
        MusicVolumeSlider.ValueChanged += MusicVolumeSlider_ValueChanged;
        EffectsVolumeSlider.ValueChanged += EffectsVolumeSlider_ValueChanged;
        ResolutionCombo.SelectionChanged += ResolutionCombo_SelectionChanged;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        double mus = (int)MusicVolumeSlider.Value / 100.0;
        double efec = (int)EffectsVolumeSlider.Value / 100.0;
        int res = ResolutionCombo.SelectedIndex;
        bool completa = WindowStyle == WindowStyle.None;
        
        if (mus != 0.3 || efec != 0.5 || res != 0 || completa)
        {
            _config.VolumenMusica = mus;
            _config.VolumenEfectos = efec;
            _config.ResolucionIndex = res;
            _config.PantallaCompleta = completa;
            GuardarConfiguracion();
        }
    }
    
    private void DibujarMapa(Partida partida)
    {
        if (_creandoPartida || MapaVisual == null || !IsLoaded) return;
        
        MapaVisual.Children.Clear();
        
        if (partida?.Mapa?.Celdas == null) return;
        
        int ancho = partida.Mapa.Ancho;
        int alto = partida.Mapa.Alto;
        double mapaSize = 220;
        double celdaSize = mapaSize / Math.Max(ancho, alto);
        double markerSize = Math.Max(celdaSize * 0.6, 4);
        
        MapaVisual.Width = mapaSize;
        MapaVisual.Height = mapaSize;
        
        for (int y = 0; y < alto && y < partida.Mapa.Celdas.Count; y++)
        {
            for (int x = 0; x < ancho && x < partida.Mapa.Celdas[y].Count; x++)
            {
                var terreno = partida.Mapa.Celdas[y][x];
                Rectangle rect = new Rectangle
                {
                    Width = celdaSize,
                    Height = celdaSize,
                    Fill = ObtenerColorTerreno(terreno)
                };
                Canvas.SetLeft(rect, x * celdaSize);
                Canvas.SetTop(rect, y * celdaSize);
                MapaVisual.Children.Add(rect);
            }
        }
        
        foreach (var enclave in partida.Enclaves)
        {
            int mapX = Math.Clamp(enclave.PosicionX - 1, 0, ancho - 1);
            int mapY = Math.Clamp(enclave.PosicionY - 1, 0, alto - 1);
            double posX = mapX * celdaSize + (celdaSize - markerSize) / 2;
            double posY = mapY * celdaSize + (celdaSize - markerSize) / 2;
            Ellipse ellipse = new Ellipse
            {
                Width = markerSize,
                Height = markerSize,
                Fill = new SolidColorBrush(Color.FromRgb(0, 206, 209)),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, posX);
            Canvas.SetTop(ellipse, posY);
            ToolTip tip = new ToolTip { Content = enclave.Nombre };
            ellipse.ToolTip = tip;
            MapaVisual.Children.Add(ellipse);
        }
        
        foreach (var recurso in partida.Recursos.Where(r => !r.Extraido))
        {
            int mapX = Math.Clamp(recurso.PosicionX - 1, 0, ancho - 1);
            int mapY = Math.Clamp(recurso.PosicionY - 1, 0, alto - 1);
            double posX = mapX * celdaSize + (celdaSize - markerSize) / 2;
            double posY = mapY * celdaSize + (celdaSize - markerSize) / 2;
            Brush color = recurso.Tipo switch
            {
                TipoRecurso.Melange => new SolidColorBrush(Color.FromRgb(201, 162, 39)),
                TipoRecurso.EspeciaRosa => new SolidColorBrush(Color.FromRgb(255, 105, 180)),
                TipoRecurso.EspeciaNegra => new SolidColorBrush(Color.FromRgb(128, 0, 128)),
                TipoRecurso.Agua => new SolidColorBrush(Color.FromRgb(65, 105, 225)),
                _ => Brushes.LimeGreen
            };
            Rectangle rect = new Rectangle
            {
                Width = markerSize,
                Height = markerSize,
                Fill = color,
                Stroke = Brushes.White,
                StrokeThickness = 0.5
            };
            Canvas.SetLeft(rect, posX);
            Canvas.SetTop(rect, posY);
            ToolTip tip = new ToolTip { Content = $"{recurso.Tipo}: {recurso.Cantidad}" };
            rect.ToolTip = tip;
            MapaVisual.Children.Add(rect);
        }
        
        foreach (var criatura in partida.Criaturas.Where(c => c.Activo))
        {
            int mapX = Math.Clamp(criatura.PosicionX - 1, 0, ancho - 1);
            int mapY = Math.Clamp(criatura.PosicionY - 1, 0, alto - 1);
            double posX = mapX * celdaSize + (celdaSize - markerSize) / 2;
            double posY = mapY * celdaSize + (celdaSize - markerSize) / 2;
            Ellipse ellipse = new Ellipse
            {
                Width = markerSize,
                Height = markerSize,
                Fill = Brushes.Red,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1
            };
            Canvas.SetLeft(ellipse, posX);
            Canvas.SetTop(ellipse, posY);
            ToolTip tip = new ToolTip { Content = criatura.Nombre };
            ellipse.ToolTip = tip;
            MapaVisual.Children.Add(ellipse);
        }
    }
    
    private Brush ObtenerColorTerreno(TipoTerreno tipo)
    {
        return tipo switch
        {
            TipoTerreno.Desierto => new SolidColorBrush(Color.FromRgb(194, 154, 108)),
            TipoTerreno.Roca => new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            TipoTerreno.ArenaMovida => new SolidColorBrush(Color.FromRgb(210, 180, 140)),
            TipoTerreno.Melange => new SolidColorBrush(Color.FromRgb(201, 162, 39)),
            TipoTerreno.EspeciaRosa => new SolidColorBrush(Color.FromRgb(255, 105, 180)),
            TipoTerreno.EspeciaNegra => new SolidColorBrush(Color.FromRgb(128, 0, 128)),
            _ => new SolidColorBrush(Color.FromRgb(194, 154, 108))
        };
    }

    private Configuracion CargarConfiguracion()
    {
        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<Configuracion>(json, _jsonOptions) ?? new Configuracion();
            }
            catch
            {
                return new Configuracion();
            }
        }
        return new Configuracion();
    }

    private void GuardarConfiguracion()
    {
        try
        {
            _config.VolumenMusica = SoundManager.GetMusicVolume();
            _config.VolumenEfectos = SoundManager.GetEffectsVolume();
            _config.ResolucionIndex = ResolutionCombo.SelectedIndex;
            _config.PantallaCompleta = WindowStyle == WindowStyle.None;
            
            var json = JsonSerializer.Serialize(_config, _jsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch { }
    }

    private void MostrarPagina(Grid pagina, bool guardarAnterior = true)
    {
        if (guardarAnterior)
        {
            if (MenuPrincipal.Visibility == Visibility.Visible) _paginaAnterior = MenuPrincipal;
            else if (PaginaJuego.Visibility == Visibility.Visible) _paginaAnterior = PaginaJuego;
            else if (PaginaCargar.Visibility == Visibility.Visible) _paginaAnterior = PaginaCargar;
            else if (PaginaGuardar.Visibility == Visibility.Visible) _paginaAnterior = PaginaGuardar;
            else if (PaginaBestiario.Visibility == Visibility.Visible) _paginaAnterior = PaginaBestiario;
        }
        
        MenuPrincipal.Visibility = Visibility.Collapsed;
        PaginaJuego.Visibility = Visibility.Collapsed;
        PaginaCargar.Visibility = Visibility.Collapsed;
        PaginaGuardar.Visibility = Visibility.Collapsed;
        PaginaOpciones.Visibility = Visibility.Collapsed;
        PaginaCreditos.Visibility = Visibility.Collapsed;
        PaginaBestiario.Visibility = Visibility.Collapsed;
        
        pagina.Visibility = Visibility.Visible;
    }

    private void NuevaPartida_Click(object sender, RoutedEventArgs e)
    {
        var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        var log = new System.Text.StringBuilder();
        Action<string> EscribirLog = (msg) => {
            log.AppendLine($"{DateTime.Now:HH:mm:ss.fff} - {msg}");
            System.Diagnostics.Debug.WriteLine(msg);
        };
        
        try
        {
            EscribirLog("=== NUEVA PARTIDA CLICK INICIO ===");
            
            SoundManager.PlayClick();
            EscribirLog("Click reproducido");
            
            EscribirLog($"Estado actual - MenuVisible: {MenuPrincipal.Visibility == Visibility.Visible}, JuegoVisible: {PaginaJuego.Visibility == Visibility.Visible}");
            
            var dialog = new CrearPartidaWindow();
            EscribirLog("Dialog creado");
            
            EscribirLog("Antes de ShowDialog");
            var result = dialog.ShowDialog();
            EscribirLog($"DESPUES de ShowDialog - result: {result}");
            
            if (result == true)
            {
                EscribirLog("=== INICIO NuevaPartida_Click ===");
                EscribirLog($"Nombre partida: {dialog.NombrePartida}");
                
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    EscribirLog("VM obtenido");
                    EscribirLog("Antes de LimpiarEventos");
                    vm.LimpiarEventos();
                    EscribirLog("DESPUES de LimpiarEventos");
                    
                    EscribirLog("Antes de CrearPartidaConNombre");
                    vm.CrearPartidaConNombre(dialog.NombrePartida);
                    EscribirLog("DESPUES de CrearPartidaConNombre");
                    
                    vm.RondaCompletada += OnRondaCompletada;
                    vm.PartidaActualizada += OnPartidaActualizada;
                    EscribirLog("Eventos suscritos");
                    
                    _musicaActual = "juego";
                    if (SoundManager.IsMenuMusicPlaying())
                    {
                        SoundManager.StopMusic();
                        SoundManager.PlayGameMusic();
                    }
                    
                    MostrarPagina(PaginaJuego);
                    EscribirLog("Pagina mostrada");
                    
                    if (vm.PartidaActual != null)
                    {
                        DibujarMapa(vm.PartidaActual);
                        EscribirLog("Mapa dibujado");
                    }
                    
                    EscribirLog("=== FIN NuevaPartida_Click EXITOSO ===");
                }
            }
            
            EscribirLog("Escribiendo log final");
            System.IO.File.WriteAllText(logPath, log.ToString());
            EscribirLog("Log escrito");
        }
        catch (Exception ex)
        {
            log.AppendLine($"ERROR: {ex.Message}");
            log.AppendLine(ex.StackTrace);
            System.IO.File.WriteAllText(logPath, log.ToString());
            System.Diagnostics.Debug.WriteLine($"ERROR NuevaPartida_Click: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void IrACargar_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        CargarSlots();
        MostrarPagina(PaginaCargar);
    }

    private void Slot1Cargar_Click(object sender, RoutedEventArgs e) => CargarSlot(0);
    private void Slot2Cargar_Click(object sender, RoutedEventArgs e) => CargarSlot(1);
    private void Slot3Cargar_Click(object sender, RoutedEventArgs e) => CargarSlot(2);

    private void Slot1Eliminar_Click(object sender, RoutedEventArgs e) => EliminarSlot(0);
    private void Slot2Eliminar_Click(object sender, RoutedEventArgs e) => EliminarSlot(1);
    private void Slot3Eliminar_Click(object sender, RoutedEventArgs e) => EliminarSlot(2);

    private void EliminarSlot(int index)
    {
        var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        
        try
        {
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] Slot {index} - inicio\n");
            
            if (_partidasGuardadas[index] == null)
            {
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] Slot {index} - es null, return\n");
                return;
            }

            SoundManager.PlayClick();
            
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] Slot {index} - creando dialog\n");
            var dialog = new Views.ConfirmarEliminarWindow($"¿Estás seguro de eliminar la partida del SLOT {index + 1}?");
            dialog.Owner = this;
            
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] Slot {index} - mostrando dialog\n");
            dialog.ShowDialog();

            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] Slot {index} - dialog cerrado, confirmado={dialog.Confirmado}\n");
            
            if (dialog.Confirmado)
            {
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] Slot {index} - eliminando archivo\n");
                if (File.Exists(_slotPaths[index]))
                {
                    File.Delete(_slotPaths[index]);
                }
                _partidasGuardadas[index] = null;
                CargarSlots();
                System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] Slot {index} - completado\n");
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss.fff} - [ELIMINAR] ERROR: {ex.Message}\n{ex.StackTrace}\n");
            System.Diagnostics.Debug.WriteLine($"Error al eliminar slot: {ex.Message}");
            MessageBox.Show($"Error al eliminar: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CargarSlot(int index)
    {
        SoundManager.PlayClick();
        
        if (_partidasGuardadas[index] != null)
        {
            _musicaActual = "juego";
            if (SoundManager.IsMenuMusicPlaying())
            {
                SoundManager.StopMusic();
                SoundManager.PlayGameMusic();
            }
            
            MostrarPagina(PaginaJuego);
            
            if (DataContext is ViewModels.MainViewModel vm)
            {
                vm.CargarPartidaDesde(_partidasGuardadas[index]!);
                SuscribirEventosViewModel(vm);
                DibujarMapa(_partidasGuardadas[index]!);
            }
        }
    }

    private void Slot1Guardar_Click(object sender, RoutedEventArgs e) => GuardarSlot(0);
    private void Slot2Guardar_Click(object sender, RoutedEventArgs e) => GuardarSlot(1);
    private void Slot3Guardar_Click(object sender, RoutedEventArgs e) => GuardarSlot(2);

    private void GuardarSlot(int index)
    {
        SoundManager.PlayClick();
        
        if (DataContext is ViewModels.MainViewModel vm)
        {
            var partida = vm.ObtenerPartidaActual();
            if (partida != null)
            {
                partida.UltimoGuardado = DateTime.Now;
                var json = JsonSerializer.Serialize(partida, _jsonOptions);
                File.WriteAllText(_slotPaths[index], json);
                CargarSlotsGuardar();
            }
        }
    }

    private void IrAGuardar_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        CargarSlotsGuardar();
        MostrarPagina(PaginaGuardar);
    }

    private void CargarSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            var path = _slotPaths[i];
            TextBlock? nombreText = null;
            TextBlock? infoText = null;
            TextBlock? fechaText = null;
            Button? button = null;

            switch (i)
            {
                case 0:
                    nombreText = Slot1CargarNombre;
                    infoText = Slot1CargarInfo;
                    fechaText = Slot1CargarFecha;
                    button = Slot1Cargar;
                    break;
                case 1:
                    nombreText = Slot2CargarNombre;
                    infoText = Slot2CargarInfo;
                    fechaText = Slot2CargarFecha;
                    button = Slot2Cargar;
                    break;
                case 2:
                    nombreText = Slot3CargarNombre;
                    infoText = Slot3CargarInfo;
                    fechaText = Slot3CargarFecha;
                    button = Slot3Cargar;
                    break;
            }

            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var partida = JsonSerializer.Deserialize<Partida>(json, _jsonOptions);
                    if (partida != null)
                    {
                        _partidasGuardadas[i] = partida;
                        nombreText!.Text = $"SLOT {i + 1}: {partida.Nombre}";
                        infoText!.Text = $"Criaturas: {partida.Criaturas.Count} | Recursos: {partida.Recursos.Count}";
                        fechaText!.Text = partida.UltimoGuardado.ToString("dd/MM/yyyy HH:mm:ss");
                        button!.IsEnabled = true;
                    }
                }
                catch
                {
                    nombreText!.Text = $"SLOT {i + 1} - CORRUPTO";
                    infoText!.Text = "Error al cargar";
                    button!.IsEnabled = false;
                }
            }
            else
            {
                nombreText!.Text = $"SLOT {i + 1} - VACIO";
                infoText!.Text = "Sin datos";
                fechaText!.Text = "";
                button!.IsEnabled = false;
            }
        }
    }

    private void CargarSlotsGuardar()
    {
        for (int i = 0; i < 3; i++)
        {
            var path = _slotPaths[i];
            TextBlock? nombreText = null;
            TextBlock? infoText = null;

            switch (i)
            {
                case 0:
                    nombreText = Slot1GuardarNombre;
                    infoText = Slot1GuardarInfo;
                    break;
                case 1:
                    nombreText = Slot2GuardarNombre;
                    infoText = Slot2GuardarInfo;
                    break;
                case 2:
                    nombreText = Slot3GuardarNombre;
                    infoText = Slot3GuardarInfo;
                    break;
            }

            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var partida = JsonSerializer.Deserialize<Partida>(json, _jsonOptions);
                    if (partida != null)
                    {
                        nombreText!.Text = $"SLOT {i + 1}: {partida.Nombre}";
                        infoText!.Text = $"Sobrescribir - Ultimo: {partida.UltimoGuardado:dd/MM/yyyy HH:mm}";
                    }
                }
                catch
                {
                    nombreText!.Text = $"SLOT {i + 1} - LIBRE";
                    infoText!.Text = "Disponible para guardar";
                }
            }
            else
            {
                nombreText!.Text = $"SLOT {i + 1} - LIBRE";
                infoText!.Text = "Disponible para guardar";
            }
        }
    }

    private void IrAOpciones_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        MostrarPagina(PaginaOpciones);
    }

    private void IrACreditos_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        MostrarPagina(PaginaCreditos);
    }

    private void TestGameOver_Click(object sender, RoutedEventArgs e)
    {
        MostrarGameOver("Modo de prueba.\nLa doma de Arrakis ha fracasado.");
    }

    private void IrABestiario_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        MostrarPagina(PaginaBestiario);
    }

    private void PaginaAnteriorRecursos_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.PaginaAnteriorRecursos();
        }
    }

    private void PaginaSiguienteRecursos_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.PaginaSiguienteRecursos();
        }
    }

    private void VolverMenu_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        if (_musicaActual == "juego")
        {
            MostrarPagina(PaginaJuego, guardarAnterior: false);
        }
        else
        {
            MostrarPagina(MenuPrincipal, guardarAnterior: false);
        }
    }

    private void GameOverVolverMenu_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        SoundManager.StopMusic();
        SoundManager.PlayMenuMusic();
        _musicaActual = "menu";
        PaginaGameOver.Visibility = Visibility.Collapsed;
        MenuPrincipal.Visibility = Visibility.Visible;
        PaginaJuego.Visibility = Visibility.Collapsed;
    }

    private void MostrarGameOver(string mensaje)
    {
        GameOverMensaje.Text = mensaje;
        PaginaGameOver.Visibility = Visibility.Visible;
        PaginaJuego.Visibility = Visibility.Collapsed;
    }

    private void IrAMenu_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        SoundManager.StopMusic();
        SoundManager.PlayMenuMusic();
        _musicaActual = "menu";
        MostrarPagina(MenuPrincipal, guardarAnterior: false);
    }

    private void VolverAJuego_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        MostrarPagina(PaginaJuego);
    }

    private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MusicVolumeText != null)
        {
            int value = (int)MusicVolumeSlider.Value;
            MusicVolumeText.Text = $"{value}%";
            SoundManager.SetMusicVolume(value / 100.0);
        }
    }

    private void EffectsVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EffectsVolumeText != null)
        {
            int value = (int)EffectsVolumeSlider.Value;
            EffectsVolumeText.Text = $"{value}%";
            SoundManager.SetEffectsVolume(value / 100.0);
        }
    }

    private void ResolutionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResolutionCombo == null || !IsLoaded) return;
        
        AplicarResolucion(ResolutionCombo.SelectedIndex);
    }

    private void ModoVentana_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        WindowStyle = WindowStyle.SingleBorderWindow;
        WindowState = WindowState.Normal;
        Width = 1200;
        Height = 700;
        WindowState = WindowState.Normal;
    }

    private void ModoPantallaCompleta_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
    }

    private void AplicarResolucion(int index)
    {
        int width = 1200, height = 700;
        switch (index)
        {
            case 0: width = 1280; height = 720; break;
            case 1: width = 1366; height = 768; break;
            case 2: width = 1600; height = 900; break;
            case 3: width = 1920; height = 1080; break;
        }
        Width = width;
        Height = height;
        WindowState = WindowState.Normal;
    }

    private void Salir_Click(object sender, RoutedEventArgs e)
    {
        SoundManager.PlayClick();
        Application.Current.Shutdown();
    }
}
