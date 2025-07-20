using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;
using SimBlock.Core.Domain.Enums;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Application.Interfaces;

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
        private readonly ISettingsManager _settingsManager;
        private readonly IKeyboardBlockerService _keyboardBlockerService;
        private readonly IMouseBlockerService _mouseBlockerService;
        private readonly IBlockingVisualizationManager _visualizationManager;

        // UI Controls
        private Button _themeToggleButton = null!;
        
        // Visualization controls for Select mode
        private SimBlock.Presentation.Controls.KeyboardVisualizationControl? _keyboardVisualizationControl;
        private SimBlock.Presentation.Controls.MouseVisualizationControl? _mouseVisualizationControl;
        private GroupBox? _visualizationGroupBox;
        private Panel? _legendPanel;
        private Button _closeButton = null!;
        private Label _themeLabel = null!;
        private GroupBox _appearanceGroupBox = null!;
        private GroupBox _behaviorGroupBox = null!;
        private CheckBox _startWithWindowsCheckBox = null!;
        
        // Emergency unlock shortcut controls
        private GroupBox _emergencyUnlockGroupBox = null!;
        private Label _emergencyUnlockLabel = null!;
        private ComboBox _emergencyUnlockKeyComboBox = null!;
        private CheckBox _emergencyUnlockCtrlCheckBox = null!;
        private CheckBox _emergencyUnlockAltCheckBox = null!;
        private CheckBox _emergencyUnlockShiftCheckBox = null!;
        private Label _emergencyUnlockPreviewLabel = null!;
        
        // Advanced blocking controls
        private GroupBox _blockingModeGroupBox = null!;
        private RadioButton _simpleModeRadioButton = null!;
        private RadioButton _advancedModeRadioButton = null!;
        private RadioButton _selectModeRadioButton = null!;
        private GroupBox _advancedKeyboardGroupBox = null!;
        private GroupBox _advancedMouseGroupBox = null!;
        private Panel _advancedConfigPanel = null!;
        
        // Keyboard blocking controls
        private CheckBox _blockModifierKeysCheckBox = null!;
        private CheckBox _blockFunctionKeysCheckBox = null!;
        private CheckBox _blockNumberKeysCheckBox = null!;
        private CheckBox _blockLetterKeysCheckBox = null!;
        private CheckBox _blockArrowKeysCheckBox = null!;
        private CheckBox _blockSpecialKeysCheckBox = null!;
        
        // Mouse blocking controls
        private CheckBox _blockLeftButtonCheckBox = null!;
        private CheckBox _blockRightButtonCheckBox = null!;
        private CheckBox _blockMiddleButtonCheckBox = null!;
        private CheckBox _blockX1ButtonCheckBox = null!;
        private CheckBox _blockX2ButtonCheckBox = null!;
        private CheckBox _blockMouseWheelCheckBox = null!;
        private CheckBox _blockMouseMovementCheckBox = null!;
        private CheckBox _blockDoubleClickCheckBox = null!;

        public SettingsForm(
            IThemeManager themeManager,
            UISettings uiSettings,
            ILogger<SettingsForm> logger,
            ISettingsManager settingsManager,
            IKeyboardBlockerService keyboardBlockerService,
            IMouseBlockerService mouseBlockerService,
            IBlockingVisualizationManager visualizationManager)
        {
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _keyboardBlockerService = keyboardBlockerService ?? throw new ArgumentNullException(nameof(keyboardBlockerService));
            _mouseBlockerService = mouseBlockerService ?? throw new ArgumentNullException(nameof(mouseBlockerService));
            _visualizationManager = visualizationManager ?? throw new ArgumentNullException(nameof(visualizationManager));

            InitializeComponent();
            InitializeEventHandlers();
            InitializeStartupState();
            LoadSettings();
            ApplyCurrentTheme();
        }

        private void InitializeComponent()
        {
            // Configure form properties
            Text = "Settings - SimBlock";
            Size = new Size(650, 1100); // Further increased height to accommodate visualization controls
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
            
            // Emergency unlock group box
            _emergencyUnlockGroupBox = new GroupBox
            {
                Text = "Emergency Unlock Shortcut",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor
            };
            
            CreateEmergencyUnlockControls();

            // Blocking mode group box
            _blockingModeGroupBox = new GroupBox
            {
                Text = "Blocking Mode",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor
            };

            // Simple mode radio button
            _simpleModeRadioButton = new RadioButton
            {
                Text = "Simple Mode (Block all input)",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Checked = _uiSettings.KeyboardBlockingMode == BlockingMode.Simple
            };

            // Advanced mode radio button
            _advancedModeRadioButton = new RadioButton
            {
                Text = "Advanced Mode (Block selected keys/actions)",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Checked = _uiSettings.KeyboardBlockingMode == BlockingMode.Advanced
            };

            // Select mode radio button
            _selectModeRadioButton = new RadioButton
            {
                Text = "Select Mode (Visual selection then block)",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Checked = _uiSettings.KeyboardBlockingMode == BlockingMode.Select
            };

            // Advanced configuration panel
            _advancedConfigPanel = new Panel
            {
                AutoScroll = true,
                BackColor = _uiSettings.BackgroundColor,
                Visible = _uiSettings.KeyboardBlockingMode == BlockingMode.Advanced
            };

            CreateAdvancedControls();
            CreateVisualizationControls();


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

        private void CreateAdvancedControls()
        {
            // Advanced keyboard configuration group box
            _advancedKeyboardGroupBox = new GroupBox
            {
                Text = "Keyboard Blocking",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor,
                Size = new Size(280, 200),
                Location = new Point(10, 10)
            };

            // Keyboard blocking checkboxes
            _blockModifierKeysCheckBox = new CheckBox
            {
                Text = "Modifier Keys (Ctrl, Alt, Shift, Win)",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 25),
                Checked = _uiSettings.AdvancedKeyboardConfig?.BlockModifierKeys ?? false
            };

            _blockFunctionKeysCheckBox = new CheckBox
            {
                Text = "Function Keys (F1-F12)",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 50),
                Checked = _uiSettings.AdvancedKeyboardConfig?.BlockFunctionKeys ?? false
            };

            _blockNumberKeysCheckBox = new CheckBox
            {
                Text = "Number Keys (0-9)",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 75),
                Checked = _uiSettings.AdvancedKeyboardConfig?.BlockNumberKeys ?? false
            };

            _blockLetterKeysCheckBox = new CheckBox
            {
                Text = "Letter Keys (A-Z)",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 100),
                Checked = _uiSettings.AdvancedKeyboardConfig?.BlockLetterKeys ?? false
            };

            _blockArrowKeysCheckBox = new CheckBox
            {
                Text = "Arrow Keys",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 125),
                Checked = _uiSettings.AdvancedKeyboardConfig?.BlockArrowKeys ?? false
            };

            _blockSpecialKeysCheckBox = new CheckBox
            {
                Text = "Special Keys (Space, Enter, Tab, etc.)",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 150),
                Checked = _uiSettings.AdvancedKeyboardConfig?.BlockSpecialKeys ?? false
            };

            // Add keyboard controls to group box
            _advancedKeyboardGroupBox.Controls.Add(_blockModifierKeysCheckBox);
            _advancedKeyboardGroupBox.Controls.Add(_blockFunctionKeysCheckBox);
            _advancedKeyboardGroupBox.Controls.Add(_blockNumberKeysCheckBox);
            _advancedKeyboardGroupBox.Controls.Add(_blockLetterKeysCheckBox);
            _advancedKeyboardGroupBox.Controls.Add(_blockArrowKeysCheckBox);
            _advancedKeyboardGroupBox.Controls.Add(_blockSpecialKeysCheckBox);

            // Advanced mouse configuration group box
            _advancedMouseGroupBox = new GroupBox
            {
                Text = "Mouse Blocking",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor,
                Size = new Size(280, 200),
                Location = new Point(300, 10)
            };

            // Mouse blocking checkboxes
            _blockLeftButtonCheckBox = new CheckBox
            {
                Text = "Left Button",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 25),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockLeftButton ?? false
            };

            _blockRightButtonCheckBox = new CheckBox
            {
                Text = "Right Button",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 50),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockRightButton ?? false
            };

            _blockMiddleButtonCheckBox = new CheckBox
            {
                Text = "Middle Button",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 75),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockMiddleButton ?? false
            };

            _blockX1ButtonCheckBox = new CheckBox
            {
                Text = "X1 Button",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 100),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockX1Button ?? false
            };

            _blockX2ButtonCheckBox = new CheckBox
            {
                Text = "X2 Button",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(15, 125),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockX2Button ?? false
            };

            _blockMouseWheelCheckBox = new CheckBox
            {
                Text = "Mouse Wheel",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(140, 25),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockMouseWheel ?? false
            };

            _blockMouseMovementCheckBox = new CheckBox
            {
                Text = "Mouse Movement",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(140, 50),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockMouseMovement ?? false
            };

            _blockDoubleClickCheckBox = new CheckBox
            {
                Text = "Double Click",
                Font = new Font("Segoe UI", 8),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true,
                Location = new Point(140, 75),
                Checked = _uiSettings.AdvancedMouseConfig?.BlockDoubleClick ?? false
            };

            // Add mouse controls to group box
            _advancedMouseGroupBox.Controls.Add(_blockLeftButtonCheckBox);
            _advancedMouseGroupBox.Controls.Add(_blockRightButtonCheckBox);
            _advancedMouseGroupBox.Controls.Add(_blockMiddleButtonCheckBox);
            _advancedMouseGroupBox.Controls.Add(_blockX1ButtonCheckBox);
            _advancedMouseGroupBox.Controls.Add(_blockX2ButtonCheckBox);
            _advancedMouseGroupBox.Controls.Add(_blockMouseWheelCheckBox);
            _advancedMouseGroupBox.Controls.Add(_blockMouseMovementCheckBox);
            _advancedMouseGroupBox.Controls.Add(_blockDoubleClickCheckBox);

            // Add group boxes to advanced config panel
            _advancedConfigPanel.Controls.Add(_advancedKeyboardGroupBox);
            _advancedConfigPanel.Controls.Add(_advancedMouseGroupBox);
            _advancedConfigPanel.Size = new Size(590, 220);
        }
        
        private void CreateEmergencyUnlockControls()
        {
            // Emergency unlock label
            _emergencyUnlockLabel = new Label
            {
                Text = "Key:",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true
            };

            // Emergency unlock key combo box
            _emergencyUnlockKeyComboBox = new ComboBox
            {
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(80, 25),
                BackColor = _uiSettings.BackgroundColor,
                ForeColor = _uiSettings.TextColor
            };
            
            // Populate with common keys
            var commonKeys = new[] { Keys.U, Keys.E, Keys.Q, Keys.X, Keys.Z, Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12 };
            foreach (var key in commonKeys)
            {
                _emergencyUnlockKeyComboBox.Items.Add(key);
            }
            _emergencyUnlockKeyComboBox.SelectedItem = _uiSettings.EmergencyUnlockKey;

            // Modifier checkboxes
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

            // Preview label
            _emergencyUnlockPreviewLabel = new Label
            {
                Text = GetEmergencyUnlockPreviewText(),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = _uiSettings.PrimaryButtonColor,
                AutoSize = true
            };
        }
        
        private void LayoutEmergencyUnlockControls()
        {
            var emergencyUnlockPanel = new TableLayoutPanel
            {
                ColumnCount = 6,
                RowCount = 2,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor
            };

            // First row: Key selection
            emergencyUnlockPanel.Controls.Add(_emergencyUnlockLabel, 0, 0);
            emergencyUnlockPanel.Controls.Add(_emergencyUnlockKeyComboBox, 1, 0);
            emergencyUnlockPanel.Controls.Add(_emergencyUnlockCtrlCheckBox, 2, 0);
            emergencyUnlockPanel.Controls.Add(_emergencyUnlockAltCheckBox, 3, 0);
            emergencyUnlockPanel.Controls.Add(_emergencyUnlockShiftCheckBox, 4, 0);
            
            // Second row: Preview
            var previewLabel = new Label
            {
                Text = "Current shortcut:",
                Font = new Font("Segoe UI", 9),
                ForeColor = _uiSettings.TextColor,
                AutoSize = true
            };
            emergencyUnlockPanel.Controls.Add(previewLabel, 0, 1);
            emergencyUnlockPanel.Controls.Add(_emergencyUnlockPreviewLabel, 1, 1);
            emergencyUnlockPanel.SetColumnSpan(_emergencyUnlockPreviewLabel, 4);

            // Set column styles
            emergencyUnlockPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            emergencyUnlockPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            emergencyUnlockPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            emergencyUnlockPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            emergencyUnlockPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            emergencyUnlockPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Add to group box
            _emergencyUnlockGroupBox.Controls.Add(emergencyUnlockPanel);
            _emergencyUnlockGroupBox.Size = new Size(450, 80);
        }

        private void CreateVisualizationControls()
        {
            // Create visualization group box
            _visualizationGroupBox = new GroupBox
            {
                Text = "Select Mode - Click to Select Keys/Actions",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _uiSettings.TextColor,
                Size = new Size(600, 520), // Increased height to accommodate legend at bottom
                Visible = false // Initially hidden, shown only in Select mode
            };

            // Create keyboard visualization control
            _keyboardVisualizationControl = new SimBlock.Presentation.Controls.KeyboardVisualizationControl(_uiSettings)
            {
                Location = new Point(10, 25),
                Size = new Size(570, 190) // Slightly reduced to give more space
            };

            // Create mouse visualization control - with more adequate spacing from keyboard visualization
            _mouseVisualizationControl = new SimBlock.Presentation.Controls.MouseVisualizationControl(_uiSettings)
            {
                Location = new Point(10, 220), // Moved up slightly for better layout
                Size = new Size(570, 250) // Further increased height to ensure full visibility
            };

            // Create legend panel at the bottom
            _legendPanel = new Panel
            {
                Location = new Point(10, 480), // Positioned at bottom of group box
                Size = new Size(570, 25),
                BackColor = _uiSettings.BackgroundColor
            };

            // Hook up event handlers to save settings when selections change
            _keyboardVisualizationControl.KeyClicked += (sender, key) =>
            {
                _logger.LogInformation("Key selection changed in visualization: {Key}", key);
                SaveSettings(); // Save immediately when selection changes
                RefreshLegend(_legendPanel); // Refresh legend when selection changes
            };

            _mouseVisualizationControl.ComponentClicked += (sender, component) =>
            {
                _logger.LogInformation("Mouse component selection changed: {Component}", component);
                SaveSettings(); // Save immediately when selection changes
                RefreshLegend(_legendPanel); // Refresh legend when selection changes
            };

            // Create initial legend
            RefreshLegend(_legendPanel);

            // Add controls to group box
            _visualizationGroupBox.Controls.Add(_keyboardVisualizationControl);
            _visualizationGroupBox.Controls.Add(_mouseVisualizationControl);
            _visualizationGroupBox.Controls.Add(_legendPanel);
        }


        private void LayoutControls()
        {
            // Main layout panel
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
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
            
            // Emergency unlock controls layout
            LayoutEmergencyUnlockControls();

            // Blocking mode controls panel
            var blockingModePanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 3,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor
            };

            blockingModePanel.Controls.Add(_simpleModeRadioButton, 0, 0);
            blockingModePanel.Controls.Add(_advancedModeRadioButton, 0, 1);
            blockingModePanel.Controls.Add(_selectModeRadioButton, 0, 2);
            blockingModePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Add blocking mode panel to group box
            _blockingModeGroupBox.Controls.Add(blockingModePanel);
            _blockingModeGroupBox.Size = new Size(450, 105);


            // Button panel for close button
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                BackColor = _uiSettings.BackgroundColor
            };
            buttonPanel.Controls.Add(_closeButton);

            // Add to main panel
            mainPanel.RowCount = 7; // Increased to accommodate emergency unlock group
            mainPanel.Controls.Add(_appearanceGroupBox, 0, 0);
            mainPanel.Controls.Add(_behaviorGroupBox, 0, 1);
            mainPanel.Controls.Add(_emergencyUnlockGroupBox, 0, 2);
            mainPanel.Controls.Add(_blockingModeGroupBox, 0, 3);
            mainPanel.Controls.Add(_advancedConfigPanel, 0, 4);
            if (_visualizationGroupBox != null)
                mainPanel.Controls.Add(_visualizationGroupBox, 0, 5);
            mainPanel.Controls.Add(buttonPanel, 0, 6);

            // Set row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
            
            // Behavior settings event handlers
            _startWithWindowsCheckBox.CheckedChanged += OnStartWithWindowsChanged;
            
            // Emergency unlock shortcut event handlers
            _emergencyUnlockKeyComboBox.SelectedIndexChanged += OnEmergencyUnlockSettingChanged;
            _emergencyUnlockCtrlCheckBox.CheckedChanged += OnEmergencyUnlockSettingChanged;
            _emergencyUnlockAltCheckBox.CheckedChanged += OnEmergencyUnlockSettingChanged;
            _emergencyUnlockShiftCheckBox.CheckedChanged += OnEmergencyUnlockSettingChanged;
            
            // Blocking mode event handlers
            _simpleModeRadioButton.CheckedChanged += OnBlockingModeChanged;
            _advancedModeRadioButton.CheckedChanged += OnBlockingModeChanged;
            _selectModeRadioButton.CheckedChanged += OnBlockingModeChanged;
            
            // Advanced keyboard configuration event handlers
            _blockModifierKeysCheckBox.CheckedChanged += OnAdvancedKeyboardConfigChanged;
            _blockFunctionKeysCheckBox.CheckedChanged += OnAdvancedKeyboardConfigChanged;
            _blockNumberKeysCheckBox.CheckedChanged += OnAdvancedKeyboardConfigChanged;
            _blockLetterKeysCheckBox.CheckedChanged += OnAdvancedKeyboardConfigChanged;
            _blockArrowKeysCheckBox.CheckedChanged += OnAdvancedKeyboardConfigChanged;
            _blockSpecialKeysCheckBox.CheckedChanged += OnAdvancedKeyboardConfigChanged;
            
            // Advanced mouse configuration event handlers
            _blockLeftButtonCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
            _blockRightButtonCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
            _blockMiddleButtonCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
            _blockX1ButtonCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
            _blockX2ButtonCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
            _blockMouseWheelCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
            _blockMouseMovementCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
            _blockDoubleClickCheckBox.CheckedChanged += OnAdvancedMouseConfigChanged;
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


        private async void OnBlockingModeChanged(object? sender, EventArgs e)
        {
            try
            {
                var radioButton = sender as RadioButton;
                
                if (radioButton?.Checked != true)
                {
                    return;
                }

                if (radioButton == _simpleModeRadioButton)
                {
                    _uiSettings.KeyboardBlockingMode = BlockingMode.Simple;
                    _uiSettings.MouseBlockingMode = BlockingMode.Simple;
                    _advancedConfigPanel.Visible = false;
                    if (_visualizationGroupBox != null)
                        _visualizationGroupBox.Visible = false;
                    _logger.LogInformation("Blocking mode changed to Simple");
                    
                    // Refresh legend to hide "Selected" indicator
                    if (_legendPanel != null)
                    {
                        RefreshLegend(_legendPanel);
                    }
                    
                    // Apply simple mode to active blocker services
                    await _keyboardBlockerService.SetSimpleModeAsync();
                    await _mouseBlockerService.SetSimpleModeAsync();
                    
                    // Update visualization manager
                    _visualizationManager.SetKeyboardBlockingMode(BlockingMode.Simple);
                    _visualizationManager.SetMouseBlockingMode(BlockingMode.Simple);
                }
                else if (radioButton == _advancedModeRadioButton)
                {
                    _uiSettings.KeyboardBlockingMode = BlockingMode.Advanced;
                    _uiSettings.MouseBlockingMode = BlockingMode.Advanced;
                    _advancedConfigPanel.Visible = true;
                    if (_visualizationGroupBox != null)
                        _visualizationGroupBox.Visible = false;
                    _logger.LogInformation("Blocking mode changed to Advanced");
                    
                    // Refresh legend to hide "Selected" indicator
                    if (_legendPanel != null)
                    {
                        RefreshLegend(_legendPanel);
                    }
                    
                    // Apply advanced mode to active blocker services
                    if (_uiSettings.AdvancedKeyboardConfig != null)
                    {
                        await _keyboardBlockerService.SetAdvancedModeAsync(_uiSettings.AdvancedKeyboardConfig);
                    }
                    if (_uiSettings.AdvancedMouseConfig != null)
                    {
                        await _mouseBlockerService.SetAdvancedModeAsync(_uiSettings.AdvancedMouseConfig);
                    }
                    
                    // Update visualization manager
                    _visualizationManager.SetKeyboardBlockingMode(BlockingMode.Advanced, _uiSettings.AdvancedKeyboardConfig);
                    _visualizationManager.SetMouseBlockingMode(BlockingMode.Advanced, _uiSettings.AdvancedMouseConfig);
                }
                else if (radioButton == _selectModeRadioButton)
                {
                    _uiSettings.KeyboardBlockingMode = BlockingMode.Select;
                    _uiSettings.MouseBlockingMode = BlockingMode.Select;
                    _advancedConfigPanel.Visible = false;
                    _logger.LogInformation("Blocking mode changed to Select");
                    
                    // Initialize Advanced configurations if null for Select mode
                    if (_uiSettings.AdvancedKeyboardConfig == null)
                    {
                        _logger.LogInformation("Creating new AdvancedKeyboardConfiguration for Select mode");
                        _uiSettings.AdvancedKeyboardConfig = new AdvancedKeyboardConfiguration();
                    }
                    if (_uiSettings.AdvancedMouseConfig == null)
                    {
                        _logger.LogInformation("Creating new AdvancedMouseConfiguration for Select mode");
                        _uiSettings.AdvancedMouseConfig = new AdvancedMouseConfiguration();
                    }
                    
                    // Clear any existing selections AND advanced mode category flags
                    _uiSettings.AdvancedKeyboardConfig.ClearSelection();
                    // Also clear the category-based blocking flags from Advanced mode
                    _uiSettings.AdvancedKeyboardConfig.BlockModifierKeys = false;
                    _uiSettings.AdvancedKeyboardConfig.BlockFunctionKeys = false;
                    _uiSettings.AdvancedKeyboardConfig.BlockNumberKeys = false;
                    _uiSettings.AdvancedKeyboardConfig.BlockLetterKeys = false;
                    _uiSettings.AdvancedKeyboardConfig.BlockArrowKeys = false;
                    _uiSettings.AdvancedKeyboardConfig.BlockSpecialKeys = false;
                    
                    _uiSettings.AdvancedMouseConfig.ClearSelection();
                    // Also clear the mouse blocking flags from Advanced mode
                    _uiSettings.AdvancedMouseConfig.BlockLeftButton = false;
                    _uiSettings.AdvancedMouseConfig.BlockRightButton = false;
                    _uiSettings.AdvancedMouseConfig.BlockMiddleButton = false;
                    _uiSettings.AdvancedMouseConfig.BlockX1Button = false;
                    _uiSettings.AdvancedMouseConfig.BlockX2Button = false;
                    _uiSettings.AdvancedMouseConfig.BlockMouseWheel = false;
                    _uiSettings.AdvancedMouseConfig.BlockMouseMovement = false;
                    _uiSettings.AdvancedMouseConfig.BlockDoubleClick = false;
                    
                    _logger.LogInformation("Cleared existing selections and Advanced mode flags");
                    
                    // Show visualization controls for Select mode
                    if (_visualizationGroupBox != null)
                    {
                        _visualizationGroupBox.Visible = true;
                        
                        // Update visualization controls with Select mode
                        if (_keyboardVisualizationControl != null)
                        {
                            _keyboardVisualizationControl.UpdateVisualization(BlockingMode.Select, _uiSettings.AdvancedKeyboardConfig, false);
                            // Re-hook event handler after mode change
                            _keyboardVisualizationControl.KeyClicked -= OnVisualizationKeyClicked;
                            _keyboardVisualizationControl.KeyClicked += OnVisualizationKeyClicked;
                        }
                        if (_mouseVisualizationControl != null)
                        {
                            _mouseVisualizationControl.UpdateVisualization(BlockingMode.Select, _uiSettings.AdvancedMouseConfig, false);
                            // Re-hook event handler after mode change
                            _mouseVisualizationControl.ComponentClicked -= OnVisualizationComponentClicked;
                            _mouseVisualizationControl.ComponentClicked += OnVisualizationComponentClicked;
                        }
                        
                        // Refresh legend to show "Selected" indicator
                        if (_legendPanel != null)
                        {
                            RefreshLegend(_legendPanel);
                        }
                    }
                    
                    // Apply select mode to active blocker services
                    _logger.LogInformation("Applying Select mode to blocker services");
                    
                    await _keyboardBlockerService.SetSelectModeAsync(_uiSettings.AdvancedKeyboardConfig);
                    await _mouseBlockerService.SetSelectModeAsync(_uiSettings.AdvancedMouseConfig);
                    
                    // Update visualization manager to Select mode
                    _visualizationManager.SetKeyboardBlockingMode(BlockingMode.Select, _uiSettings.AdvancedKeyboardConfig);
                    _visualizationManager.SetMouseBlockingMode(BlockingMode.Select, _uiSettings.AdvancedMouseConfig);
                    
                    _logger.LogInformation("Select mode configuration complete");
                }
                SaveSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SettingsForm.OnBlockingModeChanged: ERROR - {ex.Message}");
                _logger.LogError(ex, "Error changing blocking mode");
            }
        }

        private async void OnAdvancedKeyboardConfigChanged(object? sender, EventArgs e)
        {
            try
            {
                // Initialize advanced keyboard config if null
                if (_uiSettings.AdvancedKeyboardConfig == null)
                {
                    _uiSettings.AdvancedKeyboardConfig = new AdvancedKeyboardConfiguration();
                }

                // Update configuration based on checkboxes
                _uiSettings.AdvancedKeyboardConfig.BlockModifierKeys = _blockModifierKeysCheckBox.Checked;
                _uiSettings.AdvancedKeyboardConfig.BlockFunctionKeys = _blockFunctionKeysCheckBox.Checked;
                _uiSettings.AdvancedKeyboardConfig.BlockNumberKeys = _blockNumberKeysCheckBox.Checked;
                _uiSettings.AdvancedKeyboardConfig.BlockLetterKeys = _blockLetterKeysCheckBox.Checked;
                _uiSettings.AdvancedKeyboardConfig.BlockArrowKeys = _blockArrowKeysCheckBox.Checked;
                _uiSettings.AdvancedKeyboardConfig.BlockSpecialKeys = _blockSpecialKeysCheckBox.Checked;

                _logger.LogInformation("Advanced keyboard configuration updated");
                SaveSettings();
                
                // Apply the updated configuration to the blocker service if in advanced mode
                if (_uiSettings.KeyboardBlockingMode == BlockingMode.Advanced)
                {
                    await _keyboardBlockerService.SetAdvancedModeAsync(_uiSettings.AdvancedKeyboardConfig);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating advanced keyboard configuration");
            }
        }

        private async void OnAdvancedMouseConfigChanged(object? sender, EventArgs e)
        {
            try
            {
                // Initialize advanced mouse config if null
                if (_uiSettings.AdvancedMouseConfig == null)
                {
                    _uiSettings.AdvancedMouseConfig = new AdvancedMouseConfiguration();
                }

                // Update configuration based on checkboxes
                _uiSettings.AdvancedMouseConfig.BlockLeftButton = _blockLeftButtonCheckBox.Checked;
                _uiSettings.AdvancedMouseConfig.BlockRightButton = _blockRightButtonCheckBox.Checked;
                _uiSettings.AdvancedMouseConfig.BlockMiddleButton = _blockMiddleButtonCheckBox.Checked;
                _uiSettings.AdvancedMouseConfig.BlockX1Button = _blockX1ButtonCheckBox.Checked;
                _uiSettings.AdvancedMouseConfig.BlockX2Button = _blockX2ButtonCheckBox.Checked;
                _uiSettings.AdvancedMouseConfig.BlockMouseWheel = _blockMouseWheelCheckBox.Checked;
                _uiSettings.AdvancedMouseConfig.BlockMouseMovement = _blockMouseMovementCheckBox.Checked;
                _uiSettings.AdvancedMouseConfig.BlockDoubleClick = _blockDoubleClickCheckBox.Checked;

                _logger.LogInformation("Advanced mouse configuration updated");
                SaveSettings();
                
                // Apply the updated configuration to the blocker service if in advanced mode
                if (_uiSettings.MouseBlockingMode == BlockingMode.Advanced)
                {
                    await _mouseBlockerService.SetAdvancedModeAsync(_uiSettings.AdvancedMouseConfig);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating advanced mouse configuration");
            }
        }

        private void OnEmergencyUnlockSettingChanged(object? sender, EventArgs e)
        {
            try
            {
                // Update settings from UI controls
                if (_emergencyUnlockKeyComboBox.SelectedItem != null)
                {
                    _uiSettings.EmergencyUnlockKey = (Keys)_emergencyUnlockKeyComboBox.SelectedItem;
                }
                _uiSettings.EmergencyUnlockRequiresCtrl = _emergencyUnlockCtrlCheckBox.Checked;
                _uiSettings.EmergencyUnlockRequiresAlt = _emergencyUnlockAltCheckBox.Checked;
                _uiSettings.EmergencyUnlockRequiresShift = _emergencyUnlockShiftCheckBox.Checked;
                
                // Update preview
                _emergencyUnlockPreviewLabel.Text = GetEmergencyUnlockPreviewText();
                
                _logger.LogInformation("Emergency unlock shortcut changed to: {Shortcut}", _emergencyUnlockPreviewLabel.Text);
                SaveSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing emergency unlock shortcut setting");
                MessageBox.Show($"Failed to change emergency unlock shortcut.\n\nError: {ex.Message}",
                    "SimBlock Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private string GetEmergencyUnlockPreviewText()
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
            
            return shortcut + " (3 times)";
        }

        private void OnStartWithWindowsChanged(object? sender, EventArgs e)
        {
            try
            {
                _uiSettings.StartWithWindows = _startWithWindowsCheckBox.Checked;
                _logger.LogInformation("Start with Windows setting changed to: {StartWithWindows}", _uiSettings.StartWithWindows);
                
                // Apply the startup setting
                ApplyStartupSetting(_uiSettings.StartWithWindows);
                SaveSettings();
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
                
                // Apply theme to blocking mode controls
                _blockingModeGroupBox.ForeColor = _uiSettings.TextColor;
                _simpleModeRadioButton.ForeColor = _uiSettings.TextColor;
                _advancedModeRadioButton.ForeColor = _uiSettings.TextColor;
                _selectModeRadioButton.ForeColor = _uiSettings.TextColor;
                _advancedConfigPanel.BackColor = _uiSettings.BackgroundColor;
                
                // Apply theme to advanced keyboard controls
                _advancedKeyboardGroupBox.ForeColor = _uiSettings.TextColor;
                _blockModifierKeysCheckBox.ForeColor = _uiSettings.TextColor;
                _blockFunctionKeysCheckBox.ForeColor = _uiSettings.TextColor;
                _blockNumberKeysCheckBox.ForeColor = _uiSettings.TextColor;
                _blockLetterKeysCheckBox.ForeColor = _uiSettings.TextColor;
                _blockArrowKeysCheckBox.ForeColor = _uiSettings.TextColor;
                _blockSpecialKeysCheckBox.ForeColor = _uiSettings.TextColor;
                
                // Apply theme to advanced mouse controls
                _advancedMouseGroupBox.ForeColor = _uiSettings.TextColor;
                _blockLeftButtonCheckBox.ForeColor = _uiSettings.TextColor;
                _blockRightButtonCheckBox.ForeColor = _uiSettings.TextColor;
                _blockMiddleButtonCheckBox.ForeColor = _uiSettings.TextColor;
                _blockX1ButtonCheckBox.ForeColor = _uiSettings.TextColor;
                _blockX2ButtonCheckBox.ForeColor = _uiSettings.TextColor;
                _blockMouseWheelCheckBox.ForeColor = _uiSettings.TextColor;
                _blockMouseMovementCheckBox.ForeColor = _uiSettings.TextColor;
                _blockDoubleClickCheckBox.ForeColor = _uiSettings.TextColor;
                
                
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

        private void LoadSettings()
        {
            try
            {
                // Load settings directly into the UISettings object
                _settingsManager.LoadSettings();
                
                // Update UI controls to reflect loaded settings
                UpdateUIFromSettings();
                
                _logger.LogInformation("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
            }
        }

        private void UpdateUIFromSettings()
        {
            // Update blocking mode radio buttons
            _simpleModeRadioButton.Checked = _uiSettings.KeyboardBlockingMode == BlockingMode.Simple;
            _advancedModeRadioButton.Checked = _uiSettings.KeyboardBlockingMode == BlockingMode.Advanced;
            _selectModeRadioButton.Checked = _uiSettings.KeyboardBlockingMode == BlockingMode.Select;
            _advancedConfigPanel.Visible = _uiSettings.KeyboardBlockingMode == BlockingMode.Advanced;
            
            // Update visualization controls visibility based on mode
            if (_visualizationGroupBox != null)
            {
                _visualizationGroupBox.Visible = _uiSettings.KeyboardBlockingMode == BlockingMode.Select;
                
                // If Select mode is active, update the visualization controls
                if (_uiSettings.KeyboardBlockingMode == BlockingMode.Select)
                {
                    if (_keyboardVisualizationControl != null && _uiSettings.AdvancedKeyboardConfig != null)
                    {
                        _logger.LogInformation("Loading Select mode keyboard visualization with {Count} selected keys",
                            _uiSettings.AdvancedKeyboardConfig.SelectedKeys.Count);
                        _keyboardVisualizationControl.UpdateVisualization(BlockingMode.Select, _uiSettings.AdvancedKeyboardConfig, false);
                    }
                    if (_mouseVisualizationControl != null && _uiSettings.AdvancedMouseConfig != null)
                    {
                        _logger.LogInformation("Loading Select mode mouse visualization with selections: Left={Left}, Right={Right}, Middle={Middle}",
                            _uiSettings.AdvancedMouseConfig.SelectedLeftButton,
                            _uiSettings.AdvancedMouseConfig.SelectedRightButton,
                            _uiSettings.AdvancedMouseConfig.SelectedMiddleButton);
                        _mouseVisualizationControl.UpdateVisualization(BlockingMode.Select, _uiSettings.AdvancedMouseConfig, false);
                    }
                }
            }

            // Update keyboard blocking checkboxes
            _blockModifierKeysCheckBox.Checked = _uiSettings.AdvancedKeyboardConfig?.BlockModifierKeys ?? false;
            _blockFunctionKeysCheckBox.Checked = _uiSettings.AdvancedKeyboardConfig?.BlockFunctionKeys ?? false;
            _blockNumberKeysCheckBox.Checked = _uiSettings.AdvancedKeyboardConfig?.BlockNumberKeys ?? false;
            _blockLetterKeysCheckBox.Checked = _uiSettings.AdvancedKeyboardConfig?.BlockLetterKeys ?? false;
            _blockArrowKeysCheckBox.Checked = _uiSettings.AdvancedKeyboardConfig?.BlockArrowKeys ?? false;
            _blockSpecialKeysCheckBox.Checked = _uiSettings.AdvancedKeyboardConfig?.BlockSpecialKeys ?? false;

            // Update mouse blocking checkboxes
            _blockLeftButtonCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockLeftButton ?? false;
            _blockRightButtonCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockRightButton ?? false;
            _blockMiddleButtonCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockMiddleButton ?? false;
            _blockX1ButtonCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockX1Button ?? false;
            _blockX2ButtonCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockX2Button ?? false;
            _blockMouseWheelCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockMouseWheel ?? false;
            _blockMouseMovementCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockMouseMovement ?? false;
            _blockDoubleClickCheckBox.Checked = _uiSettings.AdvancedMouseConfig?.BlockDoubleClick ?? false;


            // Update startup checkbox
            _startWithWindowsCheckBox.Checked = _uiSettings.StartWithWindows;
            
            // Update emergency unlock settings
            _emergencyUnlockKeyComboBox.SelectedItem = _uiSettings.EmergencyUnlockKey;
            _emergencyUnlockCtrlCheckBox.Checked = _uiSettings.EmergencyUnlockRequiresCtrl;
            _emergencyUnlockAltCheckBox.Checked = _uiSettings.EmergencyUnlockRequiresAlt;
            _emergencyUnlockShiftCheckBox.Checked = _uiSettings.EmergencyUnlockRequiresShift;
            _emergencyUnlockPreviewLabel.Text = GetEmergencyUnlockPreviewText();
        }

        private void SaveSettings()
        {
            try
            {
                // Log Select mode selections before saving
                if (_uiSettings.KeyboardBlockingMode == BlockingMode.Select && _uiSettings.AdvancedKeyboardConfig != null)
                {
                    _logger.LogInformation("Saving Select mode keyboard settings with {Count} selected keys",
                        _uiSettings.AdvancedKeyboardConfig.SelectedKeys.Count);
                }
                if (_uiSettings.MouseBlockingMode == BlockingMode.Select && _uiSettings.AdvancedMouseConfig != null)
                {
                    _logger.LogInformation("Saving Select mode mouse settings - Left={Left}, Right={Right}, Middle={Middle}",
                        _uiSettings.AdvancedMouseConfig.SelectedLeftButton,
                        _uiSettings.AdvancedMouseConfig.SelectedRightButton,
                        _uiSettings.AdvancedMouseConfig.SelectedMiddleButton);
                }
                
                _settingsManager.SaveSettings();
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
            }
        }

        private void OnVisualizationKeyClicked(object? sender, Keys key)
        {
            _logger.LogInformation("Key selection changed in visualization: {Key}", key);
            SaveSettings(); // Save immediately when selection changes
        }

        private void OnVisualizationComponentClicked(object? sender, string component)
        {
            _logger.LogInformation("Mouse component selection changed: {Component}", component);
            SaveSettings(); // Save immediately when selection changes
        }

        /// <summary>
        /// Refreshes the legend panel with current color indicators
        /// </summary>
        private void RefreshLegend(Panel legendPanel)
        {
            // Clear existing legend items
            legendPanel.Controls.Clear();
            
            int x = 0;
            
            // Add legend items based on current mode
            CreateLegendItem(legendPanel, ref x, _uiSettings.ErrorColor, "Blocked");
            CreateLegendItem(legendPanel, ref x, _uiSettings.SuccessColor, "Allowed");
            CreateLegendItem(legendPanel, ref x, _uiSettings.BackgroundColor, "Inactive");
            
            // Show selected color in Select mode
            if (_selectModeRadioButton?.Checked == true)
            {
                CreateLegendItem(legendPanel, ref x, _uiSettings.SelectedColor, "Selected");
            }
        }

        /// <summary>
        /// Creates a single legend item (color box + text) in the legend panel
        /// </summary>
        private void CreateLegendItem(Panel parent, ref int x, Color color, string text)
        {
            // Create color box
            var colorBox = new Panel
            {
                Location = new Point(x, 6),
                Size = new Size(12, 12),
                BackColor = color,
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(colorBox);
            
            // Create text label
            var textLabel = new Label
            {
                Location = new Point(x + 15, 4),
                Size = new Size(60, 16),
                Text = text,
                ForeColor = _uiSettings.TextColor,
                Font = new Font("Arial", 8),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(textLabel);
            
            // Update x position for next item
            x += 80;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Save settings when form closes
            SaveSettings();
            
            // Unsubscribe from events
            _themeManager.ThemeChanged -= OnThemeChanged;
            base.OnFormClosing(e);
        }
    }
}