using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Dune.Domain.Models;

namespace Dune.Admin.Client.Converters;

public class TipoTerrenoToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TipoTerreno tipo)
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
        return new SolidColorBrush(Color.FromRgb(194, 154, 108));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PosicionXToColumnConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int pos) return pos;
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PosicionYToRowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int pos) return pos;
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
