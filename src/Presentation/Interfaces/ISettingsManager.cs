using System;
using SimBlock.Presentation.Managers;

namespace SimBlock.Presentation.Interfaces
{
    /// <summary>
    /// Interface for managing application settings persistence
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Event fired when settings are changed
        /// </summary>
        event EventHandler? SettingsChanged;

        /// <summary>
        /// Saves all current settings to persistent storage
        /// </summary>
        void SaveSettings();

        /// <summary>
        /// Loads settings from persistent storage
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// Resets all settings to their default values
        /// </summary>
        void ResetToDefaults();

        /// <summary>
        /// Validates the current settings configuration
        /// </summary>
        /// <returns>True if settings are valid, false otherwise</returns>
        bool ValidateSettings();

        /// <summary>
        /// Validates the current settings configuration and returns detailed error messages
        /// </summary>
        /// <returns>Validation result with detailed error and warning messages</returns>
        SettingsManager.ValidationResult ValidateSettingsWithMessages();
    }
}