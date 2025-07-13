using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages the main UI layout and control creation
    /// </summary>
    public class UILayoutManager
    {
        private readonly UISettings _uiSettings;
        private readonly LogoManager _logoManager;
        private readonly ILogger<UILayoutManager> _logger;

        public UILayoutManager(UISettings uiSettings, LogoManager logoManager, ILogger<UILayoutManager> logger)
        {
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logoManager = logoManager ?? throw new ArgumentNullException(nameof(logoManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Contains all the UI controls for the main form
        /// </summary>
        public class UIControls
        {
            public Button ToggleButton { get; set; } = null!;
            public Label StatusLabel { get; set; } = null!;
            public PictureBox LogoIcon { get; set; } = null!;
            public Label LastToggleLabel { get; set; } = null!;
            public Button HideToTrayButton { get; set; } = null!;
            public Label InstructionsLabel { get; set; } = null!;
        }

        /// <summary>
        /// Initializes the form layout and creates all UI controls
        /// </summary>
        public UIControls InitializeLayout(Form form)
        {
            // Configure form properties
            ConfigureFormProperties(form);

            // Create UI controls
            var controls = CreateUIControls();

            // Create and configure the main layout panel
            var mainPanel = CreateMainLayoutPanel(controls);

            // Add the main panel to the form
            form.Controls.Add(mainPanel);

            return controls;
        }

        private void ConfigureFormProperties(Form form)
        {
            form.Text = "SimBlock - Keyboard Blocker";
            form.Size = _uiSettings.WindowSize;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.MaximizeBox = false;
            form.MinimizeBox = true;
            form.ShowInTaskbar = true;
            form.WindowState = FormWindowState.Normal;
            form.Visible = true;
            form.TopMost = false;
            form.Icon = _logoManager.CreateApplicationIcon();
            form.KeyPreview = true; // Enable keyboard shortcuts
        }

        private UIControls CreateUIControls()
        {
            var controls = new UIControls();

            // Status label
            controls.StatusLabel = new Label
            {
                Text = "Keyboard is unlocked",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.SuccessColor
            };

            // Logo icon
            controls.LogoIcon = _logoManager.CreateLogoPictureBox();

            // Toggle button
            controls.ToggleButton = new Button
            {
                Text = "Block Keyboard",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(200, 50),
                Anchor = AnchorStyles.None,
                BackColor = _uiSettings.PrimaryButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            controls.ToggleButton.FlatAppearance.BorderSize = 0;

            // Last toggle time label
            controls.LastToggleLabel = new Label
            {
                Text = "Last toggle: Never",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.InactiveColor
            };

            // Hide to tray button
            controls.HideToTrayButton = new Button
            {
                Text = "Hide to Tray",
                Font = new Font("Segoe UI", 10),
                Size = new Size(120, 30),
                Anchor = AnchorStyles.None,
                BackColor = _uiSettings.SecondaryButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            controls.HideToTrayButton.FlatAppearance.BorderSize = 0;

            // Instructions label
            controls.InstructionsLabel = new Label
            {
                Text = "Space: Toggle • Esc: Hide • F1: Help • Emergency: Ctrl+Alt+U (3x)",
                Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = Color.DarkGray
            };

            return controls;
        }

        private TableLayoutPanel CreateMainLayoutPanel(UIControls controls)
        {
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(_uiSettings.WindowPadding)
            };

            // Add controls to panel
            mainPanel.Controls.Add(controls.StatusLabel, 0, 0);
            mainPanel.Controls.Add(controls.LogoIcon, 0, 1);
            mainPanel.Controls.Add(controls.ToggleButton, 0, 2);
            mainPanel.Controls.Add(controls.LastToggleLabel, 0, 3);
            mainPanel.Controls.Add(controls.HideToTrayButton, 0, 4);
            mainPanel.Controls.Add(controls.InstructionsLabel, 0, 5);

            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // Status text
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15)); // Logo
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // Toggle button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10)); // Last toggle
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15)); // Hide button
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 10)); // Instructions

            return mainPanel;
        }

        /// <summary>
        /// Updates the UI controls based on the current state
        /// </summary>
        public void UpdateUI(UIControls controls, bool isKeyboardBlocked, string statusText, string toggleButtonText, DateTime lastToggleTime)
        {
            // Update status label
            controls.StatusLabel.Text = statusText;
            controls.StatusLabel.ForeColor = isKeyboardBlocked ? _uiSettings.ErrorColor : _uiSettings.SuccessColor;

            // Update toggle button
            controls.ToggleButton.Text = toggleButtonText;
            
            // Only update button color if it's enabled (not processing)
            if (controls.ToggleButton.Enabled)
            {
                controls.ToggleButton.BackColor = isKeyboardBlocked ? _uiSettings.DangerButtonColor : _uiSettings.PrimaryButtonColor;
            }

            // Update last toggle time
            controls.LastToggleLabel.Text = $"Last toggle: {lastToggleTime:HH:mm:ss}";

            // Update logo appearance
            _logoManager.UpdateLogoState(controls.LogoIcon, isKeyboardBlocked);
        }

        /// <summary>
        /// Updates the toggle button state during processing
        /// </summary>
        public void SetToggleButtonProcessing(Button toggleButton, bool isProcessing)
        {
            if (isProcessing)
            {
                toggleButton.Enabled = false;
                toggleButton.Text = "Processing...";
            }
            else
            {
                toggleButton.Enabled = true;
                // Text will be updated by UpdateUI method
            }
        }
    }
}
