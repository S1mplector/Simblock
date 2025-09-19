using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;
using SimBlock.Presentation.Configuration;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation of mouse hook service using Win32 API
    /// </summary>
    public class WindowsMouseHookService : IMouseHookService
    {
        private readonly ILogger<WindowsMouseHookService> _logger;
        private readonly UISettings _uiSettings;
        private readonly MouseBlockState _state;
        private readonly IKeyboardHookService _keyboardHookService;
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelMouseProc _proc;

        // Emergency unlock tracking (handled via keyboard service)
        private const int EMERGENCY_UNLOCK_REQUIRED_PRESSES = 3;

        public event EventHandler<MouseBlockState>? BlockStateChanged;
        public event EventHandler<int>? EmergencyUnlockAttempt;
        public event EventHandler<MouseHookEventArgs>? MouseEvent;

        public bool IsHookInstalled => _hookId != IntPtr.Zero;
        public MouseBlockState CurrentState => _state;

        public WindowsMouseHookService(
            ILogger<WindowsMouseHookService> logger, 
            UISettings uiSettings,
            IKeyboardHookService keyboardHookService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
            _keyboardHookService = keyboardHookService ?? throw new ArgumentNullException(nameof(keyboardHookService));
            _state = new MouseBlockState();
            _proc = HookCallback;

            // Subscribe to keyboard hook service for emergency unlock
            _keyboardHookService.EmergencyUnlockAttempt += OnKeyboardEmergencyUnlockAttempt;
        }

        public Task InstallHookAsync()
        {
            if (_hookId != IntPtr.Zero)
            {
                _logger.LogWarning("Mouse hook is already installed");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Installing mouse hook...");

            // For low-level mouse hooks (WH_MOUSE_LL) the hMod parameter MUST be NULL (IntPtr.Zero)
            _hookId = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_MOUSE_LL,
                _proc,
                IntPtr.Zero,
                0);

            if (_hookId == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to install mouse hook. Error code: {ErrorCode}", error);
                throw new InvalidOperationException($"Failed to install mouse hook. Error code: {error}");
            }

            _logger.LogInformation("Mouse hook installed successfully");

            return Task.CompletedTask;
        }

        public Task UninstallHookAsync()
        {
            if (_hookId == IntPtr.Zero)
            {
                _logger.LogWarning("Mouse hook is not installed");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Uninstalling mouse hook...");

            bool result = NativeMethods.UnhookWindowsHookEx(_hookId);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to uninstall mouse hook. Error code: {ErrorCode}", error);
            }
            else
            {
                _logger.LogInformation("Mouse hook uninstalled successfully");
            }

            _hookId = IntPtr.Zero;

            return Task.CompletedTask;
        }

        public Task SetBlockingAsync(bool shouldBlock, string? reason = null)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("Setting mouse blocking to {ShouldBlock}. Reason: {Reason}", 
                    shouldBlock, reason ?? "Not specified");

                _state.SetBlocked(shouldBlock, reason);
                BlockStateChanged?.Invoke(this, _state);
            });
        }

        public Task ToggleBlockingAsync(string? reason = null)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("Toggling mouse blocking. Current state: {CurrentState}. Reason: {Reason}",
                    _state.IsBlocked, reason ?? "Not specified");

                _state.Toggle(reason);
                BlockStateChanged?.Invoke(this, _state);
            });
        }
        
        /// <summary>
        /// Sets the mouse blocking to simple mode
        /// </summary>
        public Task SetSimpleModeAsync(string? reason = null)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("Setting mouse blocking to simple mode. Reason: {Reason}",
                    reason ?? "Not specified");

                _state.SetSimpleMode(reason);
                BlockStateChanged?.Invoke(this, _state);
            });
        }
        
        /// <summary>
        /// Sets the mouse blocking to advanced mode with specific configuration
        /// </summary>
        public Task SetAdvancedModeAsync(AdvancedMouseConfiguration config, string? reason = null)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("Setting mouse blocking to advanced mode. Reason: {Reason}",
                    reason ?? "Not specified");

                _state.SetAdvancedMode(config, reason);
                BlockStateChanged?.Invoke(this, _state);
            });
        }
        
        /// <summary>
        /// Sets the mouse blocking to select mode with specific configuration
        /// </summary>
        public Task SetSelectModeAsync(AdvancedMouseConfiguration config, string? reason = null)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"WindowsMouseHookService.SetSelectModeAsync: BEFORE SetSelectMode - Current mode: {_state.Mode}");
                _logger.LogInformation("WindowsMouseHookService.SetSelectModeAsync: BEFORE SetSelectMode - Current mode: {CurrentMode}. Reason: {Reason}",
                    _state.Mode, reason ?? "Not specified");

                try
                {
                    _state.SetSelectMode(config, reason);
                    Console.WriteLine($"WindowsMouseHookService.SetSelectModeAsync: AFTER SetSelectMode - Current mode: {_state.Mode}");
                    _logger.LogInformation("WindowsMouseHookService.SetSelectModeAsync: AFTER SetSelectMode - Current mode: {CurrentMode}", _state.Mode);
                    
                    BlockStateChanged?.Invoke(this, _state);
                    Console.WriteLine($"WindowsMouseHookService.SetSelectModeAsync: AFTER BlockStateChanged event - Current mode: {_state.Mode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WindowsMouseHookService.SetSelectModeAsync: EXCEPTION: {ex.Message}");
                    _logger.LogError(ex, "WindowsMouseHookService.SetSelectModeAsync: Exception occurred");
                    throw;
                }
            });
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var mouseStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
                int message = wParam.ToInt32();
                
                // Log mouse activity for debugging
                _logger.LogDebug("Mouse event: Message={Message}, X={X}, Y={Y}, MouseData={MouseData}",
                    GetMouseMessageName(message), mouseStruct.x, mouseStruct.y, mouseStruct.mouseData);

                // Raise per-mouse event for listeners (e.g., macro recording)
                try
                {
                    int wheelDelta = 0;
                    if (message == NativeMethods.WM_MOUSEWHEEL || message == NativeMethods.WM_MOUSEHWHEEL)
                    {
                        // HIWORD of mouseData is wheel delta (signed)
                        wheelDelta = unchecked((short)((mouseStruct.mouseData >> 16) & 0xFFFF));
                    }

                    bool left = message == NativeMethods.WM_LBUTTONDOWN || message == NativeMethods.WM_LBUTTONUP;
                    bool right = message == NativeMethods.WM_RBUTTONDOWN || message == NativeMethods.WM_RBUTTONUP;
                    bool middle = message == NativeMethods.WM_MBUTTONDOWN || message == NativeMethods.WM_MBUTTONUP;

                    var args = new MouseHookEventArgs
                    {
                        Message = message,
                        X = mouseStruct.x,
                        Y = mouseStruct.y,
                        MouseData = mouseStruct.mouseData,
                        LeftButton = left,
                        RightButton = right,
                        MiddleButton = middle,
                        WheelDelta = wheelDelta,
                        Timestamp = DateTime.UtcNow
                    };
                    MouseEvent?.Invoke(this, args);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error raising MouseEvent");
                }
                
                // Check if we should block the mouse input using the new advanced blocking logic
                if (_state.IsMouseActionBlocked(message, mouseStruct.mouseData))
                {
                    _logger.LogDebug("Blocking mouse input: {Message} (Mode: {Mode})",
                        GetMouseMessageName(message), _state.Mode);
                    return (IntPtr)1; // Block the mouse input
                }
            }

            // Allow the mouse input to pass through
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void OnKeyboardEmergencyUnlockAttempt(object? sender, int attemptCount)
        {
            try
            {
                _logger.LogInformation("Mouse service received emergency unlock attempt from keyboard. Attempt count: {Count}/{Required}",
                    attemptCount, EMERGENCY_UNLOCK_REQUIRED_PRESSES);
                
                // Forward the emergency unlock attempt event
                EmergencyUnlockAttempt?.Invoke(this, attemptCount);

                // If emergency unlock is successful, disable mouse blocking
                if (attemptCount >= EMERGENCY_UNLOCK_REQUIRED_PRESSES)
                {
                    _logger.LogWarning("Emergency unlock activated via keyboard! Mouse will be unlocked. Current mouse state: IsBlocked={IsBlocked}, Mode={Mode}",
                        _state.IsBlocked, _state.Mode);
                    _ = SetBlockingAsync(false, "Emergency unlock (3x Ctrl+Alt+U)");
                    _logger.LogInformation("Mouse SetBlockingAsync(false) called for emergency unlock");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling keyboard emergency unlock attempt");
            }
        }

        private string GetMouseMessageName(int message)
        {
            return message switch
            {
                NativeMethods.WM_LBUTTONDOWN => "WM_LBUTTONDOWN",
                NativeMethods.WM_LBUTTONUP => "WM_LBUTTONUP",
                NativeMethods.WM_RBUTTONDOWN => "WM_RBUTTONDOWN",
                NativeMethods.WM_RBUTTONUP => "WM_RBUTTONUP",
                NativeMethods.WM_MBUTTONDOWN => "WM_MBUTTONDOWN",
                NativeMethods.WM_MBUTTONUP => "WM_MBUTTONUP",
                NativeMethods.WM_MOUSEMOVE => "WM_MOUSEMOVE",
                NativeMethods.WM_MOUSEWHEEL => "WM_MOUSEWHEEL",
                NativeMethods.WM_MOUSEHWHEEL => "WM_MOUSEHWHEEL",
                NativeMethods.WM_XBUTTONDOWN => "WM_XBUTTONDOWN",
                NativeMethods.WM_XBUTTONUP => "WM_XBUTTONUP",
                _ => $"Unknown({message:X})"
            };
        }

        public void Dispose()
        {
            try
            {
                // Unsubscribe from keyboard hook service events
                if (_keyboardHookService != null)
                {
                    _keyboardHookService.EmergencyUnlockAttempt -= OnKeyboardEmergencyUnlockAttempt;
                }

                // Uninstall the hook
                if (_hookId != IntPtr.Zero)
                {
                    _ = UninstallHookAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during WindowsMouseHookService disposal");
            }
        }
    }
}