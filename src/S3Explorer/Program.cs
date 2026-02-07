using Avalonia;
using System;
using System.IO;

namespace S3Explorer;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                LogFatal("AppDomain.UnhandledException", e.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                LogFatal("TaskScheduler.UnobservedTaskException", e.Exception);
                e.SetObserved();
            };

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            LogFatal("Main", ex);
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void LogFatal(string source, Exception? ex)
    {
        var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ({source}): {ex}";
        Console.Error.WriteLine(msg);
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "S3Explorer");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(Path.Combine(logDir, "crash.log"), msg + Environment.NewLine);
        }
        catch { }
    }
}
