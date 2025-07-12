using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation of keyboard hook service using Win32 API
    /// </summary>
    public class WindowsKeyboardHookService : IKeyboardHookService
    {
        private readonly ILogger<WindowsKeyboardHookService> _logger;
        private readonly KeyboardBlockState _state;
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;

        public event EventHandler<KeyboardBlockState>? BlockStateChanged;

        public bool IsHookInstalled => _hookId != IntPtr.Zero;
        public KeyboardBlockState CurrentState => _state;

        public WindowsKeyboardHookService(ILogger<WindowsKeyboardHookService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = new KeyboardBlockState();
            _proc = HookCallback;
        }

        public Task InstallHookAsync()
        {
            if (_hookId != IntPtr.Zero)
            {
                _logger.LogWarning("Hook is already installed");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Installing keyboard hook...");

            // For low-level keyboard hooks (WH_KEYBOARD_LL) the hMod parameter MUST be NULL (IntPtr.Zero)
            _hookId = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _proc,
                IntPtr.Zero,
                0);

            if (_hookId == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to install keyboard hook. Error code: {ErrorCode}", error);
                throw new InvalidOperationException($"Failed to install keyboard hook. Error code: {error}");
            }

            _logger.LogInformation("Keyboard hook installed successfully");

            return Task.CompletedTask;
        }

        public Task UninstallHookAsync()
        {
            if (_hookId == IntPtr.Zero)
            {
                _logger.LogWarning("Hook is not installed");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Uninstalling keyboard hook...");

            bool result = NativeMethods.UnhookWindowsHookEx(_hookId);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to uninstall keyboard hook. Error code: {ErrorCode}", error);
            }
            else
            {
                _logger.LogInformation("Keyboard hook uninstalled successfully");
            }

            _hookId = IntPtr.Zero;

            return Task.CompletedTask;
        }

        public Task SetBlockingAsync(bool shouldBlock, string? reason = null)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("Setting keyboard blocking to {ShouldBlock}. Reason: {Reason}", 
                    shouldBlock, reason ?? "Not specified");

                _state.SetBlocked(shouldBlock, reason);
                BlockStateChanged?.Invoke(this, _state);
            });
        }

        public Task ToggleBlockingAsync(string? reason = null)
        {
            return Task.Run(() =>
            {
                _logger.LogInformation("Toggling keyboard blocking. Current state: {CurrentState}. Reason: {Reason}", 
                    _state.IsBlocked, reason ?? "Not specified");

                _state.Toggle(reason);
                BlockStateChanged?.Invoke(this, _state);
            });
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // Check if we should block the key
                if (_state.IsBlocked)
                {
                    // Check for emergency unlock combination (Ctrl+Alt+U)
                    if (IsEmergencyUnlockCombination(lParam))
                    {
                        _logger.LogWarning("Emergency unlock combination detected - unblocking keyboard");
                        _ = SetBlockingAsync(false, "Emergency unlock");
                        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                    }

                    // Block all other keys when blocking is enabled
                    _logger.LogDebug("Blocking keyboard input");
                    return (IntPtr)1; // Return non-zero to suppress the key
                }
            }

            // Allow the key to pass through
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool IsEmergencyUnlockCombination(IntPtr lParam)
        {
            try
            {
                var kbStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
                
                // Check if it's the 'U' key (Virtual Key Code 85)
                if (kbStruct.vkCode == 85) // VK_U
                {
                    // Check if Ctrl and Alt are pressed
                    bool ctrlPressed = (Control.ModifierKeys & Keys.Control) != 0;
                    bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
                    
                    return ctrlPressed && altPressed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking emergency unlock combination");
            }

            return false;
        }
    }
}
