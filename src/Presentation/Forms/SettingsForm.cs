using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;

namespace SimBlock.Presentation.Forms
{
    /// <summary>
    /// Settings popup window for configuring application settings
    /// </summary>
    public partial class SettingsForm : Form
    {
        private readonly IThemeManager _themeManager;
        private readonly UISettings _uiSettings;
        private readonly ILogger<SettingsForm> _logger;

        // UI Controls
        private Button _themeToggleButton = null!;
        private Button _closeButton = null!;
        private Label _themeLabel = null!;
        private GroupBox _appearanceGroupBox = null!;
        private GroupBox _behaviorGroupBox = null!;
        private GroupBox _keyboardShortcutsGroupBox = null!;
        private Label _emergencyUnlockLabel = null!;
        private ComboBox _emergencyUnlockKeyComboBox = null!;
        private CheckBox _emergencyUnlockCtrlCheckBox = null!;
        private CheckBox _emergencyUnlockAltCheckBox = null!;
        private CheckBox _emergencyUnlockShiftCheckBox = null!;
        private CheckBox _startWithWindowsCheckBox = null!;

        public SettingsForm(
            IThemeManager themeManager,
            UISettings uiSettings,
            ILogger<SettingsForm> logger)
        {
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();
            InitializeEventHandlers();
            InitializeStartupState();
            ApplyCurrentTheme();
        }

        private void InitializeComponent()
        {
            // Configure form properties
            Text = "Settings - SimBlock";
            Size = new Size(500, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = _uiSettings.BackgroundColor;
            ForeColor = _uiSettings.TextColor;

            // Create controls
            CreateControls();
            LayoutControls();
        }

        private void CreateControls()
        {
            // Appearance group box
            _appearanceGroupBox = new GroupBox
            {
                Text = "Appearance",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor
            };

            // Theme label
            _themeLabel = new Label
            {
                Text = "Theme:",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true
            };

            // Theme toggle button
            _themeToggleButton = new Button
            {
                Text = GetThemeButtonText(),
                Font = new Font("Segoe UI", 9),
                Size = new Size(120, 30),
                BackColor = _uiSettings.SecondaryButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _themeToggleButton.FlatAppearance.BorderSize = 0;

            // Behavior group box
            _behaviorGroupBox = new GroupBox
            {
                Text = "Behavior",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor
            };

            // Start with Windows checkbox
            _startWithWindowsCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Checked = _uiSettings.StartWithWindows
            };

            // Keyboard shortcuts group box
            _keyboardShortcutsGroupBox = new GroupBox
            {
                Text = "Keyboard Shortcuts",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor
            };

            // Emergency unlock label
            _emergencyUnlockLabel = new Label
            {
                Text = "Emergency Unlock:",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true
            };

            // Emergency unlock key combo box
            _emergencyUnlockKeyComboBox = new ComboBox
            {
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(100, 25)
            };
            PopulateKeyComboBox();

            // Emergency unlock modifier checkboxes
            _emergencyUnlockCtrlCheckBox = new CheckBox
            {
                Text = "Ctrl",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Checked = _uiSettings.EmergencyUnlockRequiresCtrl
            };

            _emergencyUnlockAltCheckBox = new CheckBox
            {
                Text = "Alt",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Checked = _uiSettings.EmergencyUnlockRequiresAlt
            };

            _emergencyUnlockShiftCheckBox = new CheckBox
            {
                Text = "Shift",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Checked = _uiSettings.EmergencyUnlockRequiresShift
            };

            // Close button
            _closeButton = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 9),
                Size = new Size(100, 30),
                BackColor = _uiSettings.PrimaryButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            _closeButton.FlatAppearance.BorderSize = 0;
        }

