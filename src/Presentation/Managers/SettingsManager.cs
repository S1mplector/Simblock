using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;
using SimBlock.Core.Domain.Enums;
using SimBlock.Core.Domain.Entities;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages application settings persistence
    /// </summary>
    public class SettingsManager : ISettingsManager
    {
        private readonly UISettings _uiSettings;
        private readonly ILogger<SettingsManager> _logger;
        private readonly string _settingsPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public event EventHandler? SettingsChanged;

        public SettingsManager(UISettings uiSettings, ILogger<SettingsManager> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Store settings in the application data folder
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimBlock");
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            LoadSettings();
        }

        /// <summary>
        /// Saves all current settings to persistent storage
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var settingsData = new SettingsData
                {
                    // Timer intervals
                    StatusUpdateInterval = _uiSettings.StatusUpdateInterval,
                    
                    // Theme settings
                    CurrentTheme = _uiSettings.CurrentTheme,
                    
                    // Emergency unlock shortcut settings
                    EmergencyUnlockKey = _uiSettings.EmergencyUnlockKey,
                    EmergencyUnlockRequiresCtrl = _uiSettings.EmergencyUnlockRequiresCtrl,
                    EmergencyUnlockRequiresAlt = _uiSettings.EmergencyUnlockRequiresAlt,
                    EmergencyUnlockRequiresShift = _uiSettings.EmergencyUnlockRequiresShift,
                    
                    // Startup settings
                    StartWithWindows = _uiSettings.StartWithWindows,
                    
                    // Advanced blocking settings
                    KeyboardBlockingMode = _uiSettings.KeyboardBlockingMode,
                    MouseBlockingMode = _uiSettings.MouseBlockingMode,
                    AdvancedKeyboardConfig = _uiSettings.AdvancedKeyboardConfig,
                    AdvancedMouseConfig = _uiSettings.AdvancedMouseConfig,
                    
                    // Resource usage thresholds
                    CpuWarningThreshold = _uiSettings.CpuWarningThreshold,
                    CpuErrorThreshold = _uiSettings.CpuErrorThreshold,
                    MemoryWarningThreshold = _uiSettings.MemoryWarningThreshold,
                    MemoryErrorThreshold = _uiSettings.MemoryErrorThreshold
                };

                var json = JsonSerializer.Serialize(settingsData, _jsonOptions);
                File.WriteAllText(_settingsPath, json);
                _logger.LogDebug("Settings saved successfully to {SettingsPath}", _settingsPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings to {SettingsPath}", _settingsPath);
            }
        }

        /// <summary>
        /// Loads settings from persistent storage
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settingsData = JsonSerializer.Deserialize<SettingsData>(json, _jsonOptions);
                    
                    if (settingsData != null)
                    {
                        ApplyLoadedSettings(settingsData);
                        _logger.LogInformation("Settings loaded successfully from {SettingsPath}", _settingsPath);
                    }
                    else
                    {
                        _logger.LogWarning("Settings file contained null data, using defaults");
                        InitializeDefaultSettings();
                    }
                }
                else
                {
                    _logger.LogInformation("No settings file found, using defaults");
                    InitializeDefaultSettings();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings from {SettingsPath}, using defaults", _settingsPath);
                InitializeDefaultSettings();
            }
        }

        /// <summary>
        /// Resets all settings to their default values
        /// </summary>
        public void ResetToDefaults()
        {
            try
            {
                _logger.LogInformation("Resetting all settings to defaults");
                InitializeDefaultSettings();
                SaveSettings();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings to defaults");
            }
        }

        /// <summary>
        /// Validates the current settings configuration
        /// </summary>
        public bool ValidateSettings()
        {
            var validationResult = ValidateSettingsWithMessages();
            return validationResult.IsValid;
        }

        /// <summary>
        /// Validates the current settings configuration and returns detailed error messages
        /// </summary>
        public ValidationResult ValidateSettingsWithMessages()
        {
            var result = new ValidationResult();
            
            try
            {
                // Validate emergency unlock key is set
                if (_uiSettings.EmergencyUnlockKey == Keys.None)
                {
                    result.AddError("Emergency unlock key is not set");
                    _logger.LogWarning("Emergency unlock key is not set");
                }

                // Validate advanced blocking configurations
                if (_uiSettings.KeyboardBlockingMode == BlockingMode.Advanced)
                {
                    if (_uiSettings.AdvancedKeyboardConfig == null)
                    {
                        result.AddError("Advanced keyboard blocking mode is enabled but no configuration is set");
                        _logger.LogWarning("Advanced keyboard blocking mode is enabled but no configuration is set");
                    }
                    else
                    {
                        // Ensure at least one input method is not blocked for emergency unlock
                        if (_uiSettings.AdvancedKeyboardConfig.BlockAllCategories())
                        {
                            result.AddError("Advanced keyboard configuration blocks all input - emergency unlock may not work");
                            _logger.LogWarning("Advanced keyboard configuration blocks all input - emergency unlock may not work");
                        }
                    }
                }

                if (_uiSettings.MouseBlockingMode == BlockingMode.Advanced)
                {
                    if (_uiSettings.AdvancedMouseConfig == null)
                    {
                        result.AddError("Advanced mouse blocking mode is enabled but no configuration is set");
                        _logger.LogWarning("Advanced mouse blocking mode is enabled but no configuration is set");
                    }
                    else
                    {
                        // Ensure at least one mouse action is not blocked for emergency unlock
                        if (_uiSettings.AdvancedMouseConfig.BlockAllActions())
                        {
                            result.AddError("Advanced mouse configuration blocks all mouse actions - emergency unlock may not work");
                            _logger.LogWarning("Advanced mouse configuration blocks all mouse actions - emergency unlock may not work");
                        }
                    }
                }

                // Validate combined blocking scenarios
                if (_uiSettings.KeyboardBlockingMode == BlockingMode.Advanced &&
                    _uiSettings.MouseBlockingMode == BlockingMode.Advanced)
                {
                    // If both keyboard and mouse are in advanced mode, ensure at least one input method is available
                    bool keyboardCompletelyBlocked = _uiSettings.AdvancedKeyboardConfig?.BlockAllCategories() == true;
                    bool mouseCompletelyBlocked = _uiSettings.AdvancedMouseConfig?.BlockAllActions() == true;
                    
                    if (keyboardCompletelyBlocked && mouseCompletelyBlocked)
                    {
                        result.AddError("Both keyboard and mouse are completely blocked - emergency unlock will be impossible");
                        _logger.LogWarning("Both keyboard and mouse are completely blocked - emergency unlock will be impossible");
                    }
                }

                // Validate emergency unlock key accessibility in advanced keyboard mode
                if (_uiSettings.KeyboardBlockingMode == BlockingMode.Advanced &&
                    _uiSettings.AdvancedKeyboardConfig != null)
                {
                    // Check if the emergency unlock key itself is blocked
                    if (_uiSettings.AdvancedKeyboardConfig.IsKeyBlocked(_uiSettings.EmergencyUnlockKey))
                    {
                        result.AddError($"Emergency unlock key ({_uiSettings.EmergencyUnlockKey}) is blocked in advanced keyboard configuration");
                        _logger.LogWarning("Emergency unlock key ({0}) is blocked in advanced keyboard configuration", _uiSettings.EmergencyUnlockKey);
                    }
                }

                // Validate resource thresholds
                if (_uiSettings.CpuWarningThreshold >= _uiSettings.CpuErrorThreshold)
                {
                    result.AddError("CPU warning threshold should be less than error threshold");
                    _logger.LogWarning("CPU warning threshold should be less than error threshold");
                }

                if (_uiSettings.MemoryWarningThreshold >= _uiSettings.MemoryErrorThreshold)
                {
                    result.AddError("Memory warning threshold should be less than error threshold");
                    _logger.LogWarning("Memory warning threshold should be less than error threshold");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.AddError($"Error validating settings: {ex.Message}");
                _logger.LogError(ex, "Error validating settings");
                return result;
            }
        }

        private void ApplyLoadedSettings(SettingsData settingsData)
        {
            // Timer intervals
            _uiSettings.StatusUpdateInterval = settingsData.StatusUpdateInterval;
            
            // Theme settings
            _uiSettings.ApplyTheme(settingsData.CurrentTheme);
            
            // Emergency unlock shortcut settings (now configurable)
            _uiSettings.EmergencyUnlockKey = settingsData.EmergencyUnlockKey;
            _uiSettings.EmergencyUnlockRequiresCtrl = settingsData.EmergencyUnlockRequiresCtrl;
            _uiSettings.EmergencyUnlockRequiresAlt = settingsData.EmergencyUnlockRequiresAlt;
            _uiSettings.EmergencyUnlockRequiresShift = settingsData.EmergencyUnlockRequiresShift;
            
            // Startup settings
            _uiSettings.StartWithWindows = settingsData.StartWithWindows;
            
            // Advanced blocking settings
            _uiSettings.KeyboardBlockingMode = settingsData.KeyboardBlockingMode;
            _uiSettings.MouseBlockingMode = settingsData.MouseBlockingMode;
            _uiSettings.AdvancedKeyboardConfig = settingsData.AdvancedKeyboardConfig ?? new AdvancedKeyboardConfiguration();
            _uiSettings.AdvancedMouseConfig = settingsData.AdvancedMouseConfig ?? new AdvancedMouseConfiguration();
            
            // Resource usage thresholds
            _uiSettings.CpuWarningThreshold = settingsData.CpuWarningThreshold;
            _uiSettings.CpuErrorThreshold = settingsData.CpuErrorThreshold;
            _uiSettings.MemoryWarningThreshold = settingsData.MemoryWarningThreshold;
            _uiSettings.MemoryErrorThreshold = settingsData.MemoryErrorThreshold;
        }

        private void InitializeDefaultSettings()
        {
            // Settings are already initialized with defaults in UISettings constructor
            // Just ensure advanced configurations are not null
            if (_uiSettings.AdvancedKeyboardConfig == null)
            {
                _uiSettings.AdvancedKeyboardConfig = new AdvancedKeyboardConfiguration();
            }
            
            if (_uiSettings.AdvancedMouseConfig == null)
            {
                _uiSettings.AdvancedMouseConfig = new AdvancedMouseConfiguration();
            }
        }

        /// <summary>
        /// Represents the result of settings validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> ErrorMessages { get; set; } = new();
            public List<string> WarningMessages { get; set; } = new();

            public ValidationResult()
            {
                IsValid = true;
            }

            public void AddError(string message)
            {
                ErrorMessages.Add(message);
                IsValid = false;
            }

            public void AddWarning(string message)
            {
                WarningMessages.Add(message);
            }
        }

        /// <summary>
        /// Data class for settings persistence
        /// </summary>
        private class SettingsData
        {
            // Timer intervals
            public int StatusUpdateInterval { get; set; } = 1000;

            // Theme settings
            public Theme CurrentTheme { get; set; } = Theme.Light;

            // Emergency unlock shortcut settings
            public Keys EmergencyUnlockKey { get; set; } = Keys.U;
            public bool EmergencyUnlockRequiresCtrl { get; set; } = true;
            public bool EmergencyUnlockRequiresAlt { get; set; } = true;
            public bool EmergencyUnlockRequiresShift { get; set; } = false;

            // Startup settings
            public bool StartWithWindows { get; set; } = false;

            // Advanced blocking settings
            public BlockingMode KeyboardBlockingMode { get; set; } = BlockingMode.Simple;
            public BlockingMode MouseBlockingMode { get; set; } = BlockingMode.Simple;
            public AdvancedKeyboardConfiguration? AdvancedKeyboardConfig { get; set; }
            public AdvancedMouseConfiguration? AdvancedMouseConfig { get; set; }

            // Resource usage thresholds
            public float CpuWarningThreshold { get; set; } = 2.0f;
            public float CpuErrorThreshold { get; set; } = 5.0f;
            public long MemoryWarningThreshold { get; set; } = 50; // MB
            public long MemoryErrorThreshold { get; set; } = 80; // MB
        }
    }
}