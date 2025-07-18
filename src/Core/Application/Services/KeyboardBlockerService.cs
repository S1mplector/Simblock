using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Core.Application.Services
{
    /// <summary>
    /// Main application service that orchestrates keyboard blocking functionality
    /// </summary>
    public class KeyboardBlockerService : IKeyboardBlockerService
    {
        private readonly IKeyboardHookService _hookService;
        private readonly ISystemTrayService _trayService;
        private readonly ILogger<KeyboardBlockerService> _logger;

        public event EventHandler<KeyboardBlockState>? StateChanged;
        public event EventHandler<int>? EmergencyUnlockAttempt;
        public event EventHandler? ShowWindowRequested;
        
        public KeyboardBlockState CurrentState => _hookService.CurrentState;

        public KeyboardBlockerService(
            IKeyboardHookService hookService,
            ISystemTrayService trayService,
            ILogger<KeyboardBlockerService> logger)
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
            _logger.LogInformation("Initializing KeyboardBlocker service...");
            
            await _hookService.InstallHookAsync();
            _trayService.Show();
            _trayService.UpdateIcon(false);
            _trayService.UpdateTooltip("SimBlock - Keyboard unlocked");
            
            _logger.LogInformation("KeyboardBlocker service initialized successfully");
        }

        public async Task ShutdownAsync()
        {
            _logger.LogInformation("Shutting down KeyboardBlocker service...");
            
            await _hookService.UninstallHookAsync();
            _trayService.Hide();
            
            _logger.LogInformation("KeyboardBlocker service shut down successfully");
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
        /// Sets keyboard blocking to simple mode (blocks all keys when enabled)
        /// </summary>
        public async Task SetSimpleModeAsync()
        {
            await _hookService.SetSimpleModeAsync("User set simple mode");
        }
        
        /// <summary>
        /// Sets keyboard blocking to advanced mode with specific configuration
        /// </summary>
        public async Task SetAdvancedModeAsync(AdvancedKeyboardConfiguration config)
        {
            await _hookService.SetAdvancedModeAsync(config, "User set advanced mode");
        }
        
        /// <summary>
        /// Sets keyboard blocking to select mode with specific configuration
        /// </summary>
        public async Task SetSelectModeAsync(AdvancedKeyboardConfiguration config)
        {
            Console.WriteLine($"KeyboardBlockerService.SetSelectModeAsync: BEFORE hook service call - Current mode: {CurrentState.Mode}");
            _logger.LogInformation("KeyboardBlockerService.SetSelectModeAsync: BEFORE hook service call - Current mode: {CurrentMode}", CurrentState.Mode);
            
            try
            {
                await _hookService.SetSelectModeAsync(config, "User set select mode");
                Console.WriteLine($"KeyboardBlockerService.SetSelectModeAsync: AFTER hook service call - Current mode: {CurrentState.Mode}");
                _logger.LogInformation("KeyboardBlockerService.SetSelectModeAsync: AFTER hook service call - Current mode: {CurrentMode}", CurrentState.Mode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KeyboardBlockerService.SetSelectModeAsync: EXCEPTION: {ex.Message}");
                _logger.LogError(ex, "KeyboardBlockerService.SetSelectModeAsync: Exception occurred");
                throw;
            }
        }
        
        /// <summary>
        /// Applies current selection to blocking and switches to advanced mode
        /// </summary>
        public async Task ApplySelectionAsync()
        {
            var currentState = CurrentState;
            if (currentState.AdvancedConfig != null)
            {
                // Apply selection to convert selected keys to blocked keys
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

        private void OnBlockStateChanged(object? sender, KeyboardBlockState state)
        {
            _logger.LogInformation("Keyboard block state changed: {IsBlocked}", state.IsBlocked);
            
            _trayService.UpdateIcon(state.IsBlocked);
            _trayService.UpdateTooltip(state.IsBlocked ? 
                "SimBlock - Keyboard BLOCKED" : 
                "SimBlock - Keyboard unlocked");
            
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
