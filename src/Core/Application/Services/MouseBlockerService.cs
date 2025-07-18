using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Core.Application.Services
{
    /// <summary>
    /// Main application service that orchestrates mouse blocking functionality
    /// </summary>
    public class MouseBlockerService : IMouseBlockerService
    {
        private readonly IMouseHookService _hookService;
        private readonly ISystemTrayService _trayService;
        private readonly ILogger<MouseBlockerService> _logger;

        public event EventHandler<MouseBlockState>? StateChanged;
        public event EventHandler<int>? EmergencyUnlockAttempt;
        public event EventHandler? ShowWindowRequested;
        
        public MouseBlockState CurrentState => _hookService.CurrentState;

        public MouseBlockerService(
            IMouseHookService hookService,
            ISystemTrayService trayService,
            ILogger<MouseBlockerService> logger)
        {
            _hookService = hookService ?? throw new ArgumentNullException(nameof(hookService));
            _trayService = trayService ?? throw new ArgumentNullException(nameof(trayService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _hookService.BlockStateChanged += OnBlockStateChanged;
            _hookService.EmergencyUnlockAttempt += OnEmergencyUnlockAttempt;
            _trayService.TrayIconClicked += OnTrayIconClicked;
            _trayService.ShowWindowRequested += OnShowWindowRequested;
            _trayService.ExitRequested += OnExitRequested;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing MouseBlocker service...");
            
            await _hookService.InstallHookAsync();
            _trayService.Show();
            _trayService.UpdateIcon(false);
            _trayService.UpdateTooltip("SimBlock - Mouse unlocked");
            
            _logger.LogInformation("MouseBlocker service initialized successfully");
        }

        public async Task ShutdownAsync()
        {
            _logger.LogInformation("Shutting down MouseBlocker service...");
            
            await _hookService.UninstallHookAsync();
            _trayService.Hide();
            
            _logger.LogInformation("MouseBlocker service shut down successfully");
        }

        public async Task ToggleBlockingAsync()
        {
            await _hookService.ToggleBlockingAsync("User toggle");
        }

        public async Task SetBlockingAsync(bool shouldBlock)
        {
            await _hookService.SetBlockingAsync(shouldBlock, "User request");
        }
        
        /// <summary>
        /// Sets mouse blocking to simple mode (blocks all mouse actions when enabled)
        /// </summary>
        public async Task SetSimpleModeAsync()
        {
            await _hookService.SetSimpleModeAsync("User set simple mode");
        }
        
        /// <summary>
        /// Sets mouse blocking to advanced mode with specific configuration
        /// </summary>
        public async Task SetAdvancedModeAsync(AdvancedMouseConfiguration config)
        {
            await _hookService.SetAdvancedModeAsync(config, "User set advanced mode");
        }
        
        /// <summary>
        /// Sets mouse blocking to select mode with specific configuration
        /// </summary>
        public async Task SetSelectModeAsync(AdvancedMouseConfiguration config)
        {
            await _hookService.SetSelectModeAsync(config, "User set select mode");
        }
        
        /// <summary>
        /// Applies current selection to blocking and switches to advanced mode
        /// </summary>
        public async Task ApplySelectionAsync()
        {
            var currentState = CurrentState;
            if (currentState.AdvancedConfig != null)
            {
                // Apply selection to convert selected components to blocked components
                currentState.AdvancedConfig.ApplySelection();
                
                // Switch to advanced mode with the updated configuration
                await _hookService.SetAdvancedModeAsync(currentState.AdvancedConfig, "User applied selection");
            }
        }

        public Task ShowMainWindowAsync()
        {
            // This will be implemented when we create the main window
            _logger.LogInformation("Show main window requested");
            return Task.CompletedTask;
        }

        public Task HideToTrayAsync()
        {
            // Hide main window to tray
            _logger.LogInformation("Hide to tray requested");
            return Task.CompletedTask;
        }

        private void OnBlockStateChanged(object? sender, MouseBlockState state)
        {
            _logger.LogInformation("Mouse block state changed: {IsBlocked}", state.IsBlocked);
            
            _trayService.UpdateIcon(state.IsBlocked);
            _trayService.UpdateTooltip(state.IsBlocked ? 
                "SimBlock - Mouse BLOCKED" : 
                "SimBlock - Mouse unlocked");
            
            StateChanged?.Invoke(this, state);
        }

        private void OnTrayIconClicked(object? sender, EventArgs e)
        {
            _ = ToggleBlockingAsync();
        }

        private void OnShowWindowRequested(object? sender, EventArgs e)
        {
            _logger.LogInformation("Show window requested from tray");
            ShowWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnEmergencyUnlockAttempt(object? sender, int attemptCount)
        {
            _logger.LogInformation("Emergency unlock attempt forwarded: {AttemptCount}", attemptCount);
            EmergencyUnlockAttempt?.Invoke(this, attemptCount);
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            _ = ShutdownAsync();
            Environment.Exit(0);
        }
    }
}