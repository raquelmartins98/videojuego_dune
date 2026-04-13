using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Dune.Domain.Models;

namespace Dune.Admin.Client.Converters;

public class TipoCriaturaToImageConverter : IValueConverter
{
    public static BitmapImage? InstanceGusano { get; } = LoadImage("gusano");
    public static BitmapImage? InstanceTigre { get; } = LoadImage("tigre");
    public static BitmapImage? InstanceRoedoer { get; } = LoadImage("roedoer");
    public static BitmapImage? InstanceHalcon { get; } = LoadImage("halcon");
    public static BitmapImage? InstanceAmeba { get; } = LoadImage("ameba");

    private static BitmapImage? LoadImage(string spriteName)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/{spriteName}.png");
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null!;
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TipoCriatura tipo)
        {
            string spriteName = tipo switch
            {
                TipoCriatura.GusanoArenaJuvenil => "gusano",
                TipoCriatura.TigreLaza => "tigre",
                TipoCriatura.MuadDib => "roedoer",
                TipoCriatura.HalconDesierto => "halcon",
                TipoCriatura.TruchaArena => "ameba",
                _ => "gusano"
            };
            return LoadImage(spriteName)!;
        }
        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TipoEnclaveToImageConverter : IValueConverter
{
    public static BitmapImage? InstanceArena1 { get; } = LoadImage("arena1");
    public static BitmapImage? InstanceArena2 { get; } = LoadImage("arena2");
    public static BitmapImage? InstanceArena3 { get; } = LoadImage("arena3");
    public static BitmapImage? InstanceRocas { get; } = LoadImage("rocas");

    private static BitmapImage? LoadImage(string spriteName)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/{spriteName}.png");
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null!;
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Enclave enclave)
        {
            string spriteName = enclave.Nombre.ToLower() switch
            {
                var n when n.Contains("cuenca") => "arena1",
                var n when n.Contains("arrakeen") => "arena2",
                var n when n.Contains("giedi") => "arena3",
                _ => "rocas"
            };
            return LoadImage(spriteName)!;
        }
        if (value is TipoEnclave tipo)
        {
            string spriteName = tipo switch
            {
                TipoEnclave.Aclimatacion => "arena1",
                TipoEnclave.Exhibicion => "arena2",
                _ => "rocas"
            };
            return LoadImage(spriteName)!;
        }
        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class TipoRecursoToImageConverter : IValueConverter
{
    public static BitmapImage? InstanceMelange { get; } = LoadImage("milagne");
    public static BitmapImage? InstanceRosa { get; } = LoadImage("rosa");
    public static BitmapImage? InstanceNegro { get; } = LoadImage("negro");
    public static BitmapImage? InstanceAgua { get; } = LoadImage("agua");

    private static BitmapImage? LoadImage(string spriteName)
    {
        try
        {
            var uri = new Uri($"pack://application:,,,/Resources/{spriteName}.png");
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null!;
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        TipoRecurso tipoRecurso = default;
        
        if (value is TipoRecurso tipo)
        {
            tipoRecurso = tipo;
        }
        else if (value is ViewModels.RecursoAgrupado agrupado)
        {
            tipoRecurso = agrupado.Tipo;
        }
        else if (value is Recurso recurso)
        {
            tipoRecurso = recurso.Tipo;
        }
        else
        {
            return null!;
        }
        
        string spriteName = tipoRecurso switch
        {
            TipoRecurso.Melange => "milagne",
            TipoRecurso.EspeciaRosa => "rosa",
            TipoRecurso.EspeciaNegra => "negro",
            TipoRecurso.Agua => "agua",
            TipoRecurso.Materiales => "materiales",
            TipoRecurso.Energia => "energia",
            _ => "arena1"
        };
        return LoadImage(spriteName);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
