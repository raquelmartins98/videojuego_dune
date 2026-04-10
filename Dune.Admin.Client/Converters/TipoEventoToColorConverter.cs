using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Dune.Domain.Models;

namespace Dune.Admin.Client.Converters;

public class TipoEventoToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TipoEvento tipo)
        {
            return tipo switch
            {
                TipoEvento.CriaturaAtaco => new SolidColorBrush(Color.FromRgb(255, 69, 0)),    // Naranja-rojo (combate)
                TipoEvento.CriaturaMovio => new SolidColorBrush(Color.FromRgb(0, 206, 209)),    // Cyan (movimiento)
                TipoEvento.RecursoExtraido => new SolidColorBrush(Color.FromRgb(201, 162, 39)), // Oro (recurso)
                TipoEvento.InstalacionConstruida => new SolidColorBrush(Color.FromRgb(34, 139, 34)), // Verde (nacimiento)
                TipoEvento.EnclaveAtacado => new SolidColorBrush(Color.FromRgb(255, 0, 0)),     // Rojo (enclave atacado)
                TipoEvento.RondaCompletada => new SolidColorBrush(Color.FromRgb(212, 175, 55)), // Oro claro
                TipoEvento.Ganancias => new SolidColorBrush(Color.FromRgb(255, 215, 0)),       // Dorado (ganancias)
                _ => new SolidColorBrush(Color.FromRgb(212, 175, 55))
            };
        }
        return new SolidColorBrush(Color.FromRgb(212, 175, 55));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
