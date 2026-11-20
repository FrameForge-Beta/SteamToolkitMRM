using System.Text.Json;
using SteamToolkitMRM;

string root = Path.Combine(Path.GetTempPath(), $"SteamToolkitMRM-test-{Guid.NewGuid():N}");
string fakeSteam = Path.Combine(root, "Steam");
string fakeAppData = Path.Combine(root, "App");

try
{
    CreateFixture(Path.Combine(fakeSteam, "userdata"), "user", "userdata.bin", "test-userdata");
    CreateFixture(Path.Combine(fakeSteam, "userdata"), "user", "hidden.bin", "hidden-file");
    CreateFixture(Path.Combine(fakeSteam, "appcache", "stats"), "", "stats.bin", "test-stats");
    CreateFixture(Path.Combine(fakeSteam, "depotcache"), "", "depot.bin", "test-depotcache");
    File.SetAttributes(Path.Combine(fakeSteam, "userdata", "user", "hidden.bin"), FileAttributes.Hidden);

    Directory.CreateDirectory(fakeAppData);
    await File.WriteAllTextAsync(Path.Combine(fakeAppData, "settings.json"),
        """{"backup_limit":5,"backup_history":{},"language":"pt"}""");

    var service = new BackupService(fakeAppData, fakeSteam, _ => Task.CompletedTask, _ => { });
    Assert(service.Settings.AutoBackupOnGameStart,
        "Automatic backups on game start should default to enabled.");
    Assert(service.Settings.AutoBackupEnabled,
        "Existing automatic backups on game close should remain enabled.");
    Assert(service.Settings.PrimaryColor == "#8B5CF6",
        "The default accent color should be purple.");
    Assert(service.Settings.StartWithWindows && service.Settings.StartMinimizedToTray,
        "Windows startup and tray startup should default to enabled.");
    Assert(service.Settings.Language == "pt",
        "Portuguese should remain the default application language.");
    const string legacyFolder = "BackupSteamMRM-2026-07-16_13-04-11";
    string legacyBackup = Path.Combine(fakeAppData, "backups", legacyFolder);
    Directory.CreateDirectory(Path.Combine(legacyBackup, "userdata"));
    await File.WriteAllTextAsync(Path.Combine(legacyBackup, "meta.json"),
        """{"trigger":"auto","timestamp":"2026-07-16_13-04-11","pinned":true,"custom_name":"Legacy Lua Tools backup"}""");

    await service.CreateBackupAsync("manual");

    IReadOnlyList<BackupEntry> entries = await service.ListBackupsAsync();
    Assert(entries.Count == 2, $"Expected new and legacy backups, found {entries.Count}.");
    BackupEntry legacyEntry = entries.Single(entry => entry.Folder == legacyFolder);
    Assert(legacyEntry.Name == "Legacy Lua Tools backup" && legacyEntry.Pinned,
        "Existing backup metadata was not recognized.");
    BackupEntry backup = entries.Single(entry => entry.Folder != legacyFolder);
    string backupRoot = Path.Combine(fakeAppData, "backups", backup.Folder);

    Assert(File.ReadAllText(Path.Combine(backupRoot, "userdata", "user", "userdata.bin")) == "test-userdata",
        "Userdata content was not copied.");
    Assert(File.ReadAllText(Path.Combine(backupRoot, "appcache_stats", "stats.bin")) == "test-stats",
        "Appcache stats content was not copied.");
    Assert(File.ReadAllText(Path.Combine(backupRoot, "depotcache", "depot.bin")) == "test-depotcache",
        "Depotcache content was not copied.");
    string copiedUserdata = Path.Combine(backupRoot, "userdata", "user", "hidden.bin");
    Assert((File.GetAttributes(copiedUserdata) & FileAttributes.Hidden) != 0,
        "Hidden file attribute was not preserved.");

    using JsonDocument metadata = JsonDocument.Parse(
        await File.ReadAllTextAsync(Path.Combine(backupRoot, "meta.json")));
    Assert(metadata.RootElement.GetProperty("trigger").GetString() == "manual",
        "Backup trigger was not saved.");
    Assert(metadata.RootElement.GetProperty("pinned").GetBoolean() == false,
        "New backup should not be pinned.");
    Assert(metadata.RootElement.TryGetProperty("timestamp", out _),
        "Backup timestamp is missing.");

    string userdataFile = Path.Combine(fakeSteam, "userdata", "user", "userdata.bin");
    await File.WriteAllTextAsync(userdataFile, "modified-after-backup");
    string extraFile = Path.Combine(fakeSteam, "userdata", "user", "keep-existing.bin");
    await File.WriteAllTextAsync(extraFile, "preserve-existing-xcopy-file");
    await service.RestoreBackupAsync(backup.Folder);
    Assert(await File.ReadAllTextAsync(userdataFile) == "test-userdata",
        "Restore did not put the backed-up userdata file back.");
    Assert(await File.ReadAllTextAsync(extraFile) == "preserve-existing-xcopy-file",
        "Restore did not preserve files that were not in the backup.");

    string chosenBackupPath = Path.Combine(root, "Chosen Backup Folder");
    await service.SaveSettingsAsync(5, chosenBackupPath, autoBackupEnabled: true, autoBackupOnGameStart: true,
        primaryColor: "#8B5CF6", moveExistingBackups: true,
        startWithWindows: true, startMinimizedToTray: true);
    Assert(service.BackupRoot == Path.GetFullPath(chosenBackupPath),
        "The selected backup path was not applied.");
    Assert(Directory.Exists(Path.Combine(chosenBackupPath, backup.Folder)),
        "Existing backup was not moved to the selected path.");
    Assert(!Directory.Exists(Path.Combine(fakeAppData, "backups", backup.Folder)),
        "Existing backup remained in the previous location after moving.");
    Assert((await service.ListBackupsAsync()).Count == 2,
        "Backups in the selected path were not listed.");

    await service.CreateBackupAsync("manual");
    Assert((await service.ListBackupsAsync()).Count == 3,
        "A new backup was not created in the selected path.");

    string newestFolder = (await service.ListBackupsAsync())
        .OrderByDescending(entry => entry.Folder, StringComparer.OrdinalIgnoreCase)
        .First().Folder;
    await service.CompressBackupManualAsync(newestFolder, "zip");
    IReadOnlyList<BackupEntry> afterCompression = await service.ListBackupsAsync();
    BackupEntry? compressedEntry = afterCompression.FirstOrDefault(entry => entry.Folder == newestFolder);
    Assert(compressedEntry is not null && compressedEntry.Compressed,
        "A compressed backup did not appear in the backup list.");
    Assert(compressedEntry!.IsManuallyCompressed && compressedEntry.IsProtectedFromDeletion,
        "A manually compressed backup was not protected from deletion.");
    bool deleteRejected = false;
    try
    {
        await service.DeleteBackupAsync(newestFolder);
    }
    catch (InvalidOperationException)
    {
        deleteRejected = true;
    }
    Assert(deleteRejected, "A manually compressed backup was deleted.");
    await service.RestoreBackupAsync(newestFolder);
    Assert(File.Exists(Path.Combine(fakeSteam, "userdata", "user", "userdata.bin")),
        "Restoring a compressed backup did not restore its contents.");

    string invalidNestedPath = Path.Combine(chosenBackupPath, backup.Folder, "nested");
    bool rejectedNestedPath = false;
    try
    {
        await service.SaveSettingsAsync(5, invalidNestedPath, autoBackupEnabled: true,
            autoBackupOnGameStart: true, primaryColor: "#8B5CF6", moveExistingBackups: false,
            startWithWindows: true, startMinimizedToTray: true);
    }
    catch (ArgumentException)
    {
        rejectedNestedPath = true;
    }
    Assert(rejectedNestedPath && service.BackupRoot == Path.GetFullPath(chosenBackupPath),
        "A backup directory nested inside an existing backup was not rejected.");

    await service.SaveSettingsAsync(5, chosenBackupPath, autoBackupEnabled: false, autoBackupOnGameStart: false,
        primaryColor: "#4F46E5", moveExistingBackups: false,
        startWithWindows: false, startMinimizedToTray: false, language: "ru");
    var reloaded = new BackupService(fakeAppData, fakeSteam, _ => Task.CompletedTask, _ => { });
    Assert(!reloaded.Settings.AutoBackupEnabled && !reloaded.Settings.AutoBackupOnGameStart,
        "Automatic backup preferences were not persisted.");
    Assert(reloaded.Settings.PrimaryColor == "#4F46E5",
        "The selected accent color was not persisted.");
    Assert(!reloaded.Settings.StartWithWindows && !reloaded.Settings.StartMinimizedToTray,
        "Windows startup preferences were not persisted.");
    Assert(reloaded.Settings.Language == "ru",
        "The selected application language was not persisted.");
    Assert(reloaded.BackupRoot == Path.GetFullPath(chosenBackupPath),
        "The selected backup path was not persisted.");
    Localization.SetLanguage("en");
    Assert(Localization.Get("Definições") == "Settings",
        "English translations were not applied.");
    Localization.SetLanguage("es");
    Assert(Localization.Get("Definições") == "Configuración",
        "Spanish translations were not applied.");
    Localization.SetLanguage("ru");
    Assert(Localization.Get("Definições") == "Настройки",
        "Russian translations were not applied.");
    Assert(Enumerable.Range(0, 4)
            .Select(Localization.GetLanguageCode)
            .Select(Localization.GetLanguageIndex)
            .SequenceEqual(Enumerable.Range(0, 4)),
        "The language selector indices do not map consistently to saved languages.");
    Localization.SetLanguage("pt");

    Console.WriteLine("PASS: copied userdata, appcache stats and depotcache.");
    Console.WriteLine("PASS: preserved the hidden file attribute and legacy-compatible metadata.");
    Console.WriteLine("PASS: listed an existing legacy backup and preserved its custom name and pinned state.");
    Console.WriteLine("PASS: restored backed-up files and preserved unrelated existing files.");
    Console.WriteLine("PASS: compressed backups are listed, protected and restorable.");
    Console.WriteLine("PASS: purple default, game automation, Windows startup preferences, and custom accent persist.");
    Console.WriteLine("PASS: Portuguese, English, Spanish, and Russian language selection persists and translates.");
    Console.WriteLine("PASS: selected backup folder works with spaces, moves and lists existing backups, and receives new backups.");
}
finally
{
    if (Directory.Exists(root))
        Directory.Delete(root, recursive: true);
}

static void CreateFixture(string folder, string childFolder, string fileName, string content)
{
    string directory = string.IsNullOrEmpty(childFolder) ? folder : Path.Combine(folder, childFolder);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, fileName), content);
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
