using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;
using SimBlock.Core.Domain.Enums;
using SimBlock.Presentation.ViewModels;
using SimBlock.Infrastructure.Windows;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;
using SimBlock.Presentation.Controls;

namespace SimBlock.Presentation.Forms
{
    /// <summary>
    /// Main application window
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly IKeyboardBlockerService _keyboardBlockerService;
        private readonly IMouseBlockerService _mouseBlockerService;
        private readonly ILogger<MainForm> _logger;
        private readonly MainWindowViewModel _viewModel;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IKeyboardInfoService _keyboardInfoService;
        private readonly IMouseInfoService _mouseInfoService;
        private readonly IServiceProvider _serviceProvider;
        
        // UI Managers
        private readonly UISettings _uiSettings;
        private readonly IStatusBarManager _statusBarManager;
        private readonly ILogoManager _logoManager;
        private readonly IUILayoutManager _layoutManager;
        private readonly IKeyboardShortcutManager _shortcutManager;
        private readonly IThemeManager _themeManager;
        private readonly IBlockingVisualizationManager _visualizationManager;

        // UI Controls (managed by UILayoutManager)
        private IUILayoutManager.UIControls _uiControls = null!;

        // Timer for status updates
        private System.Windows.Forms.Timer _statusTimer = null!;
        
