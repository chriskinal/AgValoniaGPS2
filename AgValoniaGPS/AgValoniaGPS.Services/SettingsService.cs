using AgValoniaGPS.Models;
using AgValoniaGPS.Services.Interfaces;
using System;
using System.IO;
using System.Text.Json;

namespace AgValoniaGPS.Services
{
    /// <summary>
    /// Service for managing application settings persistence using JSON
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string SettingsFileName = "appsettings.json";
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;

        public AppSettings Settings { get; private set; }

        public event EventHandler<AppSettings>? SettingsLoaded;
        public event EventHandler<AppSettings>? SettingsSaved;

        public SettingsService()
        {
            // Store settings in user's AppData\Local folder
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AgValoniaGPS");

            _settingsFilePath = Path.Combine(_settingsDirectory, SettingsFileName);

            // Initialize with defaults
            Settings = new AppSettings();
        }

        public bool Load()
        {
            try
            {

                if (!File.Exists(_settingsFilePath))
                {
                    // First run - use defaults
                    Settings = new AppSettings { IsFirstRun = true };
                    return false;
                }

                var json = File.ReadAllText(_settingsFilePath);

                // Use same options as Save to match camelCase property names
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true // Add case-insensitive matching
                };

                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, options);

                if (loadedSettings != null)
                {
                    Settings = loadedSettings;
                    Settings.IsFirstRun = false;
                    Settings.LastRunDate = DateTime.Now;
                    SettingsLoaded?.Invoke(this, Settings);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Settings = new AppSettings();
                return false;
            }
        }

        public bool Save()
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(_settingsDirectory))
                {
                    Directory.CreateDirectory(_settingsDirectory);
                }

                // Update last run date
                Settings.LastRunDate = DateTime.Now;

                // Serialize with indentation for readability
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(Settings, options);

                // Write to temp file first, then move (atomic operation)
                var tempFile = _settingsFilePath + ".tmp";
                File.WriteAllText(tempFile, json);

                if (File.Exists(_settingsFilePath))
                {
                    File.Delete(_settingsFilePath);
                }

                File.Move(tempFile, _settingsFilePath);

                SettingsSaved?.Invoke(this, Settings);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                return false;
            }
        }

        public void ResetToDefaults()
        {
            Settings = new AppSettings
            {
                IsFirstRun = false,
                LastRunDate = DateTime.Now
            };
        }

        public string GetSettingsFilePath()
        {
            return _settingsFilePath;
        }
    }
}
