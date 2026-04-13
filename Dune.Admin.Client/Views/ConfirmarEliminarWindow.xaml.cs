using System.Windows;

namespace Dune.Admin.Client.Views;

public partial class ConfirmarEliminarWindow : Window
{
    public bool Confirmado { get; private set; } = false;

    public ConfirmarEliminarWindow(string mensaje)
    {
        InitializeComponent();
        try
        {
            Owner = Application.Current.MainWindow;
        }
        catch { }
        TextoMensaje.Text = mensaje;
    }

    private void BtnSi_Click(object sender, RoutedEventArgs e)
    {
        Confirmado = true;
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        Confirmado = false;
        DialogResult = false;
        Close();
    }
}