        // Emergency unlock feedback tracking
        private System.Windows.Forms.Timer? _emergencyFeedbackTimer = null;
        private bool _showingEmergencyFeedback = false;
        public MainForm(
            IKeyboardBlockerService keyboardBlockerService,
            IMouseBlockerService mouseBlockerService,
            ILogger<MainForm> logger,
            UISettings uiSettings,
            IStatusBarManager statusBarManager,
            ILogoManager logoManager,
            IUILayoutManager layoutManager,
            IKeyboardShortcutManager shortcutManager,
            IResourceMonitor resourceMonitor,
            IThemeManager themeManager,
            IKeyboardInfoService keyboardInfoService,
            IMouseInfoService mouseInfoService,
            IServiceProvider serviceProvider,
            IBlockingVisualizationManager visualizationManager)
        {
            _keyboardBlockerService = keyboardBlockerService ?? throw new ArgumentNullException(nameof(keyboardBlockerService));
            _mouseBlockerService = mouseBlockerService ?? throw new ArgumentNullException(nameof(mouseBlockerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _statusBarManager = statusBarManager ?? throw new ArgumentNullException(nameof(statusBarManager));
            _logoManager = logoManager ?? throw new ArgumentNullException(nameof(logoManager));
            _layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
            _shortcutManager = shortcutManager ?? throw new ArgumentNullException(nameof(shortcutManager));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _keyboardInfoService = keyboardInfoService ?? throw new ArgumentNullException(nameof(keyboardInfoService));
            _mouseInfoService = mouseInfoService ?? throw new ArgumentNullException(nameof(mouseInfoService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _visualizationManager = visualizationManager ?? throw new ArgumentNullException(nameof(visualizationManager));
            
            _viewModel = new MainWindowViewModel();

            InitializeComponent();
            InitializeEventHandlers();
            InitializeTimer();
            UpdateUI();
            
            // Load keyboard and mouse names initially
            _ = LoadKeyboardNameAsync();
            _ = LoadMouseNameAsync();
            
            // Ensure window is visible and focused on startup
            this.Load += (s, e) =>
            {
                this.BringToFront();
                this.Activate();
                this.Focus();
            };
        }

        private void InitializeComponent()
        {
            // Initialize layout using the layout manager
            _uiControls = _layoutManager.InitializeLayout(this);
            
            // Add visualization panel after layout is initialized
            var visualizationPanel = _visualizationManager.CreateVisualizationPanel();
            visualizationPanel.Dock = DockStyle.Top;
            this.Controls.Add(visualizationPanel);
            
            // Initialize status bar
            _statusBarManager.Initialize(this);
            
            // Register components with theme manager
            _themeManager.RegisterComponents(this, _layoutManager, _statusBarManager);
        }

        private void InitializeEventHandlers()
        {
            _uiControls.KeyboardToggleButton.Click += OnKeyboardToggleButtonClick;
            _uiControls.MouseToggleButton.Click += OnMouseToggleButtonClick;
            _uiControls.HideToTrayButton.Click += OnHideToTrayButtonClick;
            _uiControls.SettingsButton.Click += OnSettingsButtonClick;
            _keyboardBlockerService.StateChanged += OnKeyboardStateChanged;
            _keyboardBlockerService.EmergencyUnlockAttempt += OnEmergencyUnlockAttempt;
            _keyboardBlockerService.ShowWindowRequested += OnShowWindowRequested;
            _mouseBlockerService.StateChanged += OnMouseStateChanged;
            _mouseBlockerService.EmergencyUnlockAttempt += OnEmergencyUnlockAttempt;
            _mouseBlockerService.ShowWindowRequested += OnShowWindowRequested;

            // Handle form closing
            FormClosing += OnFormClosing;
            
            // Handle keyboard shortcuts
            KeyDown += OnKeyDown;
            
            // Wire up shortcut manager events
            _shortcutManager.ToggleRequested += (s, e) => OnKeyboardToggleButtonClick(s, e);
            _shortcutManager.HideToTrayRequested += (s, e) => OnHideToTrayButtonClick(s, e);
            _shortcutManager.HelpRequested += (s, e) => _shortcutManager.ShowHelp();
            _shortcutManager.SettingsRequested += (s, e) => OnSettingsButtonClick(s, e);
            
            // Wire up theme manager events
            _themeManager.ThemeChanged += OnThemeChanged;
            
            // Wire up visualization control events for Select mode
            WireUpVisualizationEvents();
        }

        private void InitializeTimer()
        {
            // Initialize timer for real-time updates
            _statusTimer = new System.Windows.Forms.Timer
            {
                Interval = _uiSettings.StatusUpdateInterval,
                Enabled = true
            };
            _statusTimer.Tick += OnStatusTimerTick;
        }

        private void WireUpVisualizationEvents()
        {
            try
            {
                // Get the visualization controls from the manager
                var keyboardControl = _visualizationManager.GetKeyboardVisualizationControl() as KeyboardVisualizationControl;
                var mouseControl = _visualizationManager.GetMouseVisualizationControl() as MouseVisualizationControl;

                // Wire up keyboard visualization click events
                if (keyboardControl != null)
                {
                    keyboardControl.KeyClicked += OnKeyboardVisualizationKeyClicked;
                }

                // Wire up mouse visualization click events
                if (mouseControl != null)
                {
                    mouseControl.ComponentClicked += OnMouseVisualizationComponentClicked;
                }

                _logger.LogInformation("Visualization control events wired up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error wiring up visualization events");
            }
        }

        private void OnKeyboardVisualizationKeyClicked(object? sender, Keys e)
        {
            try
            {
                _logger.LogInformation("Key clicked: {Key}, Current mode: {Mode}", e, _uiSettings.KeyboardBlockingMode);

                // Only handle clicks in Select mode
                if (_uiSettings.KeyboardBlockingMode != BlockingMode.Select)
                {
                    _logger.LogInformation("Ignoring key click - not in Select mode");
                    return;
                }

                // Initialize Advanced configuration if null
                if (_uiSettings.AdvancedKeyboardConfig == null)
                {
                    _logger.LogInformation("Initializing AdvancedKeyboardConfig for Select mode");
                    _uiSettings.AdvancedKeyboardConfig = new AdvancedKeyboardConfiguration();
                }

                _logger.LogInformation("Key clicked in Select mode: {Key}", e);

                // Toggle the selection state for the clicked key
                _uiSettings.AdvancedKeyboardConfig.ToggleKeySelection(e);

                _logger.LogInformation("Key selection toggled. Is key selected: {IsSelected}",
                    _uiSettings.AdvancedKeyboardConfig.IsKeySelected(e));

                // Update the visualization to show the new selection state
                _visualizationManager.UpdateKeyboardVisualization(_keyboardBlockerService.CurrentState);
                _visualizationManager.SetKeyboardBlockingMode(_uiSettings.KeyboardBlockingMode, _uiSettings.AdvancedKeyboardConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling keyboard visualization key click");
            }
        }

        private void OnMouseVisualizationComponentClicked(object? sender, string e)
        {
            try
            {
                _logger.LogInformation("Mouse component clicked: {Component}, Current mode: {Mode}", e, _uiSettings.MouseBlockingMode);

                // Only handle clicks in Select mode
                if (_uiSettings.MouseBlockingMode != BlockingMode.Select)
                {
                    _logger.LogInformation("Ignoring mouse click - not in Select mode");
                    return;
                }

                // Initialize Advanced configuration if null
                if (_uiSettings.AdvancedMouseConfig == null)
                {
                    _logger.LogInformation("Initializing AdvancedMouseConfig for Select mode");
                    _uiSettings.AdvancedMouseConfig = new AdvancedMouseConfiguration();
                }

                _logger.LogInformation("Mouse component clicked in Select mode: {Component}", e);

                // Toggle the selection state for the clicked component
                _uiSettings.AdvancedMouseConfig.ToggleComponentSelection(e);

                _logger.LogInformation("Mouse component selection toggled. Is component selected: {IsSelected}",
                    _uiSettings.AdvancedMouseConfig.IsComponentSelected(e));

                // Update the visualization to show the new selection state
                _visualizationManager.UpdateMouseVisualization(_mouseBlockerService.CurrentState);
                _visualizationManager.SetMouseBlockingMode(_uiSettings.MouseBlockingMode, _uiSettings.AdvancedMouseConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling mouse visualization component click");
            }
        }

        private async void OnKeyboardToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Keyboard toggle button clicked - Mode: {Mode}", _uiSettings.KeyboardBlockingMode);
                
                // Set button to processing state
                _layoutManager.SetToggleButtonProcessing(_uiControls.KeyboardToggleButton, true);
                
                // Check if we're in Select mode
                if (_uiSettings.KeyboardBlockingMode == BlockingMode.Select)
                {
                    _logger.LogInformation("=== APPLYING KEYBOARD SELECTION IN SELECT MODE ===");
                    
                    // Get the configuration from UISettings and apply selection
                    if (_uiSettings.AdvancedKeyboardConfig != null)
                    {
                        _logger.LogInformation("Found keyboard configuration with {SelectedCount} selected keys",
                            _uiSettings.AdvancedKeyboardConfig.SelectedKeys.Count);
                        
                        // Log the selected keys before applying
                        foreach (var key in _uiSettings.AdvancedKeyboardConfig.SelectedKeys)
                        {
                            _logger.LogInformation("Selected key: {Key}", key);
                        }
                        
                        // Apply selection to convert selected keys to blocked keys
                        _uiSettings.AdvancedKeyboardConfig.ApplySelection();
                        
                        // Log the blocked keys after applying
                        _logger.LogInformation("After ApplySelection: {BlockedCount} blocked keys",
                            _uiSettings.AdvancedKeyboardConfig.BlockedKeys.Count);
                        foreach (var key in _uiSettings.AdvancedKeyboardConfig.BlockedKeys)
                        {
                            _logger.LogInformation("Blocked key: {Key}", key);
                        }
                        
                        // Switch to advanced mode with the updated configuration
                        _logger.LogInformation("Calling SetAdvancedModeAsync with configuration");
                        await _keyboardBlockerService.SetAdvancedModeAsync(_uiSettings.AdvancedKeyboardConfig);
                        _logger.LogInformation("SetAdvancedModeAsync completed");
                        
                        // Now toggle blocking to actually enable it
                        _logger.LogInformation("Calling ToggleBlockingAsync to enable blocking");
                        await _keyboardBlockerService.ToggleBlockingAsync();
                        _logger.LogInformation("ToggleBlockingAsync completed");
                    }
                    else
                    {
                        _logger.LogWarning("No keyboard configuration found for Select mode");
                        // Initialize it
                        _uiSettings.AdvancedKeyboardConfig = new AdvancedKeyboardConfiguration();
                        _logger.LogInformation("Initialized new AdvancedKeyboardConfig");
                    }
                }
                else
                {
                    // Normal toggle behavior
                    _logger.LogInformation("Not in Select mode, doing normal toggle. Mode: {Mode}", _uiSettings.KeyboardBlockingMode);
                    await _keyboardBlockerService.ToggleBlockingAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling keyboard button click");
                MessageBox.Show($"Failed to handle keyboard action.\n\nError: {ex.Message}",
                    "SimBlock Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable button and update UI
                _layoutManager.SetToggleButtonProcessing(_uiControls.KeyboardToggleButton, false);
                UpdateUI();
            }
        }

        private async void OnMouseToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Mouse toggle button clicked - Mode: {Mode}", _uiSettings.MouseBlockingMode);
                
                // Set button to processing state
                _layoutManager.SetToggleButtonProcessing(_uiControls.MouseToggleButton, true);
                
                // Check if we're in Select mode
                if (_uiSettings.MouseBlockingMode == BlockingMode.Select)
                {
                    _logger.LogInformation("=== APPLYING MOUSE SELECTION IN SELECT MODE ===");
                    
                    // Get the configuration from UISettings and apply selection
                    if (_uiSettings.AdvancedMouseConfig != null)
                    {
                        _logger.LogInformation("Found mouse configuration - HasSelectedComponents: {HasSelected}",
                            _uiSettings.AdvancedMouseConfig.HasSelectedComponents());
                        
                        // Log the selected components before applying
                        _logger.LogInformation("Selected components: LeftButton={Left}, RightButton={Right}, MiddleButton={Middle}, X1={X1}, X2={X2}, Wheel={Wheel}, Movement={Movement}, DoubleClick={Double}",
                            _uiSettings.AdvancedMouseConfig.SelectedLeftButton,
                            _uiSettings.AdvancedMouseConfig.SelectedRightButton,
                            _uiSettings.AdvancedMouseConfig.SelectedMiddleButton,
                            _uiSettings.AdvancedMouseConfig.SelectedX1Button,
                            _uiSettings.AdvancedMouseConfig.SelectedX2Button,
                            _uiSettings.AdvancedMouseConfig.SelectedMouseWheel,
                            _uiSettings.AdvancedMouseConfig.SelectedMouseMovement,
                            _uiSettings.AdvancedMouseConfig.SelectedDoubleClick);
                        
                        // Apply selection to convert selected components to blocked components
                        _uiSettings.AdvancedMouseConfig.ApplySelection();
                        
                        // Log the blocked components after applying
                        _logger.LogInformation("After ApplySelection - Blocked components: LeftButton={Left}, RightButton={Right}, MiddleButton={Middle}, X1={X1}, X2={X2}, Wheel={Wheel}, Movement={Movement}, DoubleClick={Double}",
                            _uiSettings.AdvancedMouseConfig.BlockLeftButton,
                            _uiSettings.AdvancedMouseConfig.BlockRightButton,
                            _uiSettings.AdvancedMouseConfig.BlockMiddleButton,
                            _uiSettings.AdvancedMouseConfig.BlockX1Button,
                            _uiSettings.AdvancedMouseConfig.BlockX2Button,
                            _uiSettings.AdvancedMouseConfig.BlockMouseWheel,
                            _uiSettings.AdvancedMouseConfig.BlockMouseMovement,
                            _uiSettings.AdvancedMouseConfig.BlockDoubleClick);
                        
                        // Switch to advanced mode with the updated configuration
                        _logger.LogInformation("Calling SetAdvancedModeAsync with configuration");
                        await _mouseBlockerService.SetAdvancedModeAsync(_uiSettings.AdvancedMouseConfig);
                        _logger.LogInformation("SetAdvancedModeAsync completed");
                        
                        // Now toggle blocking to actually enable it
                        _logger.LogInformation("Calling ToggleBlockingAsync to enable blocking");
                        await _mouseBlockerService.ToggleBlockingAsync();
                        _logger.LogInformation("ToggleBlockingAsync completed");
                    }
                    else
                    {
                        _logger.LogWarning("No mouse configuration found for Select mode");
                        // Initialize it
                        _uiSettings.AdvancedMouseConfig = new AdvancedMouseConfiguration();
                        _logger.LogInformation("Initialized new AdvancedMouseConfig");
                    }
                }
                else
                {
                    // Normal toggle behavior
                    await _mouseBlockerService.ToggleBlockingAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling mouse button click");
                MessageBox.Show($"Failed to handle mouse action.\n\nError: {ex.Message}",
                    "SimBlock Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable button and update UI
                _layoutManager.SetToggleButtonProcessing(_uiControls.MouseToggleButton, false);
                UpdateUI();
            }
        }

        private async void OnHideToTrayButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Hide to tray button clicked");
                await _keyboardBlockerService.HideToTrayAsync();
                Hide();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hiding to tray");
            }
        }

        private void OnSettingsButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Settings button clicked");
                
                // Create and show the settings form using dependency injection
                using var settingsForm = _serviceProvider.GetRequiredService<SettingsForm>();
                settingsForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening settings window");
                MessageBox.Show($"Failed to open settings.\n\nError: {ex.Message}",
                    "SimBlock Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                _logger.LogInformation("Theme changed to {Theme}", theme);
                
                // Update settings button (no changes needed since settings button doesn't change with theme)
                _layoutManager.UpdateSettingsButton(_uiControls.SettingsButton);
                
                // Update UI with new theme
                UpdateUI();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling theme change");
            }
        }

        private void OnKeyboardStateChanged(object? sender, KeyboardBlockState state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnKeyboardStateChanged(sender, state)));
                return;
            }

            _logger.LogInformation("Keyboard state changed - Mode: {Mode}, IsBlocked: {IsBlocked}", state.Mode, state.IsBlocked);
            
            _viewModel.UpdateFromKeyboardState(state);
            UpdateUI();
            _statusBarManager.UpdateBlockingState(state.IsBlocked || _viewModel.IsMouseBlocked);
            
            // Update visualization
            _visualizationManager.UpdateKeyboardVisualization(state);
            _visualizationManager.SetKeyboardBlockingMode(state.Mode, state.AdvancedConfig);
            
            _logger.LogInformation("Keyboard button text after update: {ButtonText}", _uiControls.KeyboardToggleButton.Text);
        }

