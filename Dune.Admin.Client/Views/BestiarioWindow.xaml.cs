using System.Windows;
using System.Windows.Controls;

namespace Dune.Admin.Client.Views;

public partial class BestiarioWindow : Window
{
    private readonly string[] _categorias = { "Criaturas", "Recursos", "Enclaves", "Instalaciones", "Consejos" };
    private int _indiceActual = 0;

    public BestiarioWindow()
    {
        InitializeComponent();
        MostrarCategoriaActual();
    }

    private void Categoria_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string categoria)
        {
            _indiceActual = Array.IndexOf(_categorias, categoria);
            if (_indiceActual < 0) _indiceActual = 0;
            MostrarCategoriaActual();
        }
    }

    private void Anterior_Click(object sender, RoutedEventArgs e)
    {
        _indiceActual--;
        if (_indiceActual < 0) _indiceActual = _categorias.Length - 1;
        MostrarCategoriaActual();
    }

    private void Siguiente_Click(object sender, RoutedEventArgs e)
    {
        _indiceActual++;
        if (_indiceActual >= _categorias.Length) _indiceActual = 0;
        MostrarCategoriaActual();
    }

    private void MostrarCategoriaActual()
    {
        PanelCriaturas1.Visibility = Visibility.Collapsed;
        PanelCriaturas2.Visibility = Visibility.Collapsed;
        PanelRecursos.Visibility = Visibility.Collapsed;
        PanelEnclaves.Visibility = Visibility.Collapsed;
        PanelInstalaciones.Visibility = Visibility.Collapsed;
        PanelConsejos.Visibility = Visibility.Collapsed;

        switch (_categorias[_indiceActual])
        {
            case "Criaturas":
                PanelCriaturas1.Visibility = Visibility.Visible;
                break;
            case "Recursos":
                PanelRecursos.Visibility = Visibility.Visible;
                break;
            case "Enclaves":
                PanelEnclaves.Visibility = Visibility.Visible;
                break;
            case "Instalaciones":
                PanelInstalaciones.Visibility = Visibility.Visible;
                break;
            case "Consejos":
                PanelConsejos.Visibility = Visibility.Visible;
                break;
        }
    }

    private void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