        private void PopulateKeyComboBox()
        {
            var commonKeys = new[]
            {
                new { Display = "A", Value = Keys.A },
                new { Display = "B", Value = Keys.B },
                new { Display = "C", Value = Keys.C },
                new { Display = "D", Value = Keys.D },
                new { Display = "E", Value = Keys.E },
                new { Display = "F", Value = Keys.F },
                new { Display = "G", Value = Keys.G },
                new { Display = "H", Value = Keys.H },
                new { Display = "I", Value = Keys.I },
                new { Display = "J", Value = Keys.J },
                new { Display = "K", Value = Keys.K },
                new { Display = "L", Value = Keys.L },
                new { Display = "M", Value = Keys.M },
                new { Display = "N", Value = Keys.N },
                new { Display = "O", Value = Keys.O },
                new { Display = "P", Value = Keys.P },
                new { Display = "Q", Value = Keys.Q },
                new { Display = "R", Value = Keys.R },
                new { Display = "S", Value = Keys.S },
                new { Display = "T", Value = Keys.T },
                new { Display = "U", Value = Keys.U },
                new { Display = "V", Value = Keys.V },
                new { Display = "W", Value = Keys.W },
                new { Display = "X", Value = Keys.X },
                new { Display = "Y", Value = Keys.Y },
                new { Display = "Z", Value = Keys.Z },
                new { Display = "F1", Value = Keys.F1 },
                new { Display = "F2", Value = Keys.F2 },
                new { Display = "F3", Value = Keys.F3 },
                new { Display = "F4", Value = Keys.F4 },
                new { Display = "F5", Value = Keys.F5 },
                new { Display = "F6", Value = Keys.F6 },
                new { Display = "F7", Value = Keys.F7 },
                new { Display = "F8", Value = Keys.F8 },
                new { Display = "F9", Value = Keys.F9 },
                new { Display = "F10", Value = Keys.F10 },
                new { Display = "F11", Value = Keys.F11 },
                new { Display = "F12", Value = Keys.F12 }
            };

            _emergencyUnlockKeyComboBox.DataSource = commonKeys;
            _emergencyUnlockKeyComboBox.DisplayMember = "Display";
            _emergencyUnlockKeyComboBox.ValueMember = "Value";
            _emergencyUnlockKeyComboBox.SelectedValue = _uiSettings.EmergencyUnlockKey;
        }

        private void LayoutControls()
        {
            // Main layout panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20),
                BackColor = _uiSettings.BackgroundColor
            };

            // Theme controls panel
            var themePanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor
            };

            themePanel.Controls.Add(_themeLabel, 0, 0);
            themePanel.Controls.Add(_themeToggleButton, 1, 0);
            themePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            themePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Add theme panel to group box
            _appearanceGroupBox.Controls.Add(themePanel);
            _appearanceGroupBox.Size = new Size(450, 80);

