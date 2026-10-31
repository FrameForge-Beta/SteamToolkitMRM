using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace SteamToolkitMRM;

internal sealed class BackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly (string Source, string Backup)[] BackupTargets =
    [
        (Path.Combine("userdata"), "userdata"),
        (Path.Combine("appcache", "stats"), "appcache_stats"),
        (Path.Combine("depotcache"), "depotcache"),
        (Path.Combine("config", "stplug-in"), "stplug-in")
    ];

    private readonly string _settingsFile;
    private readonly string _defaultBackupRoot;
    private readonly string? _steamPathOverride;
    private readonly Func<CancellationToken, Task> _closeSteam;
    private readonly Action<string> _startSteam;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private AppSettings _settings;

    public BackupService() : this(null, null, null, null)
    {
    }

    internal BackupService(string? dataDirectory, string? steamPathOverride,
        Func<CancellationToken, Task>? closeSteam, Action<string>? startSteam)
    {
        DataDirectory = dataDirectory is null ? FindDataDirectory() : Path.GetFullPath(dataDirectory);
        if (dataDirectory is null)
            MigrateInstalledData(DataDirectory);
        _steamPathOverride = steamPathOverride is null ? null : Path.GetFullPath(steamPathOverride);
        _closeSteam = closeSteam ?? CloseSteamAsync;
        _startSteam = startSteam ?? StartSteam;
        Directory.CreateDirectory(DataDirectory);
        _settingsFile = Path.Combine(DataDirectory, "settings.json");
        _defaultBackupRoot = Path.Combine(DataDirectory, "backups");
        _settings = LoadSettings();
    }

    public string DataDirectory { get; }
    public string SteamPath => _steamPathOverride ?? FindSteamPath();
    public string BackupRoot => ResolveBackupRoot(_settings);
    public AppSettings Settings => _settings;

    public async Task<IReadOnlyList<BackupEntry>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        string root = BackupRoot;
        Directory.CreateDirectory(root);
        var backups = new List<BackupEntry>();

        foreach (string path in Directory.EnumerateDirectories(root, "BackupSteamMRM*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string folder = Path.GetFileName(path);
            BackupMetadata metadata = ReadMetadata(path);
            string date = ParseBackupDate(metadata.Timestamp, folder);
            long bytes = await GetDirectorySizeAsync(path, cancellationToken);
            backups.Add(new BackupEntry
            {
                Folder = folder,
                Name = string.IsNullOrWhiteSpace(metadata.CustomName) ? folder : metadata.CustomName,
                DateLabel = date,
                SizeLabel = FormatSize(bytes),
                SizeSortKey = bytes.ToString("D20", CultureInfo.InvariantCulture),
                Pinned = metadata.Pinned
            });
        }

        return backups.OrderByDescending(item => item.Folder, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task CreateBackupAsync(string trigger, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            string steamPath = SteamPath;
            if (!Directory.Exists(steamPath))
                throw new DirectoryNotFoundException(Localization.Format("A pasta da Steam não foi encontrada: {0}", steamPath));

            string root = BackupRoot;
            Directory.CreateDirectory(root);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            string folder = $"BackupSteamMRM-{timestamp}";
            string backupPath = Path.Combine(root, folder);
            for (int suffix = 1; Directory.Exists(backupPath); suffix++)
            {
                folder = $"BackupSteamMRM-{timestamp}-{suffix:00}";
                backupPath = Path.Combine(root, folder);
            }

            Directory.CreateDirectory(backupPath);
            try
            {
                int copiedFolders = 0;
                foreach ((string sourceRelative, string backupName) in BackupTargets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string source = Path.Combine(steamPath, sourceRelative);
                    if (!Directory.Exists(source))
                        continue;

                    await CopyDirectoryAsync(source, Path.Combine(backupPath, backupName), cancellationToken);
                    copiedFolders++;
                }

                if (copiedFolders == 0)
                    throw new InvalidOperationException(Localization.Get(
                        "Não foram encontradas pastas Steam para incluir no backup."));

                var metadata = new BackupMetadata
                {
                    Trigger = trigger,
                    Timestamp = timestamp,
                    Pinned = false
                };
                await WriteMetadataAsync(backupPath, metadata, cancellationToken);
                await CleanupBackupsAsync(cancellationToken);
            }
            catch
            {
                if (Directory.Exists(backupPath))
                    DeleteDirectoryTree(backupPath);
                throw;
            }
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task RestoreBackupAsync(string folder, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        bool restoreStarted = false;
        string steamPath = "";
        try
        {
            steamPath = SteamPath;
            string backupPath = GetBackupDirectory(folder);
            if (!Directory.Exists(backupPath))
                throw new DirectoryNotFoundException(Localization.Get("O backup selecionado já não existe."));

            int existingTargets = BackupTargets.Count(target =>
                Directory.Exists(Path.Combine(backupPath, target.Backup)));
            if (existingTargets == 0)
                throw new InvalidDataException(Localization.Get(
                    "Este backup não contém nenhuma das pastas Steam esperadas."));

            restoreStarted = true;
            await _closeSteam(cancellationToken);
            foreach ((string _, string backupName) in BackupTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string source = Path.Combine(backupPath, backupName);
                if (!Directory.Exists(source))
                    continue;

                string destination = backupName switch
                {
                    "appcache_stats" => Path.Combine(steamPath, "appcache", "stats"),
                    "stplug-in" => Path.Combine(steamPath, "config", "stplug-in"),
                    _ => Path.Combine(steamPath, backupName)
                };
                await CopyDirectoryAsync(source, destination, cancellationToken);
            }
        }
        finally
        {
            if (restoreStarted)
                _startSteam(steamPath);
            _operationLock.Release();
        }
    }

    public async Task DeleteBackupAsync(string folder, CancellationToken cancellationToken = default)
    {
        string path = GetBackupDirectory(folder);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(Localization.Get("O backup selecionado já não existe."));

        if (ReadMetadata(path).Pinned)
            throw new InvalidOperationException(Localization.Get("Este backup está fixado e não pode ser apagado."));

        cancellationToken.ThrowIfCancellationRequested();
        DeleteDirectoryTree(path);
        await Task.CompletedTask;
    }

    public async Task UpdateBackupAsync(string folder, string? customName, bool? pinned,
        CancellationToken cancellationToken = default)
    {
        string path = GetBackupDirectory(folder);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(Localization.Get("O backup selecionado já não existe."));

        BackupMetadata metadata = ReadMetadata(path);
        if (customName is not null)
            metadata.CustomName = string.IsNullOrWhiteSpace(customName) ? null : customName.Trim();
        if (pinned.HasValue)
            metadata.Pinned = pinned.Value;
        await WriteMetadataAsync(path, metadata, cancellationToken);
    }

    public async Task SaveSettingsAsync(int backupLimit, string? backupPath, bool autoBackupEnabled,
        bool autoBackupOnGameStart, string primaryColor, bool moveExistingBackups,
        bool startWithWindows, bool startMinimizedToTray,
        CancellationToken cancellationToken = default, string language = "pt")
    {
        if (backupLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(backupLimit),
                Localization.Get("O limite precisa ser pelo menos 1."));

        string? normalizedPath = string.IsNullOrWhiteSpace(backupPath)
            ? null
            : Path.GetFullPath(backupPath.Trim());
        string oldRoot = BackupRoot;
        string newRoot = normalizedPath ?? _defaultBackupRoot;

        if (IsInsideBackupDirectory(newRoot))
            throw new ArgumentException(Localization.Get(
                "A pasta dos backups não pode ficar dentro de um backup existente."), nameof(backupPath));

        Directory.CreateDirectory(newRoot);
        string writeTest = Path.Combine(newRoot, $".steamtoolkit-write-test-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(writeTest, "write test", cancellationToken);
        }
        finally
        {
            if (File.Exists(writeTest))
                File.Delete(writeTest);
        }

        List<string>? copiedBackups = null;
        if (!PathsEqual(oldRoot, newRoot) && moveExistingBackups)
            copiedBackups = await CopyBackupsToNewLocationAsync(oldRoot, newRoot, cancellationToken);

        string? previousBackupPath = _settings.BackupPath;
        int previousBackupLimit = _settings.BackupLimit;
        bool previousAutoBackup = _settings.AutoBackupEnabled;
        bool previousAutoBackupOnGameStart = _settings.AutoBackupOnGameStart;
        string previousPrimaryColor = _settings.PrimaryColor;
        bool previousStartWithWindows = _settings.StartWithWindows;
        bool previousStartMinimizedToTray = _settings.StartMinimizedToTray;
        string previousLanguage = _settings.Language;
        List<string> previousHistory = [.. _settings.BackupHistory];
        try
        {
            Directory.CreateDirectory(newRoot);
            if (!PathsEqual(oldRoot, newRoot) && !string.IsNullOrWhiteSpace(_settings.BackupPath))
            {
                _settings.BackupHistory ??= [];
                if (!_settings.BackupHistory.Contains(oldRoot, StringComparer.OrdinalIgnoreCase))
                    _settings.BackupHistory.Add(oldRoot);
                if (_settings.BackupHistory.Count > 5)
                    _settings.BackupHistory.RemoveAt(0);
            }

            _settings.BackupLimit = backupLimit;
            _settings.BackupPath = normalizedPath;
            _settings.AutoBackupEnabled = autoBackupEnabled;
            _settings.AutoBackupOnGameStart = autoBackupOnGameStart;
            _settings.PrimaryColor = primaryColor;
            _settings.StartWithWindows = startWithWindows;
            _settings.StartMinimizedToTray = startMinimizedToTray;
            _settings.Language = Localization.NormalizeLanguage(language);
            await SaveSettingsFileAsync(cancellationToken);
        }
        catch
        {
            _settings.BackupPath = previousBackupPath;
            _settings.BackupLimit = previousBackupLimit;
            _settings.AutoBackupEnabled = previousAutoBackup;
            _settings.AutoBackupOnGameStart = previousAutoBackupOnGameStart;
            _settings.PrimaryColor = previousPrimaryColor;
            _settings.StartWithWindows = previousStartWithWindows;
            _settings.StartMinimizedToTray = previousStartMinimizedToTray;
            _settings.Language = previousLanguage;
            _settings.BackupHistory = previousHistory;
            if (copiedBackups is not null)
            {
                foreach (string copied in copiedBackups.Where(Directory.Exists))
                    DeleteDirectoryTree(copied);
            }
            throw;
        }

        if (copiedBackups is not null)
        {
            foreach (string copied in copiedBackups)
            {
                string original = Path.Combine(oldRoot, Path.GetFileName(copied));
                if (Directory.Exists(original))
                    DeleteDirectoryTree(original);
            }
        }
        await CleanupBackupsAsync(cancellationToken);
    }

    public void OpenBackupFolder()
    {
        Directory.CreateDirectory(BackupRoot);
        Process.Start(new ProcessStartInfo
        {
            FileName = BackupRoot,
            UseShellExecute = true
        });
    }

    public void OpenLudusavi()
    {
        string executable = Path.Combine(AppContext.BaseDirectory, "ludusavi", "ludusavi.exe");
        if (!File.Exists(executable))
            executable = Path.Combine(DataDirectory, "ludusavi", "ludusavi.exe");
        if (!File.Exists(executable))
            throw new FileNotFoundException(Localization.Get(
                "Ludusavi não foi encontrado junto dos dados da aplicação."), executable);

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = true
        });
    }

    public bool IsSteamGameRunning()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            object? value = key?.GetValue("RunningAppID");
            return value is not null && Convert.ToInt64(value, CultureInfo.InvariantCulture) > 0;
        }
        catch (Exception ex) when (ex is SecurityException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(Localization.Get(
                "Não foi possível consultar o estado do jogo na Steam."), ex);
        }
    }

    private AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsFile))
            return new AppSettings();

        try
        {
            string json = File.ReadAllText(_settingsFile);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
                throw new InvalidDataException(Localization.Get("O ficheiro de configurações está vazio ou inválido."));
            settings.BackupLimit = Math.Max(1, settings.BackupLimit);
            settings.BackupHistory ??= [];
            return settings;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(Localization.Format("Não foi possível ler as configurações em {0}.",
                _settingsFile), ex);
        }
    }

    private async Task SaveSettingsFileAsync(CancellationToken cancellationToken)
    {
        string temporary = _settingsFile + ".tmp";
        string json = JsonSerializer.Serialize(_settings, JsonOptions);
        await File.WriteAllTextAsync(temporary, json, cancellationToken);
        File.Move(temporary, _settingsFile, overwrite: true);
    }

    private string ResolveBackupRoot(AppSettings settings)
    {
        return string.IsNullOrWhiteSpace(settings.BackupPath)
            ? _defaultBackupRoot
            : Path.GetFullPath(settings.BackupPath);
    }

    private string GetBackupDirectory(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) ||
            !folder.StartsWith("BackupSteamMRM", StringComparison.OrdinalIgnoreCase) ||
            folder.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            folder.Contains(Path.DirectorySeparatorChar) ||
            folder.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException(Localization.Get("Nome de backup inválido."), nameof(folder));

        return Path.Combine(BackupRoot, folder);
    }

    private static BackupMetadata ReadMetadata(string backupPath)
    {
        string file = Path.Combine(backupPath, "meta.json");
        if (!File.Exists(file))
            return new BackupMetadata();

        try
        {
            return JsonSerializer.Deserialize<BackupMetadata>(File.ReadAllText(file), JsonOptions)
                ?? new BackupMetadata();
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(Localization.Format("Metadados inválidos no backup {0}.",
                Path.GetFileName(backupPath)), ex);
        }
    }

    private static async Task WriteMetadataAsync(string backupPath, BackupMetadata metadata,
        CancellationToken cancellationToken)
    {
        string file = Path.Combine(backupPath, "meta.json");
        string temporary = file + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(metadata, JsonOptions), cancellationToken);
        File.Move(temporary, file, overwrite: true);
    }

    private async Task CleanupBackupsAsync(CancellationToken cancellationToken)
    {
        string root = BackupRoot;
        if (!Directory.Exists(root))
            return;

        var unpinned = new List<(string Path, string Name)>();
        foreach (string path in Directory.EnumerateDirectories(root, "BackupSteamMRM*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReadMetadata(path).Pinned)
                unpinned.Add((path, Path.GetFileName(path)));
        }

        foreach ((string path, _) in unpinned.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                     .Take(Math.Max(0, unpinned.Count - _settings.BackupLimit)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteDirectoryTree(path);
        }
        await Task.CompletedTask;
    }

    private async Task<List<string>> CopyBackupsToNewLocationAsync(string oldRoot, string newRoot,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(oldRoot))
            return [];

        string[] sources = Directory.EnumerateDirectories(oldRoot, "BackupSteamMRM*").ToArray();
        if (sources.Any(source => IsSubdirectory(newRoot, source)))
            throw new InvalidOperationException(Localization.Get(
                "A nova pasta não pode ficar dentro de um backup existente."));
        Directory.CreateDirectory(newRoot);
        foreach (string source in sources)
        {
            string destination = Path.Combine(newRoot, Path.GetFileName(source));
            if (Directory.Exists(destination))
                throw new IOException(Localization.Format("Já existe um backup com o mesmo nome no destino: {0}",
                    Path.GetFileName(source)));
        }

        var copied = new List<string>();
        try
        {
            foreach (string source in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string destination = Path.Combine(newRoot, Path.GetFileName(source));
                await CopyDirectoryAsync(source, destination, cancellationToken);
                copied.Add(destination);
            }
        }
        catch
        {
            foreach (string path in copied.Where(Directory.Exists))
                DeleteDirectoryTree(path);
            string partial = Path.Combine(newRoot, Path.GetFileName(sources[copied.Count]));
            if (Directory.Exists(partial))
                DeleteDirectoryTree(partial);
            throw;
        }
        return copied;
    }

    private static async Task CopyDirectoryAsync(string source, string destination,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string targetFile = Path.Combine(destination, Path.GetRelativePath(source, file));
            string? parent = Path.GetDirectoryName(targetFile);
            if (parent is not null)
                Directory.CreateDirectory(parent);
            if (File.Exists(targetFile))
                File.SetAttributes(targetFile, FileAttributes.Normal);
            await using (var input = new FileStream(file, FileMode.Open, FileAccess.Read,
                             FileShare.ReadWrite | FileShare.Delete, 81920, FileOptions.Asynchronous))
            await using (var output = new FileStream(targetFile, FileMode.Create, FileAccess.Write,
                             FileShare.None, 81920, FileOptions.Asynchronous))
            {
                await input.CopyToAsync(output, cancellationToken);
            }

            File.SetLastWriteTimeUtc(targetFile, File.GetLastWriteTimeUtc(file));
            File.SetAttributes(targetFile, File.GetAttributes(file));
        }

        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            string targetDirectory = Path.Combine(destination, Path.GetRelativePath(source, directory));
            Directory.SetLastWriteTimeUtc(targetDirectory, Directory.GetLastWriteTimeUtc(directory));
            File.SetAttributes(targetDirectory, File.GetAttributes(directory));
        }
    }

    private static void DeleteDirectoryTree(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        foreach (string directory in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                     .OrderByDescending(item => item.Length))
            File.SetAttributes(directory, FileAttributes.Directory);
        File.SetAttributes(path, FileAttributes.Directory);
        Directory.Delete(path, recursive: true);
    }

    private static Task<long> GetDirectorySizeAsync(string path, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            long bytes = 0;
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                bytes += new FileInfo(file).Length;
            }
            return bytes;
        }, cancellationToken);
    }

    private static async Task CloseSteamAsync(CancellationToken cancellationToken)
    {
        Process[] processes = Process.GetProcessesByName("steam");
        if (processes.Length == 0)
            return;

        foreach (Process process in processes)
        {
            try
            {
                if (!process.HasExited)
                    process.CloseMainWindow();
            }
            catch (InvalidOperationException)
            {
            }
        }

        await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
        foreach (Process process in processes)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static void StartSteam(string steamPath)
    {
        string executable = Path.Combine(steamPath, "steam.exe");
        if (!File.Exists(executable))
            throw new FileNotFoundException(Localization.Get(
                "Não foi possível reiniciar a Steam após a restauração."), executable);

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = steamPath,
            UseShellExecute = true
        });
    }

    private static string FindSteamPath()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            string? path = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(path))
            {
                string normalized = path.Replace('/', Path.DirectorySeparatorChar);
                if (File.Exists(Path.Combine(normalized, "steam.exe")))
                    return normalized;
            }
        }
        catch (Exception ex) when (ex is SecurityException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(Localization.Get(
                "Não foi possível detetar a instalação da Steam."), ex);
        }

        string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        if (File.Exists(Path.Combine(defaultPath, "steam.exe")))
            return defaultPath;
        throw new DirectoryNotFoundException(Localization.Get(
            "Steam não encontrada. Confirma a instalação e a chave SteamPath no registo."));
    }

    private static string FindDataDirectory()
    {
        string appDirectory = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(appDirectory, "steamtoolkit-installed.flag")))
        {
            string installedDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamToolkitMRM");
            Directory.CreateDirectory(installedDataDirectory);
            return installedDataDirectory;
        }

        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "settings.json")) ||
                Directory.Exists(Path.Combine(current.FullName, "backups")))
                return current.FullName;
            current = current.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private static void MigrateInstalledData(string dataDirectory)
    {
        string marker = Path.Combine(AppContext.BaseDirectory, "steamtoolkit-installed.flag");
        if (!File.Exists(marker))
            return;

        string legacySettings = Path.Combine(AppContext.BaseDirectory, "settings.json");
        string newSettings = Path.Combine(dataDirectory, "settings.json");
        if (!File.Exists(newSettings) && File.Exists(legacySettings))
            File.Copy(legacySettings, newSettings);

        string legacyBackups = Path.Combine(AppContext.BaseDirectory, "backups");
        string newBackups = Path.Combine(dataDirectory, "backups");
        if (!Directory.Exists(newBackups) && Directory.Exists(legacyBackups))
            CopyDirectorySync(legacyBackups, newBackups);
    }

    private static void CopyDirectorySync(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    private bool IsInsideBackupDirectory(string path)
    {
        string candidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (!Directory.Exists(BackupRoot))
            return false;

        foreach (string backup in Directory.EnumerateDirectories(BackupRoot, "BackupSteamMRM*"))
        {
            string normalizedBackup = Path.TrimEndingDirectorySeparator(Path.GetFullPath(backup));
            if (PathsEqual(candidate, normalizedBackup) ||
                candidate.StartsWith(normalizedBackup + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string ParseBackupDate(string? timestamp, string folder)
    {
        string? value = timestamp;
        if (string.IsNullOrWhiteSpace(value))
            value = folder.StartsWith("BackupSteamMRM-", StringComparison.OrdinalIgnoreCase)
                ? folder["BackupSteamMRM-".Length..]
                : folder;

        string[] formats = ["yyyy-MM-dd_HH-mm-ss", "yyyy-MM-dd_HH-mm-ss-ff"];
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out DateTime parsed)
            ? parsed.ToString("g", Localization.Culture)
            : "Data desconhecida";
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.##} {units[unit]}";
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSubdirectory(string candidate, string parent)
    {
        string normalizedCandidate = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidate));
        string normalizedParent = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent));
        return normalizedCandidate.StartsWith(normalizedParent + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }
}
