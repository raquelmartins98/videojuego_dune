using System.Globalization;
using System.Windows.Data;
using Dune.Domain.Models;

namespace Dune.Admin.Client.Converters;

public class TipoRecursoToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TipoRecurso tipo)
        {
            return tipo switch
            {
                TipoRecurso.Melange => "Melange",
                TipoRecurso.EspeciaRosa => "Especia Rosa",
                TipoRecurso.EspeciaNegra => "Especia Negra",
                TipoRecurso.Agua => "Agua",
                TipoRecurso.Materiales => "Materiales",
                TipoRecurso.Energia => "Energia",
                _ => value.ToString()
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
