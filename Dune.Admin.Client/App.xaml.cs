using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Dune.Admin.Client;

public partial class App : Application
{
    private static readonly string LogFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "dune_error.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        SoundManager.Initialize();

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var msg = $"[{DateTime.Now}] Error Fatal:\n{ex?.Message}\n\n{ex?.StackTrace}";
            File.AppendAllText(LogFile, msg + "\n\n");
            Environment.Exit(1);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            var msg = $"[{DateTime.Now}] Error UI:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}";
            File.AppendAllText(LogFile, msg + "\n\n");
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            var msg = $"[{DateTime.Now}] Error Tarea:\n{args.Exception.Message}\n\n{args.Exception.StackTrace}";
            File.AppendAllText(LogFile, msg + "\n\n");
            args.SetObserved();
        };
    }
}
