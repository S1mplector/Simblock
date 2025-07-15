using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
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
            ApplyCurrentTheme();
        }

        private void InitializeComponent()
        {
            // Configure form properties
            Text = "Settings - SimBlock";
            Size = new Size(400, 300);
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

        private void LayoutControls()
        {
            // Main layout panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
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
            _appearanceGroupBox.Size = new Size(350, 80);

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
            mainPanel.Controls.Add(buttonPanel, 0, 1);

            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            Controls.Add(mainPanel);
        }

        private void InitializeEventHandlers()
        {
            _themeToggleButton.Click += OnThemeToggleButtonClick;
            _closeButton.Click += OnCloseButtonClick;
            _themeManager.ThemeChanged += OnThemeChanged;
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