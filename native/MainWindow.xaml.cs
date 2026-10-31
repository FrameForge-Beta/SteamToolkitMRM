using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace SteamToolkitMRM;

public partial class MainWindow : Window
{
    private readonly BackupService _backupService = new();
    private readonly ObservableCollection<BackupEntry> _backups = [];
    private readonly DispatcherTimer _monitorTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private bool _steamGameWasRunning;
    private bool _monitorCheckInProgress;
    private bool _operationInProgress;
    private bool _allowClose;
    private readonly Forms.NotifyIcon _trayIcon;

    public MainWindow()
    {
        InitializeComponent();
        Localization.SetLanguage(_backupService.Settings.Language);
        Localization.Apply(this);
        BackupList.ItemsSource = _backups;
        LoadSettingsIntoForm();
        ApplyAccentColor(_backupService.Settings.PrimaryColor);
        _trayIcon = CreateTrayIcon();
        _monitorTimer.Tick += MonitorTimer_Tick;
        _monitorTimer.Start();
        Loaded += async (_, _) => await RefreshBackupsAsync();
        UpdateSteamStatus();
        ApplyStartupRegistration();
        Loaded += (_, _) =>
        {
            if (((App)Application.Current).StartedMinimized &&
                _backupService.Settings.StartMinimizedToTray)
                HideToTray();
        };
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;
    }

