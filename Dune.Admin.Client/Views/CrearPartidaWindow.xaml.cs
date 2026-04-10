using System.Windows;

namespace Dune.Admin.Client.Views;

public partial class CrearPartidaWindow : Window
{
    public string NombrePartida { get; private set; } = string.Empty;

    public CrearPartidaWindow()
    {
        InitializeComponent();
        NombreTextBox.Focus();
    }

    private void CrearButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
        {
            MessageBox.Show("Ingrese un nombre para la partida", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        NombrePartida = NombreTextBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CancelarButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
