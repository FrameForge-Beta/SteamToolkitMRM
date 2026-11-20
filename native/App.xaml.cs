using System.Threading;
using System.Windows;

namespace SteamToolkitMRM;

public partial class App : Application
{
    private const string SingleInstanceMutexName = "SteamToolkitMRM.SingleInstance";
    private const string ShowExistingInstanceEventName = "SteamToolkitMRM.ShowExistingInstance";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _showExistingInstanceEvent;
    private bool _ownsSingleInstanceMutex;

    public bool StartedMinimized { get; private set; }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        _ownsSingleInstanceMutex = createdNew;
        if (!createdNew)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        StartShowExistingInstanceListener();
        StartedMinimized = e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase);
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _showExistingInstanceEvent?.Dispose();
        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
            _ownsSingleInstanceMutex = false;
        }
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void StartShowExistingInstanceListener()
    {
        _showExistingInstanceEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            ShowExistingInstanceEventName);

        var listener = new Thread(() =>
        {
            while (_showExistingInstanceEvent.WaitOne())
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (MainWindow is MainWindow window)
                            window.BringToFront();
                    });
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
        })
        {
            IsBackground = true,
            Name = "SteamToolkitMRM single-instance listener"
        };
        listener.Start();
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using EventWaitHandle? showEvent = EventWaitHandle.OpenExisting(ShowExistingInstanceEventName);
            showEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
