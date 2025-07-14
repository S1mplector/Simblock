using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
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
        
        // UI Managers
        private readonly UISettings _uiSettings;
        private readonly IStatusBarManager _statusBarManager;
        private readonly ILogoManager _logoManager;
        private readonly IUILayoutManager _layoutManager;
        private readonly IKeyboardShortcutManager _shortcutManager;

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
            IResourceMonitor resourceMonitor)
        {
            _keyboardBlockerService = keyboardBlockerService ?? throw new ArgumentNullException(nameof(keyboardBlockerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _statusBarManager = statusBarManager ?? throw new ArgumentNullException(nameof(statusBarManager));
            _logoManager = logoManager ?? throw new ArgumentNullException(nameof(logoManager));
            _layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
            _shortcutManager = shortcutManager ?? throw new ArgumentNullException(nameof(shortcutManager));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            
            _viewModel = new MainWindowViewModel();

            InitializeComponent();
            InitializeEventHandlers();
            InitializeTimer();
            UpdateUI();
            
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
        }

        private void InitializeEventHandlers()
        {
            _uiControls.ToggleButton.Click += OnToggleButtonClick;
            _uiControls.HideToTrayButton.Click += OnHideToTrayButtonClick;
            _keyboardBlockerService.StateChanged += OnKeyboardStateChanged;
            _keyboardBlockerService.EmergencyUnlockAttempt += OnEmergencyUnlockAttempt;

            // Handle form closing
            FormClosing += OnFormClosing;
            
            // Handle keyboard shortcuts
            KeyDown += OnKeyDown;
            
            // Wire up shortcut manager events
            _shortcutManager.ToggleRequested += (s, e) => OnToggleButtonClick(s, e);
            _shortcutManager.HideToTrayRequested += (s, e) => OnHideToTrayButtonClick(s, e);
            _shortcutManager.HelpRequested += (s, e) => _shortcutManager.ShowHelp();
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

        private void ShowEmergencyUnlockFeedback(int attemptCount)
        {
            try
            {
                // Only show emergency feedback when keyboard is blocked
                if (!_viewModel.IsKeyboardBlocked)
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
