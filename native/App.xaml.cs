using System.Windows;

namespace SteamToolkitMRM;

public partial class App : Application
{
    public bool StartedMinimized { get; private set; }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        StartedMinimized = e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);
        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}
