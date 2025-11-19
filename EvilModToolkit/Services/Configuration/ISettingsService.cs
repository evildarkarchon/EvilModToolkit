using System.Threading.Tasks;
using EvilModToolkit.Models;

namespace EvilModToolkit.Services.Configuration;

/// <summary>
/// Service for persisting and loading application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    /// <returns>The loaded settings, or default settings if file doesn't exist.</returns>
    Task<AppSettings> LoadSettingsAsync();

    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>
    /// Resets settings to default values.
    /// </summary>
    /// <returns>Default settings.</returns>
    AppSettings GetDefaultSettings();

    /// <summary>
    /// Gets the path to the settings file.
    /// </summary>
    string GetSettingsFilePath();
}