using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Interfaces;
using SimBlock.Presentation.Configuration;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages keyboard shortcuts and their handlers
    /// </summary>
    public class KeyboardShortcutManager : IKeyboardShortcutManager
    {
        private readonly ILogger<KeyboardShortcutManager> _logger;
        private readonly UISettings _uiSettings;

        public event EventHandler? ToggleRequested;
        public event EventHandler? HideToTrayRequested;
        public event EventHandler? HelpRequested;
        public event EventHandler? SettingsRequested;

        public KeyboardShortcutManager(ILogger<KeyboardShortcutManager> logger, UISettings uiSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        }

        /// <summary>
        /// Handles key down events and processes shortcuts
        /// </summary>
        public void HandleKeyDown(KeyEventArgs e)
        {
            try
            {
                // Space bar to toggle
                if (e.KeyCode == Keys.Space)
                {
                    e.Handled = true;
                    _logger.LogDebug("Space key pressed - triggering toggle");
                    ToggleRequested?.Invoke(this, EventArgs.Empty);
                }
                // Escape to hide to tray
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    _logger.LogDebug("Escape key pressed - triggering hide to tray");
                    HideToTrayRequested?.Invoke(this, EventArgs.Empty);
                }
                // F1 for help/about
                else if (e.KeyCode == Keys.F1)
                {
                    e.Handled = true;
                    _logger.LogDebug("F1 key pressed - triggering help");
                    HelpRequested?.Invoke(this, EventArgs.Empty);
                }
                // F2 for settings
                else if (e.KeyCode == Keys.F2)
                {
                    e.Handled = true;
                    _logger.LogDebug("F2 key pressed - triggering settings");
                    SettingsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling keyboard shortcut");
            }
        }

        /// <summary>
        /// Shows the help dialog
        /// </summary>
        public void ShowHelp()
        {
            // Build the emergency unlock shortcut string
            var emergencyShortcut = BuildEmergencyUnlockShortcutString();
            
            string helpText = $@"SimBlock - Keyboard Blocker

Keyboard Shortcuts:
• Space - Toggle keyboard blocking
• Escape - Hide to system tray
• F1 - Show this help
• F2 - Open settings window

Emergency Unlock:
• {emergencyShortcut} (3 times) - Emergency unlock (works even when blocked)
• Must be pressed 3 times within 2 seconds

Status Bar Information:
• Current time and blocking duration
• Daily blocking session count
• Keyboard hook service status
• SimBlock application resource usage (CPU and RAM)

Tips:
• The application minimizes to system tray when closed
• Right-click the tray icon for quick access
• The tray icon shows current blocking status
• Emergency unlock requires 3 consecutive presses for safety
• Resource usage colors: Blue (normal), Orange (moderate), Red (high)
• Memory usage matches Task Manager's ""Memory"" column exactly
• Theme preference is saved and restored on startup
• Theme can be changed in the settings window (F2 or Settings button)
• Emergency unlock shortcut can be customized in settings";

            MessageBox.Show(helpText, "SimBlock Help", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string BuildEmergencyUnlockShortcutString()
        {
            var modifiers = new List<string>();
            
            if (_uiSettings.EmergencyUnlockRequiresCtrl)
                modifiers.Add("Ctrl");
            if (_uiSettings.EmergencyUnlockRequiresAlt)
                modifiers.Add("Alt");
            if (_uiSettings.EmergencyUnlockRequiresShift)
                modifiers.Add("Shift");
            
            var shortcut = string.Join("+", modifiers);
            if (shortcut.Length > 0)
                shortcut += "+";
            
            shortcut += _uiSettings.EmergencyUnlockKey.ToString();
            
            return shortcut;
        }
    }
}
