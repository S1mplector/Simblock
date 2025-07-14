using System.Windows.Forms;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MainForm> _logger;
        private readonly MainWindowViewModel _viewModel;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IKeyboardInfoService _keyboardInfoService;
        
        // UI Managers
        private readonly UISettings _uiSettings;
        private readonly IStatusBarManager _statusBarManager;
        private readonly ILogoManager _logoManager;
        private readonly IUILayoutManager _layoutManager;
        private readonly IKeyboardShortcutManager _shortcutManager;
        private readonly IThemeManager _themeManager;

        // UI Controls (managed by UILayoutManager)
        private IUILayoutManager.UIControls _uiControls = null!;

        // Timer for status updates
        private System.Windows.Forms.Timer _statusTimer = null!;
        
        // Emergency unlock feedback tracking
        private System.Windows.Forms.Timer? _emergencyFeedbackTimer = null;
        private bool _showingEmergencyFeedback = false;

        public MainForm(
            IKeyboardBlockerService keyboardBlockerService,
            ILogger<MainForm> logger,
            UISettings uiSettings,
            IStatusBarManager statusBarManager,
            ILogoManager logoManager,
            IUILayoutManager layoutManager,
            IKeyboardShortcutManager shortcutManager,
            IResourceMonitor resourceMonitor,
            IThemeManager themeManager,
            IKeyboardInfoService keyboardInfoService)
        {
            _keyboardBlockerService = keyboardBlockerService ?? throw new ArgumentNullException(nameof(keyboardBlockerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _statusBarManager = statusBarManager ?? throw new ArgumentNullException(nameof(statusBarManager));
            _logoManager = logoManager ?? throw new ArgumentNullException(nameof(logoManager));
            _layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
            _shortcutManager = shortcutManager ?? throw new ArgumentNullException(nameof(shortcutManager));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
            _keyboardInfoService = keyboardInfoService ?? throw new ArgumentNullException(nameof(keyboardInfoService));
            
            _viewModel = new MainWindowViewModel();

            InitializeComponent();
            InitializeEventHandlers();
            InitializeTimer();
            UpdateUI();
            
            // Load keyboard name initially
            _ = LoadKeyboardNameAsync();
            
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
            
            // Initialize status bar
            _statusBarManager.Initialize(this);
            
            // Register components with theme manager
            _themeManager.RegisterComponents(this, _layoutManager, _statusBarManager);
        }

        private void InitializeEventHandlers()
        {
            _uiControls.ToggleButton.Click += OnToggleButtonClick;
            _uiControls.HideToTrayButton.Click += OnHideToTrayButtonClick;
            _uiControls.ThemeToggleButton.Click += OnThemeToggleButtonClick;
            _keyboardBlockerService.StateChanged += OnKeyboardStateChanged;
            _keyboardBlockerService.EmergencyUnlockAttempt += OnEmergencyUnlockAttempt;
            _keyboardBlockerService.ShowWindowRequested += OnShowWindowRequested;

            // Handle form closing
            FormClosing += OnFormClosing;
            
            // Handle keyboard shortcuts
            KeyDown += OnKeyDown;
            
            // Wire up shortcut manager events
            _shortcutManager.ToggleRequested += (s, e) => OnToggleButtonClick(s, e);
            _shortcutManager.HideToTrayRequested += (s, e) => OnHideToTrayButtonClick(s, e);
            _shortcutManager.HelpRequested += (s, e) => _shortcutManager.ShowHelp();
            _shortcutManager.ThemeToggleRequested += (s, e) => OnThemeToggleButtonClick(s, e);
            
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

        private async void OnToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Toggle button clicked");
                
                // Set button to processing state
                _layoutManager.SetToggleButtonProcessing(_uiControls.ToggleButton, true);
                
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
                _layoutManager.SetToggleButtonProcessing(_uiControls.ToggleButton, false);
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

        private void OnThemeToggleButtonClick(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Theme toggle button clicked");
                _themeManager.ToggleTheme();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling theme");
                MessageBox.Show($"Failed to toggle theme.\n\nError: {ex.Message}",
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
                
                // Update theme button text
                _layoutManager.UpdateThemeButton(_uiControls.ThemeToggleButton);
                
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

            _viewModel.UpdateFromState(state);
            UpdateUI();
            _statusBarManager.UpdateBlockingState(state.IsBlocked);
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
            
            // Update keyboard name periodically (every 30 seconds)
            if (DateTime.Now.Second % 30 == 0)
            {
                _ = LoadKeyboardNameAsync();
            }
        }

        private void UpdateHookStatus()
        {
            try
            {
                // Check if the keyboard service is working properly
                var currentState = _keyboardBlockerService.CurrentState;
                bool isActive = currentState != null;
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
                    Invoke(new Action(() => _layoutManager.UpdateKeyboardNameLabel(_uiControls.KeyboardNameLabel, keyboardName)));
                }
                else
                {
                    _layoutManager.UpdateKeyboardNameLabel(_uiControls.KeyboardNameLabel, keyboardName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading keyboard name");
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() => _layoutManager.UpdateKeyboardNameLabel(_uiControls.KeyboardNameLabel, "Keyboard info unavailable")));
                }
                else
                {
                    _layoutManager.UpdateKeyboardNameLabel(_uiControls.KeyboardNameLabel, "Keyboard info unavailable");
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
                // Only show emergency feedback when keyboard is blocked
                if (!_viewModel.IsKeyboardBlocked)
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
                
                // Temporarily change the toggle button text to show emergency feedback
                _uiControls.ToggleButton.Text = $"Emergency: {attemptCount}/3";
                _uiControls.ToggleButton.BackColor = _uiSettings.ErrorColor;
                
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
            _layoutManager.UpdateUI(_uiControls, _viewModel.IsKeyboardBlocked, _viewModel.StatusText,
                _viewModel.ToggleButtonText, _viewModel.LastToggleTime);
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
                _uiControls?.LogoIcon?.Image?.Dispose();
                _uiControls?.LogoIcon?.Dispose();
                _logoManager?.Dispose();
                _resourceMonitor?.Dispose();
            }
        }
    }
}