            // Behavior controls panel
            var behaviorPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor
            };

            behaviorPanel.Controls.Add(_startWithWindowsCheckBox, 0, 0);
            behaviorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Add behavior panel to group box
            _behaviorGroupBox.Controls.Add(behaviorPanel);
            _behaviorGroupBox.Size = new Size(450, 60);

            // Keyboard shortcuts controls panel
            var keyboardShortcutsPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor
            };

            // First row: Emergency unlock label and key combo box
            keyboardShortcutsPanel.Controls.Add(_emergencyUnlockLabel, 0, 0);
            keyboardShortcutsPanel.Controls.Add(_emergencyUnlockKeyComboBox, 1, 0);

            // Second row: Modifier checkboxes
            var modifierPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = _uiSettings.BackgroundColor
            };
            modifierPanel.Controls.Add(_emergencyUnlockCtrlCheckBox);
            modifierPanel.Controls.Add(_emergencyUnlockAltCheckBox);
            modifierPanel.Controls.Add(_emergencyUnlockShiftCheckBox);

            keyboardShortcutsPanel.Controls.Add(new Label { Text = "Modifiers:", Font = new Font("Segoe UI", 9), ForeColor = _uiSettings.TextColor, AutoSize = true }, 0, 1);
            keyboardShortcutsPanel.Controls.Add(modifierPanel, 1, 1);

            keyboardShortcutsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            keyboardShortcutsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Add keyboard shortcuts panel to group box
            _keyboardShortcutsGroupBox.Controls.Add(keyboardShortcutsPanel);
            _keyboardShortcutsGroupBox.Size = new Size(450, 120);

            // Button panel for close button
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor
            };
            buttonPanel.Controls.Add(_closeButton);

            // Add to main panel
            mainPanel.Controls.Add(_appearanceGroupBox, 0, 0);
            mainPanel.Controls.Add(_behaviorGroupBox, 0, 1);
            mainPanel.Controls.Add(_keyboardShortcutsGroupBox, 0, 2);
            mainPanel.Controls.Add(buttonPanel, 0, 3);

            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(mainPanel);
        }

        private void InitializeEventHandlers()
        {
            _themeToggleButton.Click += OnThemeToggleButtonClick;
            _closeButton.Click += OnCloseButtonClick;
            _themeManager.ThemeChanged += OnThemeChanged;
            
            // Emergency unlock shortcut event handlers
            _emergencyUnlockKeyComboBox.SelectedIndexChanged += OnEmergencyUnlockKeyChanged;
            _emergencyUnlockCtrlCheckBox.CheckedChanged += OnEmergencyUnlockModifierChanged;
            _emergencyUnlockAltCheckBox.CheckedChanged += OnEmergencyUnlockModifierChanged;
            _emergencyUnlockShiftCheckBox.CheckedChanged += OnEmergencyUnlockModifierChanged;
            
            // Behavior settings event handlers
            _startWithWindowsCheckBox.CheckedChanged += OnStartWithWindowsChanged;
        }

        private void OnThemeToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Theme toggle button clicked in settings");
                _themeManager.ToggleTheme();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling theme from settings");
                MessageBox.Show($"Failed to toggle theme.\n\nError: {ex.Message}",
                    "SimBlock Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCloseButtonClick(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnEmergencyUnlockKeyChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_emergencyUnlockKeyComboBox.SelectedValue is Keys selectedKey)
                {
                    _uiSettings.EmergencyUnlockKey = selectedKey;
                    _logger.LogInformation("Emergency unlock key changed to: {Key}", selectedKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing emergency unlock key");
            }
        }

        private void OnEmergencyUnlockModifierChanged(object? sender, EventArgs e)
        {
            try
            {
                _uiSettings.EmergencyUnlockRequiresCtrl = _emergencyUnlockCtrlCheckBox.Checked;
                _uiSettings.EmergencyUnlockRequiresAlt = _emergencyUnlockAltCheckBox.Checked;
                _uiSettings.EmergencyUnlockRequiresShift = _emergencyUnlockShiftCheckBox.Checked;
                
                _logger.LogInformation("Emergency unlock modifiers changed - Ctrl: {Ctrl}, Alt: {Alt}, Shift: {Shift}",
                    _uiSettings.EmergencyUnlockRequiresCtrl,
                    _uiSettings.EmergencyUnlockRequiresAlt,
                    _uiSettings.EmergencyUnlockRequiresShift);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing emergency unlock modifiers");
            }
        }

        private void OnStartWithWindowsChanged(object? sender, EventArgs e)
        {
            try
            {
                _uiSettings.StartWithWindows = _startWithWindowsCheckBox.Checked;
                _logger.LogInformation("Start with Windows setting changed to: {StartWithWindows}", _uiSettings.StartWithWindows);
                
                // Apply the startup setting
                ApplyStartupSetting(_uiSettings.StartWithWindows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing start with Windows setting");
                MessageBox.Show($"Failed to change startup setting.\n\nError: {ex.Message}",
                    "SimBlock Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Revert checkbox state on error
                _startWithWindowsCheckBox.Checked = _uiSettings.StartWithWindows;
            }
        }

        private void ApplyStartupSetting(bool startWithWindows)
        {
            try
            {
                const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                const string appName = "SimBlock";
                
                using var key = Registry.CurrentUser.OpenSubKey(registryKey, true);
                if (key == null)
                {
                    throw new InvalidOperationException("Unable to access Windows startup registry key");
                }

                if (startWithWindows)
                {
                    // Add to startup
                    var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    if (executablePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        // For .NET applications, we need to get the actual executable path
                        executablePath = Environment.ProcessPath ?? executablePath;
                    }
                    
                    key.SetValue(appName, $"\"{executablePath}\"");
                    _logger.LogInformation("Added SimBlock to Windows startup");
                }
                else
                {
                    // Remove from startup
                    key.DeleteValue(appName, false);
                    _logger.LogInformation("Removed SimBlock from Windows startup");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying startup setting");
                throw;
            }
        }

        private void InitializeStartupState()
        {
            try
            {
                // Check if the application is actually set to start with Windows
                bool isInStartup = IsApplicationInStartup();
                
                // Update the UISettings to match the actual registry state
                _uiSettings.StartWithWindows = isInStartup;
                
                // Update the checkbox to reflect the current state
                _startWithWindowsCheckBox.Checked = isInStartup;
                
                _logger.LogInformation("Startup state initialized: {IsInStartup}", isInStartup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing startup state");
                // Default to false if we can't determine the state
                _startWithWindowsCheckBox.Checked = false;
                _uiSettings.StartWithWindows = false;
            }
        }

        private bool IsApplicationInStartup()
        {
            try
            {
                const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                const string appName = "SimBlock";
                
                using var key = Registry.CurrentUser.OpenSubKey(registryKey, false);
                if (key == null)
                {
                    return false;
                }

                var value = key.GetValue(appName);
                return value != null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking startup state");
                return false;
            }
        }

        private void OnThemeChanged(object? sender, Theme theme)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnThemeChanged(sender, theme)));
                return;
            }

            try
            {
                _logger.LogInformation("Theme changed in settings window: {Theme}", theme);
                ApplyCurrentTheme();
                _themeToggleButton.Text = GetThemeButtonText();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling theme change in settings");
            }
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                BackColor = _uiSettings.BackgroundColor;
                ForeColor = _uiSettings.TextColor;
                
                _appearanceGroupBox.ForeColor = _uiSettings.TextColor;
                _themeLabel.ForeColor = _uiSettings.TextColor;
                
                // Apply theme to behavior controls
                _behaviorGroupBox.ForeColor = _uiSettings.TextColor;
                _startWithWindowsCheckBox.ForeColor = _uiSettings.TextColor;
                
                // Apply theme to keyboard shortcut controls
                _keyboardShortcutsGroupBox.ForeColor = _uiSettings.TextColor;
                _emergencyUnlockLabel.ForeColor = _uiSettings.TextColor;
                _emergencyUnlockKeyComboBox.BackColor = _uiSettings.BackgroundColor;
                _emergencyUnlockKeyComboBox.ForeColor = _uiSettings.TextColor;
                _emergencyUnlockCtrlCheckBox.ForeColor = _uiSettings.TextColor;
                _emergencyUnlockAltCheckBox.ForeColor = _uiSettings.TextColor;
                _emergencyUnlockShiftCheckBox.ForeColor = _uiSettings.TextColor;
                
                // Apply theme to all panels
                ApplyThemeToControls(Controls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying theme to settings form");
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
                        case TableLayoutPanel tablePanel:
                            tablePanel.BackColor = _uiSettings.BackgroundColor;
                            break;
                        case FlowLayoutPanel flowPanel:
                            flowPanel.BackColor = _uiSettings.BackgroundColor;
                            break;
                        case GroupBox groupBox:
                            groupBox.ForeColor = _uiSettings.TextColor;
                            break;
                        case Label label:
                            label.ForeColor = _uiSettings.TextColor;
                            break;
                        case CheckBox checkBox:
                            checkBox.ForeColor = _uiSettings.TextColor;
                            break;
                        case ComboBox comboBox:
                            comboBox.BackColor = _uiSettings.BackgroundColor;
                            comboBox.ForeColor = _uiSettings.TextColor;
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

        private string GetThemeButtonText()
        {
            return _uiSettings.CurrentTheme == Theme.Light ? "🌙 Dark" : "☀️ Light";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unsubscribe from events
            _themeManager.ThemeChanged -= OnThemeChanged;
            base.OnFormClosing(e);
        }
    }
}