    private async void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        await RunOperationAsync(Localization.Get("A criar backup..."), async () =>
        {
            await _backupService.CreateBackupAsync("manual");
            await RefreshBackupsAsync();
            FooterText.Text = Localization.Get("Backup criado com sucesso.");
        });
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshBackupsAsync();

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _backupService.OpenBackupFolder();
        }
        catch (Exception ex)
        {
            ShowError(Localization.Get("Não foi possível abrir a pasta de backups."), ex);
        }
    }

    private void OpenLudusavi_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _backupService.OpenLudusavi();
        }
        catch (Exception ex)
        {
            ShowError(Localization.Get("Não foi possível abrir o Ludusavi."), ex);
        }
    }

    private void BackupsNav_Click(object sender, RoutedEventArgs e)
    {
        BackupsView.Visibility = Visibility.Visible;
        SettingsView.Visibility = Visibility.Collapsed;
        SetNavigationState(backupsSelected: true);
    }

    private void SettingsNav_Click(object sender, RoutedEventArgs e)
    {
        LoadSettingsIntoForm();
        BackupsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
        SetNavigationState(backupsSelected: false);
    }

    private void BrowseBackupPath_Click(object sender, RoutedEventArgs e)
    {
        string initialDirectory = Directory.Exists(BackupPathTextBox.Text.Trim())
            ? BackupPathTextBox.Text.Trim()
            : _backupService.BackupRoot;
        var dialog = new OpenFolderDialog
        {
            Title = Localization.Get("Escolher pasta para backups"),
            InitialDirectory = Directory.Exists(initialDirectory)
                ? initialDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == true)
            BackupPathTextBox.Text = dialog.FolderName;
    }

    private void DefaultBackupPath_Click(object sender, RoutedEventArgs e) => BackupPathTextBox.Clear();

    private void AccentPicker_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ColorPickerDialog(PrimaryColorTextBox.Text) { Owner = this };
        if (picker.ShowDialog() == true)
            PrimaryColorTextBox.Text = picker.SelectedColor;
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(BackupLimitTextBox.Text.Trim(), out int limit) || limit < 1)
        {
            MessageBox.Show(this, Localization.Get("O limite de backups precisa ser um número igual ou superior a 1."),
                Localization.Get("Valor inválido"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseAccentColor(PrimaryColorTextBox.Text, out Color accentColor))
        {
            MessageBox.Show(this, Localization.Get("Indica uma cor hexadecimal válida no formato #RRGGBB."),
                Localization.Get("Cor inválida"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string? path;
        try
        {
            path = string.IsNullOrWhiteSpace(BackupPathTextBox.Text)
                ? null
                : Path.GetFullPath(Environment.ExpandEnvironmentVariables(BackupPathTextBox.Text.Trim()));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            MessageBox.Show(this, Localization.Format("O caminho indicado não é válido.\n\n{0}", ex.Message),
                Localization.Get("Caminho inválido"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string currentRoot = Path.GetFullPath(_backupService.BackupRoot);
        string requestedRoot = Path.GetFullPath(path ?? Path.Combine(_backupService.DataDirectory, "backups"));
        bool changingPath = !string.Equals(
            Path.TrimEndingDirectorySeparator(currentRoot),
            Path.TrimEndingDirectorySeparator(requestedRoot),
            StringComparison.OrdinalIgnoreCase);
        bool moveExistingBackups = false;
        bool hasExistingBackups;
        try
        {
            hasExistingBackups = changingPath && Directory.Exists(currentRoot) &&
                                 Directory.EnumerateDirectories(currentRoot, "BackupSteamMRM*").Any();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError(Localization.Get("Não foi possível verificar os backups existentes no local atual."), ex);
            return;
        }

        if (hasExistingBackups)
        {
            MessageBoxResult answer = MessageBox.Show(this,
                Localization.Get("O novo caminho será usado para backups futuros. Queres também mover para lá os backups existentes?\n\nSim: move os backups e passa a mostrá-los no novo caminho.\nNão: mantém os backups antigos no local atual."),
                Localization.Get("Alterar local dos backups"), MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question, MessageBoxResult.Yes);
            if (answer == MessageBoxResult.Cancel)
                return;
            moveExistingBackups = answer == MessageBoxResult.Yes;
        }

        string language = Localization.GetLanguageCode(LanguageComboBox.SelectedIndex);
        await RunOperationAsync(Localization.Get("A guardar definições..."), async () =>
        {
            await _backupService.SaveSettingsAsync(limit, path, AutoBackupOnCloseCheckBox.IsChecked == true,
                AutoBackupOnStartCheckBox.IsChecked == true, FormatAccentColor(accentColor), moveExistingBackups,
                StartWithWindowsCheckBox.IsChecked == true, StartMinimizedToTrayCheckBox.IsChecked == true,
                language: language);
            Localization.SetLanguage(language);
            Localization.Apply(this);
            UpdateTrayLanguage();
            ApplyAccentColor(accentColor);
            ApplyStartupRegistration();
            BackupPathDescription.Text = Localization.Format("Os backups novos serão guardados em: {0}",
                _backupService.BackupRoot);
            await RefreshBackupsAsync();
            FooterText.Text = Localization.Get("Definições guardadas.");
            UpdateSteamStatus();
            MessageBox.Show(this, Localization.Get("As definições foram guardadas."), "Steam Toolkit MRM",
                MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private async void PinBackup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BackupEntry backup })
            return;

        await RunOperationAsync(Localization.Get("A atualizar backup..."), async () =>
        {
            await _backupService.UpdateBackupAsync(backup.Folder, customName: null, pinned: !backup.Pinned);
            await RefreshBackupsAsync();
        });
    }

    private async void RenameBackup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BackupEntry backup })
            return;

        var dialog = new RenameDialog(backup.Name) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        await RunOperationAsync(Localization.Get("A atualizar nome..."), async () =>
        {
            await _backupService.UpdateBackupAsync(backup.Folder, dialog.BackupName, pinned: null);
            await RefreshBackupsAsync();
        });
    }

    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BackupEntry backup })
            return;

        MessageBoxResult answer = MessageBox.Show(this,
            Localization.Format("A Steam será fechada para restaurar este backup:\n\n{0}\n\nOs ficheiros existentes serão atualizados pelos ficheiros do backup. Continuar?",
                backup.Name),
            Localization.Get("Confirmar restauro"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        await RunOperationAsync(Localization.Get("A restaurar backup. A Steam poderá ser reiniciada..."), async () =>
        {
            await _backupService.RestoreBackupAsync(backup.Folder);
            FooterText.Text = Localization.Get("Backup restaurado. A Steam foi iniciada.");
            MessageBox.Show(this, Localization.Get("O backup foi restaurado com sucesso."),
                Localization.Get("Restauro concluído"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private async void DeleteBackup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BackupEntry backup })
            return;

        MessageBoxResult answer = MessageBox.Show(this,
            Localization.Format("Queres apagar permanentemente este backup?\n\n{0}", backup.Name),
            Localization.Get("Apagar backup"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        await RunOperationAsync(Localization.Get("A apagar backup..."), async () =>
        {
            await _backupService.DeleteBackupAsync(backup.Folder);
            await RefreshBackupsAsync();
            FooterText.Text = Localization.Get("Backup apagado.");
        });
    }

    private async void MonitorTimer_Tick(object? sender, EventArgs e)
    {
        if (_monitorCheckInProgress)
            return;
        _monitorCheckInProgress = true;
        try
        {
            bool running = _backupService.IsSteamGameRunning();
            if (running)
            {
                bool gameJustStarted = !_steamGameWasRunning;
                _steamGameWasRunning = true;
                SetSteamStatus(Localization.Get("Jogo em execução"), isActive: true);
                if (gameJustStarted && _backupService.Settings.AutoBackupOnGameStart)
                {
                    await RunOperationAsync(Localization.Get("Jogo iniciado. A criar backup automático..."), async () =>
                    {
                        await _backupService.CreateBackupAsync("auto");
                        await RefreshBackupsAsync();
                        FooterText.Text = Localization.Get("Backup automático criado ao iniciar o jogo.");
                    });
                }
            }
            else
            {
                if (_steamGameWasRunning)
                {
                    _steamGameWasRunning = false;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    if (!_backupService.IsSteamGameRunning() && _backupService.Settings.AutoBackupEnabled)
                    {
                        await RunOperationAsync(Localization.Get("Jogo fechado. A criar backup automático..."), async () =>
                        {
                            await _backupService.CreateBackupAsync("auto");
                            await RefreshBackupsAsync();
                            FooterText.Text = Localization.Get("Backup automático criado após o fecho do jogo.");
                        });
                    }
                }
                SetSteamStatus(Localization.Get("A monitorizar Steam"), isActive: false);
            }
        }
        catch (Exception ex)
        {
            SetSteamStatus(Localization.Get("Monitorização indisponível"), isActive: false);
            FooterText.Text = ex.Message;
        }
        finally
        {
            _monitorCheckInProgress = false;
        }
    }

    private async Task RefreshBackupsAsync()
    {
        try
        {
            IReadOnlyList<BackupEntry> backups = await _backupService.ListBackupsAsync();
            _backups.Clear();
            foreach (BackupEntry backup in backups)
                _backups.Add(backup);
            BackupCountText.Text = backups.Count.ToString();
            EmptyState.Visibility = backups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FooterText.Text = Localization.Format("Local: {0}", _backupService.BackupRoot);
            Localization.Apply(BackupList);
        }
        catch (Exception ex)
        {
            ShowError(Localization.Get("Não foi possível carregar os backups."), ex);
        }
    }

    private async Task RunOperationAsync(string status, Func<Task> operation)
    {
        if (_operationInProgress)
            return;

        _operationInProgress = true;
        CreateBackupButton.IsEnabled = false;
        OpenFolderButton.IsEnabled = false;
        OpenLudusaviButton.IsEnabled = false;
        SaveSettingsButton.IsEnabled = false;
        FooterText.Text = status;
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            ShowError(Localization.Get("A operação não foi concluída."), ex);
        }
        finally
        {
            _operationInProgress = false;
            CreateBackupButton.IsEnabled = true;
            OpenFolderButton.IsEnabled = true;
            OpenLudusaviButton.IsEnabled = true;
            SaveSettingsButton.IsEnabled = true;
        }
    }

    private void LoadSettingsIntoForm()
    {
        BackupLimitTextBox.Text = _backupService.Settings.BackupLimit.ToString();
        BackupPathTextBox.Text = _backupService.Settings.BackupPath ?? "";
        BackupPathDescription.Text = Localization.Format("Local ativo: {0}", _backupService.BackupRoot);
        AutoBackupOnCloseCheckBox.IsChecked = _backupService.Settings.AutoBackupEnabled;
        AutoBackupOnStartCheckBox.IsChecked = _backupService.Settings.AutoBackupOnGameStart;
        PrimaryColorTextBox.Text = _backupService.Settings.PrimaryColor;
        StartWithWindowsCheckBox.IsChecked = _backupService.Settings.StartWithWindows;
        StartMinimizedToTrayCheckBox.IsChecked = _backupService.Settings.StartMinimizedToTray;
        LanguageComboBox.SelectedIndex = Localization.GetLanguageIndex(_backupService.Settings.Language);
        UpdateAccentPreview();
    }

    private void UpdateSteamStatus()
    {
        try
        {
            bool running = _backupService.IsSteamGameRunning();
            _steamGameWasRunning = running;
            SetSteamStatus(Localization.Get(running ? "Jogo em execução" : "A monitorizar Steam"), running);
        }
        catch (Exception ex)
        {
            SetSteamStatus(Localization.Get("Steam não detetada"), isActive: false);
            FooterText.Text = ex.Message;
        }
    }

    private void SetSteamStatus(string text, bool isActive)
    {
        SteamStatusText.Text = text;
        StatusDot.Fill = isActive
            ? (Brush)Application.Current.Resources["GreenBrush"]
            : (Brush)Application.Current.Resources["AccentBrush"];
    }

    private void PrimaryColorTextChanged(object sender, TextChangedEventArgs e) => UpdateAccentPreview();

    private void UpdateAccentPreview()
    {
        if (AccentPreview is null || PrimaryColorTextBox is null ||
            !TryParseAccentColor(PrimaryColorTextBox.Text, out Color color))
            return;
        AccentPreview.Background = new SolidColorBrush(color);
    }

    private void ApplyAccentColor(string color)
    {
        if (TryParseAccentColor(color, out Color accentColor))
            ApplyAccentColor(accentColor);
    }

    private void ApplyAccentColor(Color accentColor)
    {
        Application.Current.Resources["AccentBrush"] = new SolidColorBrush(accentColor);
        if (AccentPreview is not null)
            AccentPreview.Background = new SolidColorBrush(accentColor);
    }

    private static string FormatAccentColor(Color color) =>
        $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private static bool TryParseAccentColor(string? value, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string hex = value.Trim();
        if (!hex.StartsWith('#'))
            hex = $"#{hex}";
        if (hex.Length != 7 || hex.Skip(1).Any(character => !Uri.IsHexDigit(character)))
            return false;

        color = Color.FromRgb(
            Convert.ToByte(hex.Substring(1, 2), 16),
            Convert.ToByte(hex.Substring(3, 2), 16),
            Convert.ToByte(hex.Substring(5, 2), 16));
        return true;
    }

    private void SetNavigationState(bool backupsSelected)
    {
        BackupsNav.Background = backupsSelected ? new SolidColorBrush(Color.FromRgb(38, 55, 86)) : Brushes.Transparent;
        BackupsNav.BorderBrush = backupsSelected ? new SolidColorBrush(Color.FromRgb(49, 75, 115)) : Brushes.Transparent;
        SettingsNav.Background = backupsSelected ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(38, 55, 86));
        SettingsNav.BorderBrush = backupsSelected ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(49, 75, 115));
    }

    private void ShowError(string message, Exception ex)
    {
        message = Localization.Get(message);
        FooterText.Text = $"{message} {ex.Message}";
        MessageBox.Show(this, $"{message}\n\n{ex.Message}", "Steam Toolkit MRM",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Localization.Get("Abrir Steam Toolkit MRM"), null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Localization.Get("Sair"), null, (_, _) => Dispatcher.Invoke(ExitApplication));

        var icon = new Forms.NotifyIcon
        {
            Text = "Steam Toolkit MRM",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                Environment.ProcessPath ??
                throw new InvalidOperationException(Localization.Get("Não foi possível detetar o caminho do executável.")))
                ?? System.Drawing.SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };
        icon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
        return icon;
    }

    private void UpdateTrayLanguage()
    {
        Forms.ContextMenuStrip? menu = _trayIcon.ContextMenuStrip;
        if (menu is null || menu.Items.Count < 3)
            return;

        menu.Items[0].Text = Localization.Get("Abrir Steam Toolkit MRM");
        menu.Items[2].Text = Localization.Get("Sair");
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _backupService.Settings.StartMinimizedToTray)
            HideToTray();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowClose)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            return;
        }

        e.Cancel = true;
        HideToTray();
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
        _trayIcon.Visible = true;
        _trayIcon.ShowBalloonTip(2000, "Steam Toolkit MRM",
            Localization.Get("A aplicação continua em execução na bandeja do sistema."), Forms.ToolTipIcon.Info);
    }

    private void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _allowClose = true;
        Close();
    }

    private void ApplyStartupRegistration()
    {
        try
        {
            const string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true);
            if (runKey is null)
                throw new InvalidOperationException("Não foi possível aceder às opções de arranque do utilizador atual.");

            const string valueName = "SteamToolkitMRM";
            if (_backupService.Settings.StartWithWindows)
            {
                string executable = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Não foi possível detetar o caminho do executável.");
                string arguments = _backupService.Settings.StartMinimizedToTray ? " --minimized" : "";
                runKey.SetValue(valueName, $"\"{executable}\"{arguments}");
            }
            else
            {
                runKey.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or InvalidOperationException)
        {
            ShowError(Localization.Get("Não foi possível atualizar o arranque automático do Windows."), ex);
        }
    }
}
