using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EvilModToolkit.Models;
using Microsoft.Extensions.Logging;

namespace EvilModToolkit.Services.Configuration;

/// <summary>
/// JSON serialization context for AppSettings (trim-safe).
/// </summary>
[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal partial class AppSettingsJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Service for persisting application settings to JSON.
/// </summary>
public class SettingsService : ISettingsService
{
    private const string SettingsFileName = "settings.json";
    private const string ApplicationName = "EvilModToolkit";

    private readonly ILogger<SettingsService> _logger;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            var settingsPath = GetSettingsFilePath();

            if (!File.Exists(settingsPath))
            {
                _logger.LogInformation("Settings file not found, using defaults: {Path}", settingsPath);
                return GetDefaultSettings();
            }

            var json = await File.ReadAllTextAsync(settingsPath);
            var settings = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings);

            if (settings == null)
            {
                _logger.LogWarning("Failed to deserialize settings, using defaults");
                return GetDefaultSettings();
            }

            _logger.LogDebug("Settings loaded successfully from {Path}", settingsPath);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings, using defaults");
            return GetDefaultSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var settingsPath = GetSettingsFilePath();
            var directory = Path.GetDirectoryName(settingsPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Created settings directory: {Directory}", directory);
            }

            var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
            await File.WriteAllTextAsync(settingsPath, json);

            _logger.LogInformation("Settings saved successfully to {Path}", settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            throw;
        }
    }

    /// <inheritdoc />
    public AppSettings GetDefaultSettings()
    {
        return new AppSettings
        {
            GamePathOverride = null,
            MO2PathOverride = null,
            VortexPathOverride = null,
            LastF4SEScanDirectory = null,
            LastBA2PatchDirectory = null,
            ScanF4SERecursively = false,
            WindowWidth = 1200,
            WindowHeight = 800,
            ShowHiddenFiles = false,
            Theme = "System",
            LogLevel = "Information"
        };
    }

    /// <inheritdoc />
    public virtual string GetSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, ApplicationName);
        return Path.Combine(appFolder, SettingsFileName);
    }
}
