using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages application themes and theme persistence
    /// </summary>
    public class ThemeManager : IThemeManager
    {
        private readonly UISettings _uiSettings;
        private readonly ILogger<ThemeManager> _logger;
        private readonly string _themeSettingsPath;

        public event EventHandler<Theme>? ThemeChanged;

        public Theme CurrentTheme => _uiSettings.CurrentTheme;

        // References to UI components that need theme updates
        private Form? _mainForm;
        private IUILayoutManager? _layoutManager;
        private IStatusBarManager? _statusBarManager;

        public ThemeManager(UISettings uiSettings, ILogger<ThemeManager> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Store theme settings in the application data folder
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimBlock");
            Directory.CreateDirectory(appDataPath);
            _themeSettingsPath = Path.Combine(appDataPath, "theme.json");

            LoadThemeSettings();
        }

        /// <summary>
        /// Registers UI components that need theme updates
        /// </summary>
        public void RegisterComponents(Form mainForm, IUILayoutManager layoutManager, IStatusBarManager statusBarManager)
        {
            _mainForm = mainForm;
            _layoutManager = layoutManager;
            _statusBarManager = statusBarManager;
        }

        /// <summary>
        /// Sets the theme and applies it to UI settings
        /// </summary>
        public void SetTheme(Theme theme)
        {
            if (_uiSettings.CurrentTheme == theme)
                return;

            _logger.LogInformation("Changing theme from {CurrentTheme} to {NewTheme}", _uiSettings.CurrentTheme, theme);

            _uiSettings.ApplyTheme(theme);
            SaveThemeSettings();
            
            ApplyThemeToAllComponents();
            ThemeChanged?.Invoke(this, theme);
        }

        /// <summary>
        /// Toggles between light and dark themes
        /// </summary>
        public void ToggleTheme()
        {
            var newTheme = _uiSettings.CurrentTheme == Theme.Light ? Theme.Dark : Theme.Light;
            SetTheme(newTheme);
        }

        /// <summary>
        /// Applies the current theme to all UI components
        /// </summary>
        public void ApplyThemeToAllComponents()
        {
            if (_mainForm == null)
                return;

            try
            {
                _mainForm.Invoke(new Action(() =>
                {
                    // Apply theme to main form
                    _mainForm.BackColor = _uiSettings.BackgroundColor;
                    _mainForm.ForeColor = _uiSettings.TextColor;

                    // Apply theme to all controls recursively
                    ApplyThemeToControls(_mainForm.Controls);
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying theme to components");
            }
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                try
                {
                    switch (control)
                    {
                        case Label label:
                            // Don't override specific colored labels (like status labels)
                            if (label.ForeColor == Color.Black || label.ForeColor == Color.White ||
                                label.ForeColor == Color.DarkGray || label.ForeColor == Color.Gray)
                            {
                                label.ForeColor = _uiSettings.TextColor;
                            }
                            break;

                        case TableLayoutPanel tablePanel:
                            tablePanel.BackColor = _uiSettings.BackgroundColor;
                            break;

                        case Panel panel:
                            panel.BackColor = _uiSettings.BackgroundColor;
                            break;

                        case StatusStrip statusStrip:
                            statusStrip.BackColor = _uiSettings.StatusBarBackColor;
                            // Handle ToolStripStatusLabel items within the StatusStrip
                            foreach (ToolStripItem item in statusStrip.Items)
                            {
                                if (item is ToolStripStatusLabel statusLabel)
                                {
                                    // Only update neutral colored status labels
                                    if (statusLabel.ForeColor == Color.Black || statusLabel.ForeColor == Color.White)
                                    {
                                        statusLabel.ForeColor = _uiSettings.TextColor;
                                    }
                                }
                            }
                            break;
                    }

                    // Apply theme to child controls
                    if (control.HasChildren)
                    {
                        ApplyThemeToControls(control.Controls);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error applying theme to control {ControlType}", control.GetType().Name);
                }
            }
        }

        private void LoadThemeSettings()
        {
            try
            {
                if (File.Exists(_themeSettingsPath))
                {
                    var json = File.ReadAllText(_themeSettingsPath);
                    var themeData = JsonSerializer.Deserialize<ThemeData>(json);
                    
                    if (themeData != null && Enum.IsDefined(typeof(Theme), themeData.Theme))
                    {
                        _uiSettings.ApplyTheme(themeData.Theme);
                        _logger.LogInformation("Loaded theme settings: {Theme}", themeData.Theme);
                    }
                }
                else
                {
                    // Default to light theme
                    _uiSettings.ApplyTheme(Theme.Light);
                    _logger.LogInformation("No theme settings found, using default light theme");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading theme settings, using default light theme");
                _uiSettings.ApplyTheme(Theme.Light);
            }
        }

        private void SaveThemeSettings()
        {
            try
            {
                var themeData = new ThemeData { Theme = _uiSettings.CurrentTheme };
                var json = JsonSerializer.Serialize(themeData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_themeSettingsPath, json);
                _logger.LogDebug("Theme settings saved: {Theme}", _uiSettings.CurrentTheme);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error saving theme settings");
            }
        }

        /// <summary>
        /// Data class for theme persistence
        /// </summary>
        private class ThemeData
        {
            public Theme Theme { get; set; }
        }
    }
}