using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using System.Runtime.InteropServices;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation of input playback using Windows API
    /// </summary>
    public class WindowsInputPlayer : IInputPlayer
    {
        private readonly ILogger<WindowsInputPlayer> _logger;

        // Windows API constants
        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private bool _isPlaying;
        private bool _isPaused;
        private Macro? _currentMacro;
        private int _currentEventIndex;
        private int _currentIteration;
        private CancellationTokenSource? _cancellationTokenSource;

        // Playback settings
        private double _speedMultiplier = 1.0;
        private bool _respectTiming = true;
        private TimeSpan _customDelay = TimeSpan.FromMilliseconds(50);
        private IntPtr _targetWindowHandle = IntPtr.Zero;

        #region Events

        public event EventHandler<Macro>? PlaybackStarted;
        public event EventHandler<(Macro Macro, bool Success, string? Error)>? PlaybackStopped;
        public event EventHandler<Macro>? PlaybackPaused;
        public event EventHandler<Macro>? PlaybackResumed;
        public event EventHandler<(MacroEvent Event, int Index, bool Success)>? EventExecuted;
        public event EventHandler<(int Current, int Total, double Percentage)>? ProgressChanged;

        #endregion

        #region Properties

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public Macro? CurrentMacro => _currentMacro;
        public int CurrentEventIndex => _currentEventIndex;
        public int CurrentIteration => _currentIteration;

        #endregion

        public WindowsInputPlayer(ILogger<WindowsInputPlayer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PlayMacroAsync(Macro macro, MacroExecutionMode? executionMode = null, int? repeatCount = null)
        {
            if (macro == null)
                throw new ArgumentNullException(nameof(macro));

            if (_isPlaying)
                throw new InvalidOperationException("Another macro is already playing");

            var (canPlay, reason) = await ValidateMacroAsync(macro);
            if (!canPlay)
                throw new InvalidOperationException($"Cannot play macro: {reason}");

            try
            {
                _logger.LogInformation("Starting playback of macro: {MacroName}", macro.Name);

                _currentMacro = macro;
                _currentEventIndex = 0;
                _currentIteration = 0;
                _isPlaying = true;
                _isPaused = false;
                _cancellationTokenSource = new CancellationTokenSource();

                PlaybackStarted?.Invoke(this, macro);

                var effectiveExecutionMode = executionMode ?? macro.ExecutionMode;
                var effectiveRepeatCount = repeatCount ?? macro.RepeatCount;

                await ExecuteMacroAsync(macro, effectiveExecutionMode, effectiveRepeatCount, _cancellationTokenSource.Token);

                _logger.LogInformation("Completed playback of macro: {MacroName}", macro.Name);
                PlaybackStopped?.Invoke(this, (macro, true, null));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Playback of macro {MacroName} was cancelled", macro.Name);
                PlaybackStopped?.Invoke(this, (macro, false, "Playback was cancelled"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during playback of macro: {MacroName}", macro.Name);
                PlaybackStopped?.Invoke(this, (macro, false, ex.Message));
                throw;
            }
            finally
            {
                _isPlaying = false;
                _isPaused = false;
                _currentMacro = null;
                _currentEventIndex = 0;
                _currentIteration = 0;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task StopPlaybackAsync()
        {
            if (!_isPlaying)
                return;

            try
            {
                _logger.LogInformation("Stopping macro playback");
                _cancellationTokenSource?.Cancel();
                
                // Wait a bit for the cancellation to take effect
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping macro playback");
                throw;
            }
        }

        public void PausePlayback()
        {
            if (!_isPlaying || _isPaused)
                return;

            _isPaused = true;
            _logger.LogDebug("Macro playback paused");
            
            if (_currentMacro != null)
                PlaybackPaused?.Invoke(this, _currentMacro);
        }

        public void ResumePlayback()
        {
            if (!_isPlaying || !_isPaused)
                return;

            _isPaused = false;
            _logger.LogDebug("Macro playback resumed");
            
            if (_currentMacro != null)
                PlaybackResumed?.Invoke(this, _currentMacro);
        }

        public async Task<bool> PlayEventAsync(MacroEvent macroEvent)
        {
            if (macroEvent == null)
                throw new ArgumentNullException(nameof(macroEvent));

            try
            {
                return await ExecuteEventAsync(macroEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play event: {Event}", macroEvent);
                return false;
            }
        }

        public void SetPlaybackSpeed(double speedMultiplier)
        {
            _speedMultiplier = Math.Max(0.1, Math.Min(10.0, speedMultiplier));
            _logger.LogDebug("Playback speed set to {Speed}x", _speedMultiplier);
        }

        public double GetPlaybackSpeed()
        {
            return _speedMultiplier;
        }

        public void SetRespectTiming(bool respectTiming)
        {
            _respectTiming = respectTiming;
            _logger.LogDebug("Respect timing set to {RespectTiming}", respectTiming);
        }

        public void SetCustomDelay(TimeSpan delay)
        {
            _customDelay = delay;
            _logger.LogDebug("Custom delay set to {Delay}ms", delay.TotalMilliseconds);
        }

        public void SetTargetWindow(IntPtr windowHandle)
        {
            _targetWindowHandle = windowHandle;
            _logger.LogDebug("Target window handle set to {Handle}", windowHandle);
        }

        public Dictionary<string, object> GetPlaybackStats()
        {
            return new Dictionary<string, object>
            {
                ["IsPlaying"] = _isPlaying,
                ["IsPaused"] = _isPaused,
                ["CurrentMacro"] = _currentMacro?.Name ?? "None",
                ["CurrentEventIndex"] = _currentEventIndex,
                ["CurrentIteration"] = _currentIteration,
                ["SpeedMultiplier"] = _speedMultiplier,
                ["RespectTiming"] = _respectTiming,
                ["CustomDelay"] = _customDelay,
                ["TargetWindowHandle"] = _targetWindowHandle
            };
        }

        public async Task<(bool CanPlay, string? Reason)> ValidateMacroAsync(Macro macro)
        {
            if (macro == null)
                return (false, "Macro is null");

            if (!macro.IsEnabled)
                return (false, "Macro is disabled");

            if (macro.Events.Count == 0)
                return (false, "Macro has no events");

            // Additional validation can be added here
            await Task.CompletedTask;
            return (true, null);
        }

        public TimeSpan GetEstimatedDuration(Macro macro, MacroExecutionMode? executionMode = null, int? repeatCount = null)
        {
            if (macro == null || macro.Events.Count == 0)
                return TimeSpan.Zero;

            var baseDuration = macro.Duration;
            var effectiveExecutionMode = executionMode ?? macro.ExecutionMode;
            var effectiveRepeatCount = repeatCount ?? macro.RepeatCount;

            // Adjust for speed multiplier
            baseDuration = TimeSpan.FromMilliseconds(baseDuration.TotalMilliseconds / _speedMultiplier);

            return effectiveExecutionMode switch
            {
                MacroExecutionMode.Once => baseDuration,
                MacroExecutionMode.Repeat => TimeSpan.FromMilliseconds(baseDuration.TotalMilliseconds * effectiveRepeatCount),
                MacroExecutionMode.Loop => TimeSpan.MaxValue, // Infinite
                MacroExecutionMode.Interval => TimeSpan.MaxValue, // Depends on interval
                MacroExecutionMode.RandomInterval => TimeSpan.MaxValue, // Unpredictable
                MacroExecutionMode.UntilCondition => TimeSpan.MaxValue, // Depends on condition
                _ => baseDuration
            };
        }

        private async Task ExecuteMacroAsync(Macro macro, MacroExecutionMode executionMode, int repeatCount, CancellationToken cancellationToken)
        {
            switch (executionMode)
            {
                case MacroExecutionMode.Once:
                    await ExecuteMacroOnceAsync(macro, cancellationToken);
                    break;

                case MacroExecutionMode.Repeat:
                    for (int i = 0; i < repeatCount && !cancellationToken.IsCancellationRequested; i++)
                    {
                        _currentIteration = i + 1;
                        await ExecuteMacroOnceAsync(macro, cancellationToken);
                        
                        if (i < repeatCount - 1) // Don't delay after the last iteration
                            await Task.Delay(100, cancellationToken);
                    }
                    break;

                case MacroExecutionMode.Loop:
                    _currentIteration = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        _currentIteration++;
                        await ExecuteMacroOnceAsync(macro, cancellationToken);
                        await Task.Delay(100, cancellationToken);
                    }
                    break;

                case MacroExecutionMode.Interval:
                    _currentIteration = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        _currentIteration++;
                        await ExecuteMacroOnceAsync(macro, cancellationToken);
                        await Task.Delay(macro.IntervalMs, cancellationToken);
                    }
                    break;

                case MacroExecutionMode.RandomInterval:
                    _currentIteration = 0;
                    var random = new Random();
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        _currentIteration++;
                        await ExecuteMacroOnceAsync(macro, cancellationToken);
                        
                        var randomDelay = random.Next(macro.RandomIntervalRange.Min, macro.RandomIntervalRange.Max);
                        await Task.Delay(randomDelay, cancellationToken);
                    }
                    break;

                default:
                    await ExecuteMacroOnceAsync(macro, cancellationToken);
                    break;
            }
        }

        private async Task ExecuteMacroOnceAsync(Macro macro, CancellationToken cancellationToken)
        {
            var events = macro.Events.Where(e => e.IsEnabled).OrderBy(e => e.Timestamp).ToList();
            var totalEvents = events.Count;

            for (int i = 0; i < events.Count && !cancellationToken.IsCancellationRequested; i++)
            {
                _currentEventIndex = i;
                var macroEvent = events[i];

                // Wait while paused
                while (_isPaused && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(50, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    break;

                // Handle timing
                if (_respectTiming && i > 0)
                {
                    var previousEvent = events[i - 1];
                    var timeDifference = macroEvent.Timestamp - previousEvent.Timestamp;
                    var adjustedDelay = TimeSpan.FromMilliseconds(timeDifference.TotalMilliseconds / _speedMultiplier);
                    
                    if (adjustedDelay > TimeSpan.Zero)
                        await Task.Delay(adjustedDelay, cancellationToken);
                }
                else if (!_respectTiming)
                {
                    await Task.Delay(_customDelay, cancellationToken);
                }

                // Execute the event
                var success = await ExecuteEventAsync(macroEvent);
                
                EventExecuted?.Invoke(this, (macroEvent, i, success));
                
                // Report progress
                var percentage = (double)(i + 1) / totalEvents * 100;
                ProgressChanged?.Invoke(this, (i + 1, totalEvents, percentage));

                if (!success)
                {
                    _logger.LogWarning("Failed to execute event {Index}: {Event}", i, macroEvent);
                }
            }
        }

        private async Task<bool> ExecuteEventAsync(MacroEvent macroEvent)
        {
            try
            {
                switch (macroEvent.Type)
                {
                    case InputType.KeyDown:
                        return SendKeyboardInput(macroEvent.KeyCode!.Value, false);

                    case InputType.KeyUp:
                        return SendKeyboardInput(macroEvent.KeyCode!.Value, true);

                    case InputType.MouseDown:
                        return SendMouseInput(macroEvent.MouseButton!.Value, macroEvent.X!.Value, macroEvent.Y!.Value, true);

                    case InputType.MouseUp:
                        return SendMouseInput(macroEvent.MouseButton!.Value, macroEvent.X!.Value, macroEvent.Y!.Value, false);

                    case InputType.MouseMove:
                        return SendMouseMove(macroEvent.X!.Value, macroEvent.Y!.Value);

                    case InputType.MouseWheel:
                        return SendMouseWheel(macroEvent.X!.Value, macroEvent.Y!.Value, macroEvent.WheelDelta!.Value);

                    case InputType.Delay:
                        var delayMs = (int)(macroEvent.DelayMs!.Value / _speedMultiplier);
                        await Task.Delay(delayMs);
                        return true;

                    case InputType.TextInput:
                        return await SendTextInput(macroEvent.Text!);

                    default:
                        _logger.LogWarning("Unsupported event type: {EventType}", macroEvent.Type);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing event: {Event}", macroEvent);
                return false;
            }
        }

        private bool SendKeyboardInput(int keyCode, bool isKeyUp)
        {
            try
            {
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)keyCode,
                            dwFlags = isKeyUp ? KEYEVENTF_KEYUP : 0
                        }
                    }
                };

                var result = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send keyboard input: key={KeyCode}, isKeyUp={IsKeyUp}", keyCode, isKeyUp);
                return false;
            }
        }

        private bool SendMouseInput(int button, int x, int y, bool isButtonDown)
        {
            try
            {
                uint flags = button switch
                {
                    1 => isButtonDown ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP,
                    2 => isButtonDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP,
                    3 => isButtonDown ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP,
                    _ => 0
                };

                if (flags == 0)
                    return false;

                var input = new INPUT
                {
                    type = INPUT_MOUSE,
                    u = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = x,
                            dy = y,
                            dwFlags = flags | MOUSEEVENTF_ABSOLUTE,
                            mouseData = 0
                        }
                    }
                };

                var result = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send mouse input: button={Button}, x={X}, y={Y}, isButtonDown={IsButtonDown}", button, x, y, isButtonDown);
                return false;
            }
        }

        private bool SendMouseMove(int x, int y)
        {
            try
            {
                var input = new INPUT
                {
                    type = INPUT_MOUSE,
                    u = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = x,
                            dy = y,
                            dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                        }
                    }
                };

                var result = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send mouse move: x={X}, y={Y}", x, y);
                return false;
            }
        }

        private bool SendMouseWheel(int x, int y, int wheelDelta)
        {
            try
            {
                var input = new INPUT
                {
                    type = INPUT_MOUSE,
                    u = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = x,
                            dy = y,
                            dwFlags = MOUSEEVENTF_WHEEL,
                            mouseData = (uint)wheelDelta
                        }
                    }
                };

                var result = SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send mouse wheel: x={X}, y={Y}, delta={Delta}", x, y, wheelDelta);
                return false;
            }
        }

        private async Task<bool> SendTextInput(string text)
        {
            try
            {
                // Simple text input by sending individual characters
                foreach (char c in text)
                {
                    var keyCode = VkKeyScan(c);
                    if (keyCode != -1)
                    {
                        SendKeyboardInput(keyCode & 0xFF, false);
                        await Task.Delay(10);
                        SendKeyboardInput(keyCode & 0xFF, true);
                        await Task.Delay(10);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send text input: {Text}", text);
                return false;
            }
        }

        #region Windows API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        #endregion
    }
}
