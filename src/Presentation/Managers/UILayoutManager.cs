using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Controls;
using SimBlock.Presentation.Interfaces;
using SimBlock.Presentation.ViewModels;

namespace SimBlock.Presentation.Managers
{
    /// <summary>
    /// Manages the main UI layout and control creation
    /// </summary>
    public class UILayoutManager : IUILayoutManager
    {
        private readonly UISettings _uiSettings;
        private readonly ILogoManager _logoManager;
        private readonly ILogger<UILayoutManager> _logger;

        public UILayoutManager(UISettings uiSettings, ILogoManager logoManager, ILogger<UILayoutManager> logger)
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
            // Tab controls
            public TabControl MainTabControl { get; set; } = null!;
            public TabPage KeyboardTab { get; set; } = null!;
            public TabPage MouseTab { get; set; } = null!;
            
            // Keyboard tab controls
            public Button KeyboardToggleButton { get; set; } = null!;
            public Label KeyboardStatusLabel { get; set; } = null!;
            public PictureBox KeyboardLogoIcon { get; set; } = null!;
            public Label KeyboardNameLabel { get; set; } = null!;
            public Label KeyboardLastToggleLabel { get; set; } = null!;
            
            // Mouse tab controls
            public Button MouseToggleButton { get; set; } = null!;
            public Label MouseStatusLabel { get; set; } = null!;
            public PictureBox MouseLogoIcon { get; set; } = null!;
            public Label MouseNameLabel { get; set; } = null!;
            public Label MouseLastToggleLabel { get; set; } = null!;
            
            // Shared controls
            public Button HideToTrayButton { get; set; } = null!;
            public Button SettingsButton { get; set; } = null!;
            public Label InstructionsLabel { get; set; } = null!;
            public Label PrivacyNoticeLabel { get; set; } = null!;
        }

        /// <summary>
        /// Initializes the form layout and creates all UI controls
        /// </summary>
        public IUILayoutManager.UIControls InitializeLayout(Form form)
        {
            // Configure form properties
            ConfigureFormProperties(form);

            // Create UI controls
            var controls = CreateUIControls();

            // Create and configure the main layout with tabs
            var mainPanel = CreateMainLayoutWithTabs(controls);

            // Add the main panel to the form
            form.Controls.Add(mainPanel);

            return controls;
        }

        private void ConfigureFormProperties(Form form)
        {
            form.Text = "SimBlock - Keyboard & Mouse Blocker";
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
            form.BackColor = _uiSettings.BackgroundColor;
            form.ForeColor = _uiSettings.TextColor;
        }

        private IUILayoutManager.UIControls CreateUIControls()
        {
            var controls = new IUILayoutManager.UIControls();

            // Create tab control
            controls.MainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                BackColor = _uiSettings.BackgroundColor,
                ForeColor = _uiSettings.TextColor
            };

            // Create tab pages
            controls.KeyboardTab = new TabPage
            {
                Text = "Keyboard",
                BackColor = _uiSettings.BackgroundColor,
                ForeColor = _uiSettings.TextColor
            };

            controls.MouseTab = new TabPage
            {
                Text = "Mouse",
                BackColor = _uiSettings.BackgroundColor,
                ForeColor = _uiSettings.TextColor
            };

            // Create keyboard tab controls
            controls.KeyboardStatusLabel = new Label
            {
                Text = "Keyboard is unlocked",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.SuccessColor
            };

            controls.KeyboardLogoIcon = _logoManager.CreateLogoPictureBox();

            controls.KeyboardNameLabel = new Label
            {
                Text = "Loading keyboard info...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.InactiveColor
            };

            controls.KeyboardToggleButton = new RoundedButton
            {
                Text = "Block Keyboard",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(200, 50),
                Anchor = AnchorStyles.None,
                BackColor = _uiSettings.PrimaryButtonColor,
                ForeColor = Color.White,
                CornerRadius = 8
            };

            controls.KeyboardLastToggleLabel = new Label
            {
                Text = "Last toggle: Never",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.InactiveColor
            };

            // Create mouse tab controls
            controls.MouseStatusLabel = new Label
            {
                Text = "Mouse is unlocked",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.SuccessColor
            };

