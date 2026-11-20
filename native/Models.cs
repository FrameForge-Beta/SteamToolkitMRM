using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Windows.Data;

namespace SteamToolkitMRM;

internal sealed class AppSettings
{
    [JsonPropertyName("backup_limit")]
    public int BackupLimit { get; set; } = 5;

    [JsonPropertyName("backup_path")]
    public string? BackupPath { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = "pt";

    [JsonPropertyName("auto_backup_enabled")]
    public bool AutoBackupEnabled { get; set; } = true;

    [JsonPropertyName("auto_backup_on_game_start")]
    public bool AutoBackupOnGameStart { get; set; } = true;

    [JsonPropertyName("primary_color")]
    public string PrimaryColor { get; set; } = "#8B5CF6";

    [JsonPropertyName("start_with_windows")]
    public bool StartWithWindows { get; set; } = true;

    [JsonPropertyName("start_minimized_to_tray")]
    public bool StartMinimizedToTray { get; set; } = true;

    [JsonPropertyName("backup_history")]
    [JsonConverter(typeof(BackupHistoryConverter))]
    public List<string> BackupHistory { get; set; } = [];

    [JsonPropertyName("compression_enabled")]
    public bool CompressionEnabled { get; set; } = false;

    [JsonPropertyName("compression_format")]
    public string CompressionFormat { get; set; } = "zip"; // "zip" or "7z"

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

internal sealed class BackupHistoryConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
            return JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? [];

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using JsonDocument _ = JsonDocument.ParseValue(ref reader);
            return [];
        }

        if (reader.TokenType == JsonTokenType.Null)
            return [];

        throw new JsonException("O histórico de backups tem um formato inválido.");
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

internal sealed class BackupMetadata
{
    [JsonPropertyName("trigger")]
    public string? Trigger { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("pinned")]
    public bool Pinned { get; set; }

    [JsonPropertyName("custom_name")]
    public string? CustomName { get; set; }

    [JsonPropertyName("compressed")]
    public bool Compressed { get; set; }

    [JsonPropertyName("compression_format")]
    public string? CompressionFormat { get; set; }

    [JsonPropertyName("compression_source")]
    public string? CompressionSource { get; set; } // "manual" or "auto"

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

internal sealed class BackupEntry
{
    public required string Folder { get; init; }
    public required string Name { get; init; }
    public required string DateLabel { get; init; }
    public required string SizeLabel { get; init; }
    public required string SizeSortKey { get; init; }
    public bool Pinned { get; init; }
    public bool Compressed { get; init; }
    public string? CompressionFormat { get; init; }
    public string? CompressionSource { get; init; }
    public bool IsManuallyCompressed => string.Equals(CompressionSource, "manual", StringComparison.OrdinalIgnoreCase);
    public bool IsAutoCompressed => string.Equals(CompressionSource, "auto", StringComparison.OrdinalIgnoreCase);
    public bool IsNotManuallyCompressed => !IsManuallyCompressed;
    public bool IsNotCompressed => !Compressed;
    public bool IsProtectedFromDeletion => IsManuallyCompressed || Pinned;
}

internal sealed class PinLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Localization.Get(value is true ? "Desafixar" : "Fixar");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

internal sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

internal sealed class CompressionBadgeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not BackupEntry entry || !entry.Compressed)
            return string.Empty;

        string format = (entry.CompressionFormat ?? "zip").ToUpperInvariant();
        return $"{Localization.Get("Compactado")} · {format}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
