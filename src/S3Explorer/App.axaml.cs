using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using S3Explorer.Services;
using S3Explorer.ViewModels;
using S3Explorer.Views;

namespace S3Explorer;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashScreen();
            splash.Show();

            await Task.Delay(2000);

            var accountStore = new AccountStore();
            accountStore.Load();

            var mainWindow = new MainWindow();
            var dialogService = new DialogService(mainWindow);
            var viewModel = new MainWindowViewModel(accountStore, dialogService);
            mainWindow.DataContext = viewModel;
            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            splash.Close();

            // Catch any Avalonia binding/rendering exceptions
            Avalonia.Logging.Logger.Sink = new LogToConsoleSink();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal class LogToConsoleSink : Avalonia.Logging.ILogSink
{
    public bool IsEnabled(Avalonia.Logging.LogEventLevel level, string area)
        => level >= Avalonia.Logging.LogEventLevel.Warning;

    public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate)
    {
        if (level >= Avalonia.Logging.LogEventLevel.Error)
            Console.Error.WriteLine($"[Avalonia {level}] [{area}] {messageTemplate}");
    }

    public void Log(Avalonia.Logging.LogEventLevel level, string area, object? source, string messageTemplate,
        params object?[] propertyValues)
    {
        if (level >= Avalonia.Logging.LogEventLevel.Error)
            Console.Error.WriteLine($"[Avalonia {level}] [{area}] {messageTemplate} {string.Join(", ", propertyValues ?? [])}");
    }
}
