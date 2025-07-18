using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;
using SimBlock.Presentation.ViewModels;
using SimBlock.Infrastructure.Windows;
using SimBlock.Presentation.Configuration;
using SimBlock.Presentation.Interfaces;

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

        private async void OnKeyboardToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Keyboard toggle button clicked");
                
                // Set button to processing state
                _layoutManager.SetToggleButtonProcessing(_uiControls.KeyboardToggleButton, true);
                
                await _keyboardBlockerService.ToggleBlockingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling keyboard blocking");
                MessageBox.Show($"Failed to toggle keyboard blocking.\n\nError: {ex.Message}",
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
                _logger.LogInformation("Mouse toggle button clicked");
                
                // Set button to processing state
                _layoutManager.SetToggleButtonProcessing(_uiControls.MouseToggleButton, true);
                
                await _mouseBlockerService.ToggleBlockingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling mouse blocking");
                MessageBox.Show($"Failed to toggle mouse blocking.\n\nError: {ex.Message}",
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

            _viewModel.UpdateFromKeyboardState(state);
            UpdateUI();
            _statusBarManager.UpdateBlockingState(state.IsBlocked || _viewModel.IsMouseBlocked);
            
            // Update visualization
            _visualizationManager.UpdateKeyboardVisualization(state);
            _visualizationManager.SetKeyboardBlockingMode(_uiSettings.KeyboardBlockingMode, _uiSettings.AdvancedKeyboardConfig);
        }

        private void OnMouseStateChanged(object? sender, MouseBlockState state)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnMouseStateChanged(sender, state)));
                return;
            }

            _viewModel.UpdateFromMouseState(state);
            UpdateUI();
            _statusBarManager.UpdateBlockingState(_viewModel.IsKeyboardBlocked || state.IsBlocked);
            
            // Update visualization
            _visualizationManager.UpdateMouseVisualization(state);
            _visualizationManager.SetMouseBlockingMode(_uiSettings.MouseBlockingMode, _uiSettings.AdvancedMouseConfig);
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

                // Don't start new feedback if already showing
                if (_showingEmergencyFeedback)
                    return;

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
                
                // Reset button after 1 second
                _emergencyFeedbackTimer = new System.Windows.Forms.Timer();
                _emergencyFeedbackTimer.Interval = 1000;
                _emergencyFeedbackTimer.Tick += (s, e) =>
                {
                    ResetEmergencyFeedback();
                };
                _emergencyFeedbackTimer.Start();

                // Flash the window to draw attention
                this.Activate();
                this.BringToFront();
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