            controls.MouseLogoIcon = _logoManager.CreateMousePictureBox();

            controls.MouseNameLabel = new Label
            {
                Text = "Loading mouse info...",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.InactiveColor
            };

            controls.MouseToggleButton = new RoundedButton
            {
                Text = "Block Mouse",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(200, 50),
                Anchor = AnchorStyles.None,
                BackColor = _uiSettings.PrimaryButtonColor,
                ForeColor = Color.White,
                CornerRadius = 8
            };

            controls.MouseLastToggleLabel = new Label
            {
                Text = "Last toggle: Never",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.InactiveColor
            };

            // Create shared controls
            controls.SettingsButton = new RoundedButton
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Size = new Size(120, 36),
                Anchor = AnchorStyles.None,
                BackColor = _uiSettings.SecondaryButtonColor,
                ForeColor = _uiSettings.TextColor,
                CornerRadius = 6,
                Margin = new Padding(5)
            };

            controls.HideToTrayButton = new RoundedButton
            {
                Text = "Hide to Tray",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Size = new Size(120, 36),
                Anchor = AnchorStyles.None,
                BackColor = _uiSettings.SecondaryButtonColor,
                ForeColor = _uiSettings.TextColor,
                CornerRadius = 6,
                Margin = new Padding(5)
            };

            controls.MacroManagerButton = new RoundedButton
            {
                Text = "Macro Manager",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Size = new Size(140, 36),
                Anchor = AnchorStyles.None,
                BackColor = _uiSettings.SecondaryButtonColor,
                ForeColor = _uiSettings.TextColor,
                CornerRadius = 6,
                Margin = new Padding(5)
            };
            // Temporarily hide Macro Manager button from the main interface without removing code
            controls.MacroManagerButton.Visible = false;
            controls.MacroManagerButton.TabStop = false;

            controls.InstructionsLabel = new Label
            {
                Text = "Space: Toggle • Esc: Hide • F1: Help • F2: Settings • Emergency: Ctrl+Alt+U (3x)",
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.InactiveColor
            };

            controls.PrivacyNoticeLabel = new Label
            {
                Text = "SimBlock doesn't collect, intercept or transmit any personal data. You're welcome to inspect the source code.",
                Font = new Font("Segoe UI", 8),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = _uiSettings.InactiveColor
            };

            return controls;
        }

        private TableLayoutPanel CreateMainLayoutWithTabs(IUILayoutManager.UIControls controls)
        {
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(_uiSettings.WindowPadding / 2), // Reduced padding to allow more space
                BackColor = _uiSettings.BackgroundColor,
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // Create keyboard tab layout
            var keyboardTabPanel = CreateTabPanel(
                controls.KeyboardStatusLabel,
                controls.KeyboardLogoIcon,
                controls.KeyboardNameLabel,
                controls.KeyboardToggleButton,
                controls.KeyboardLastToggleLabel
            );

            // Create mouse tab layout
            var mouseTabPanel = CreateTabPanel(
                controls.MouseStatusLabel,
                controls.MouseLogoIcon,
                controls.MouseNameLabel,
                controls.MouseToggleButton,
                controls.MouseLastToggleLabel
            );

            // Add panels to tabs
            controls.KeyboardTab.Controls.Add(keyboardTabPanel);
            controls.MouseTab.Controls.Add(mouseTabPanel);

            // Add tabs to tab control
            controls.MainTabControl.TabPages.Add(controls.KeyboardTab);
            controls.MainTabControl.TabPages.Add(controls.MouseTab);

            // Create a compact, right-aligned area for Settings and Hide to Tray buttons
            // Container with spacer column (100%) and an autosized column hosting a flow panel
            var buttonPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor,
                Margin = new Padding(0, 6, 0, 6),
                Padding = new Padding(0),
                AutoSize = true,
                Height = 46
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var rightButtonsFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            // Slight spacing between buttons for better touch targets
            controls.SettingsButton.Margin = new Padding(0, 0, 8, 0);
            controls.HideToTrayButton.Margin = new Padding(0);

            rightButtonsFlow.Controls.Add(controls.SettingsButton);
            rightButtonsFlow.Controls.Add(controls.HideToTrayButton);

            // Place flow panel in the right autosized column (col 1)
            buttonPanel.Controls.Add(new Panel { Dock = DockStyle.Fill }, 0, 0); // spacer
            buttonPanel.Controls.Add(rightButtonsFlow, 1, 0);

            // Add main components to main panel
            mainPanel.Controls.Add(controls.MainTabControl, 0, 0);
            mainPanel.Controls.Add(buttonPanel, 0, 1);
            mainPanel.Controls.Add(controls.InstructionsLabel, 0, 2);

            // Set row styles - adjusted to give more space to the button panel
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75)); // Tab control
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); // Compact height for button panel
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Auto-size for instructions

            return mainPanel;
        }

        private TableLayoutPanel CreateTabPanel(Label statusLabel, PictureBox logoIcon, Label nameLabel, Button toggleButton, Label lastToggleLabel)
        {
            var tabPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(10),
                BackColor = _uiSettings.BackgroundColor
            };

            // Add controls to tab panel
            tabPanel.Controls.Add(statusLabel, 0, 0);
            tabPanel.Controls.Add(logoIcon, 0, 1);
            tabPanel.Controls.Add(nameLabel, 0, 2);
            tabPanel.Controls.Add(toggleButton, 0, 3);
            tabPanel.Controls.Add(lastToggleLabel, 0, 4);

            // Set row styles
            tabPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // Status text
            tabPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20)); // Logo
            tabPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15)); // Device name
            tabPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25)); // Toggle button
            tabPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 15)); // Last toggle

            return tabPanel;
        }

        /// <summary>
        /// Updates the UI controls based on the current state
        /// </summary>
        public void UpdateUI(IUILayoutManager.UIControls controls, MainWindowViewModel viewModel)
        {
            // Update keyboard tab
            controls.KeyboardStatusLabel.Text = viewModel.KeyboardStatusText;
            controls.KeyboardStatusLabel.ForeColor = viewModel.IsKeyboardBlocked ? _uiSettings.ErrorColor : _uiSettings.SuccessColor;

            controls.KeyboardToggleButton.Text = viewModel.KeyboardToggleButtonText;
            
            // Only update button color if it's enabled (not processing)
            if (controls.KeyboardToggleButton.Enabled)
            {
                controls.KeyboardToggleButton.BackColor = viewModel.IsKeyboardBlocked ? _uiSettings.DangerButtonColor : _uiSettings.PrimaryButtonColor;
            }

            controls.KeyboardLastToggleLabel.Text = $"Last toggle: {viewModel.KeyboardLastToggleTime:HH:mm:ss}";

            // Update logo appearance
            _logoManager.UpdateLogoState(controls.KeyboardLogoIcon, viewModel.IsKeyboardBlocked);

            // Update mouse tab
            controls.MouseStatusLabel.Text = viewModel.MouseStatusText;
            controls.MouseStatusLabel.ForeColor = viewModel.IsMouseBlocked ? _uiSettings.ErrorColor : _uiSettings.SuccessColor;

            controls.MouseToggleButton.Text = viewModel.MouseToggleButtonText;
            
            // Only update button color if it's enabled (not processing)
            if (controls.MouseToggleButton.Enabled)
            {
                controls.MouseToggleButton.BackColor = viewModel.IsMouseBlocked ? _uiSettings.DangerButtonColor : _uiSettings.PrimaryButtonColor;
            }

            controls.MouseLastToggleLabel.Text = $"Last toggle: {viewModel.MouseLastToggleTime:HH:mm:ss}";

            // Update logo appearance
            _logoManager.UpdateLogoState(controls.MouseLogoIcon, viewModel.IsMouseBlocked, isMouseIcon: true);
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

        /// <summary>
        /// Updates the settings button (placeholder for future settings updates)
        /// </summary>
        public void UpdateSettingsButton(Button settingsButton)
        {
            // Settings button text doesn't change, but this method is kept for consistency
            settingsButton.Text = "⚙️ Settings";
        }

        /// <summary>
        /// Updates the keyboard name label
        /// </summary>
        public void UpdateDeviceNameLabel(Label deviceNameLabel, string deviceName)
        {
            if (deviceNameLabel != null)
            {
                deviceNameLabel.Text = deviceName;
            }
        }
    }
}
