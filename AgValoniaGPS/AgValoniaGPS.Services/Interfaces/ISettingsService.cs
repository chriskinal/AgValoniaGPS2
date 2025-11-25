using AgValoniaGPS.Models;
using System;

namespace AgValoniaGPS.Services.Interfaces
{
    /// <summary>
    /// Service for managing application settings persistence
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Current application settings
        /// </summary>
        AppSettings Settings { get; }

        /// <summary>
        /// Raised when settings are loaded
        /// </summary>
        event EventHandler<AppSettings>? SettingsLoaded;

        /// <summary>
        /// Raised when settings are saved
        /// </summary>
        event EventHandler<AppSettings>? SettingsSaved;

        /// <summary>
        /// Load settings from disk
        /// </summary>
        /// <returns>True if settings were loaded successfully</returns>
        bool Load();

        /// <summary>
        /// Save settings to disk
        /// </summary>
        /// <returns>True if settings were saved successfully</returns>
        bool Save();

        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// Get the path where settings are stored
        /// </summary>
        string GetSettingsFilePath();
    }
}
