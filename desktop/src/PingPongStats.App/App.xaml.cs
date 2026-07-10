using System.Windows;
using System.Windows.Threading;
using PingPongStats.App.Services;
using PingPongStats.App.Views;
using PingPongStats.Core.Helpers;
using PingPongStats.Core.Repositories;
using PingPongStats.Core.Services;
using PingPongStats.Core.ViewModels;

namespace PingPongStats.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PingPongStats");
        Logger.Initialize(Path.Combine(appDataDir, "logs"));
        Logger.Info("PingPongStats wird gestartet.");

        var settingsRepository = new SettingsRepository();
        var dataPathService = new DataPathService();
        var folderPicker = new WpfFolderPickerService();
        var shell = new WpfShellService();

        var settings = settingsRepository.Load();
        var dataPathReady = false;

        if (!string.IsNullOrWhiteSpace(settings.DataPath))
        {
            try
            {
                dataPathService.ValidateAndPrepare(settings.DataPath);
                dataPathReady = true;
            }
            catch (DataPathUnavailableException ex)
            {
                Logger.Warn($"Konfigurierter Datenpfad nicht verwendbar: {ex.Message}");
            }
        }

        if (!dataPathReady)
        {
            var suggestedPath = string.IsNullOrWhiteSpace(settings.DataPath)
                ? Path.Combine("C:\\PingPongStats", "Data")
                : settings.DataPath;

            var setupViewModel = new DataPathSetupViewModel(dataPathService, folderPicker, suggestedPath);
            var setupWindow = new DataPathSetupWindow { DataContext = setupViewModel };
            var dialogResult = setupWindow.ShowDialog();

            if (dialogResult != true || !setupViewModel.Confirmed)
            {
                Logger.Info("Kein Datenpfad bestätigt - Anwendung wird beendet.");
                Shutdown();
                return;
            }

            settings.DataPath = setupViewModel.SelectedPath;
            settingsRepository.Save(settings);
        }

        ThemeManager.Apply(settings.DarkMode);

        var playerRepository = new PlayerXmlRepository(settings.DataPath);
        var matchRepository = new MatchXmlRepository(settings.DataPath);
        var doubleMatchRepository = new DoubleMatchXmlRepository(settings.DataPath);
        var auditLogRepository = new AuditLogXmlRepository(settings.DataPath);
        var dataService = new PingPongDataService(playerRepository, matchRepository, doubleMatchRepository, auditLogRepository);

        var mainViewModel = new MainViewModel(dataService, settingsRepository, dataPathService, folderPicker, shell);

        var mainWindow = new MainWindow { DataContext = mainViewModel };
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Unbehandelte Ausnahme in der Oberfläche", e.Exception);
        MessageBox.Show(
            $"Es ist ein unerwarteter Fehler aufgetreten:\n\n{e.Exception.Message}\n\n" +
            "Details wurden in die Log-Datei geschrieben. Die Anwendung versucht, weiter zu laufen.",
            "PingPongStats - Fehler",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