        private void OnMouseStateChanged(object? sender, MouseBlockState state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnMouseStateChanged(sender, state)));
                return;
            }

            _logger.LogInformation("Mouse state changed - Mode: {Mode}, IsBlocked: {IsBlocked}", state.Mode, state.IsBlocked);
            
            _viewModel.UpdateFromMouseState(state);
            UpdateUI();
            _statusBarManager.UpdateBlockingState(_viewModel.IsKeyboardBlocked || state.IsBlocked);
            
            // Update visualization
            _visualizationManager.UpdateMouseVisualization(state);
            _visualizationManager.SetMouseBlockingMode(state.Mode, state.AdvancedConfig);
            
            _logger.LogInformation("Mouse button text after update: {ButtonText}", _uiControls.MouseToggleButton.Text);
        }

        private void OnStatusTimerTick(object? sender, EventArgs e)
        {
            // Update current time
            _statusBarManager.UpdateTime();

            // Update blocking duration
            _statusBarManager.UpdateBlockingDuration();

            // Update hook status
            UpdateHookStatus();
            
            // Update resource usage
            UpdateResourceUsage();
            
            // Update keyboard and mouse names periodically (every 30 seconds)
            if (DateTime.Now.Second % 30 == 0)
            {
                _ = LoadKeyboardNameAsync();
                _ = LoadMouseNameAsync();
            }
        }

        private void UpdateHookStatus()
        {
            try
            {
                // Check if the keyboard or mouse services are working properly
                var keyboardState = _keyboardBlockerService.CurrentState;
                var mouseState = _mouseBlockerService.CurrentState;
                bool isActive = keyboardState != null || mouseState != null;
                _statusBarManager.UpdateHookStatus(isActive);
            }
            catch (Exception ex)
            {
                _statusBarManager.UpdateHookStatus(false);
                _logger.LogWarning(ex, "Error checking hook status");
            }
        }

        private void UpdateResourceUsage()
        {
            try
            {
                var resourceString = _resourceMonitor.GetCompactResourceString();
                var cpuUsage = _resourceMonitor.GetCpuUsage();
                var appMemoryUsage = _resourceMonitor.GetTaskManagerMemoryUsage();
                
                _statusBarManager.UpdateResourceUsage(resourceString, cpuUsage, appMemoryUsage);
            }
            catch (Exception ex)
            {
                _statusBarManager.UpdateResourceUsage("Resource info unavailable", 0, 0);
                _logger.LogWarning(ex, "Error updating resource usage");
            }
        }

        private async Task LoadKeyboardNameAsync()
        {
            try
            {
                var keyboardName = await _keyboardInfoService.GetCurrentKeyboardNameAsync();
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() => _layoutManager.UpdateDeviceNameLabel(_uiControls.KeyboardNameLabel, keyboardName)));
                }
                else
                {
                    _layoutManager.UpdateDeviceNameLabel(_uiControls.KeyboardNameLabel, keyboardName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading keyboard name");
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() => _layoutManager.UpdateDeviceNameLabel(_uiControls.KeyboardNameLabel, "Keyboard info unavailable")));
                }
                else
                {
                    _layoutManager.UpdateDeviceNameLabel(_uiControls.KeyboardNameLabel, "Keyboard info unavailable");
                }
            }
        }

        private async Task LoadMouseNameAsync()
        {
            try
            {
                var mouseName = await _mouseInfoService.GetCurrentMouseNameAsync();
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() => _layoutManager.UpdateDeviceNameLabel(_uiControls.MouseNameLabel, mouseName)));
                }
                else
                {
                    _layoutManager.UpdateDeviceNameLabel(_uiControls.MouseNameLabel, mouseName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading mouse name");
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() => _layoutManager.UpdateDeviceNameLabel(_uiControls.MouseNameLabel, "Mouse info unavailable")));
                }
                else
                {
                    _layoutManager.UpdateDeviceNameLabel(_uiControls.MouseNameLabel, "Mouse info unavailable");
                }
            }
        }

        private void OnEmergencyUnlockAttempt(object? sender, int attemptCount)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnEmergencyUnlockAttempt(sender, attemptCount)));
                return;
            }

            try
            {
                // Show temporary visual feedback in the main window
                ShowEmergencyUnlockFeedback(attemptCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling emergency unlock attempt UI update");
            }
        }

        private void OnShowWindowRequested(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnShowWindowRequested(sender, e)));
                return;
            }

            try
            {
                _logger.LogInformation("Show window requested from tray");
                
                // Show the window if it's hidden
                if (!Visible)
                {
                    Show();
                }
                
                // Restore the window if it's minimized
                if (WindowState == FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Normal;
                }
                
                // Bring the window to the front and activate it
                BringToFront();
                Activate();
                Focus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing window from tray");
            }
        }

        private void ShowEmergencyUnlockFeedback(int attemptCount)
        {
            try
            {
                // Only show emergency feedback when keyboard or mouse is blocked
                if (!_viewModel.IsKeyboardBlocked && !_viewModel.IsMouseBlocked)
                    return;

                // Allow updating feedback even if already showing to reflect current attempt count
                // Stop any existing emergency feedback timer
                if (_emergencyFeedbackTimer != null)
                {
                    _emergencyFeedbackTimer.Stop();
                    _emergencyFeedbackTimer.Dispose();
                    _emergencyFeedbackTimer = null;
                }

                // Mark that we're showing emergency feedback
                _showingEmergencyFeedback = true;
                
                // Temporarily change the appropriate toggle button text to show emergency feedback
                if (_viewModel.IsKeyboardBlocked)
                {
                    _uiControls.KeyboardToggleButton.Text = $"Emergency: {attemptCount}/3";
                    _uiControls.KeyboardToggleButton.BackColor = _uiSettings.ErrorColor;
                }
                if (_viewModel.IsMouseBlocked)
                {
                    _uiControls.MouseToggleButton.Text = $"Emergency: {attemptCount}/3";
                    _uiControls.MouseToggleButton.BackColor = _uiSettings.ErrorColor;
                }
                
                // Reset button after 1.5 seconds (longer to ensure visibility)
                _emergencyFeedbackTimer = new System.Windows.Forms.Timer();
                _emergencyFeedbackTimer.Interval = 1500;
                _emergencyFeedbackTimer.Tick += (s, e) =>
                {
                    _emergencyFeedbackTimer?.Stop();
                    ResetEmergencyFeedback();
                };
                _emergencyFeedbackTimer.Start();

                // Flash the window to draw attention
                this.Activate();
                this.BringToFront();
                
                _logger.LogInformation("Emergency unlock feedback displayed: attempt {AttemptCount}/3", attemptCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing emergency unlock feedback");
                ResetEmergencyFeedback();
            }
        }

        private void ResetEmergencyFeedback()
        {
            try
            {
                if (_emergencyFeedbackTimer != null)
                {
                    _emergencyFeedbackTimer.Stop();
                    _emergencyFeedbackTimer.Dispose();
                    _emergencyFeedbackTimer = null;
                }

                _showingEmergencyFeedback = false;
                
                // Restore normal button appearance
                UpdateUI();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting emergency unlock feedback");
            }
        }

        private void UpdateUI()
        {
            _layoutManager.UpdateUI(_uiControls, _viewModel);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            _shortcutManager.HandleKeyDown(e);
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Don't actually close, just hide to tray
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                // Actually closing - dispose resources
                _statusTimer?.Stop();
                _statusTimer?.Dispose();
                _emergencyFeedbackTimer?.Stop();
                _emergencyFeedbackTimer?.Dispose();
                _uiControls?.KeyboardLogoIcon?.Image?.Dispose();
                _uiControls?.KeyboardLogoIcon?.Dispose();
                _uiControls?.MouseLogoIcon?.Image?.Dispose();
                _uiControls?.MouseLogoIcon?.Dispose();
                _logoManager?.Dispose();
                _resourceMonitor?.Dispose();
            }
        }
    }
}
