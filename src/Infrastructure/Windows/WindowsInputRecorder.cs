using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimBlock.Infrastructure.Windows
{
    /// <summary>
    /// Windows-specific implementation of input recording using low-level hooks
    /// </summary>
    public class WindowsInputRecorder : IInputRecorder
    {
        private readonly ILogger<WindowsInputRecorder> _logger;

        // Windows API constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;

        // Hook delegates
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private readonly LowLevelKeyboardProc _keyboardHookProc;
        private readonly LowLevelMouseProc _mouseHookProc;

        private IntPtr _keyboardHookId = IntPtr.Zero;
        private IntPtr _mouseHookId = IntPtr.Zero;

        private readonly List<MacroEvent> _recordedEvents = new();
        private readonly Stopwatch _recordingStopwatch = new();

        private bool _isRecording;
        private bool _isPaused;
        private DateTime? _recordingStartTime;

        // Recording filters
        private bool _recordKeyboard = true;
        private bool _recordMouse = true;
        private bool _recordMouseMovement = false;
        private bool _recordDelays = true;
        private TimeSpan _minimumDelay = TimeSpan.FromMilliseconds(10);
        private TimeSpan _maxRecordingDuration = TimeSpan.FromMinutes(30);

        private Point _lastMousePosition = Point.Empty;
        private DateTime _lastEventTime = DateTime.MinValue;

        #region Events

        public event EventHandler<MacroEvent>? EventRecorded;
        public event EventHandler? RecordingStarted;
        public event EventHandler<TimeSpan>? RecordingStopped;

        #endregion

        #region Properties

        public bool IsRecording => _isRecording;
        public bool IsPaused => _isPaused;
        public DateTime? RecordingStartTime => _recordingStartTime;
        public int RecordedEventCount => _recordedEvents.Count;

        #endregion

        public WindowsInputRecorder(ILogger<WindowsInputRecorder> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _keyboardHookProc = KeyboardHookCallback;
            _mouseHookProc = MouseHookCallback;
        }

        public async Task StartRecordingAsync()
        {
            if (_isRecording)
                throw new InvalidOperationException("Recording is already in progress");

            try
            {
                _logger.LogInformation("Starting input recording...");

                _recordedEvents.Clear();
                _recordingStartTime = DateTime.UtcNow;
                _recordingStopwatch.Restart();
                _lastEventTime = DateTime.MinValue;
                _lastMousePosition = Point.Empty;

                // Install hooks
                if (_recordKeyboard)
                {
                    _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProc,
                        GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName), 0);
                    
                    if (_keyboardHookId == IntPtr.Zero)
                        throw new InvalidOperationException("Failed to install keyboard hook");
                }

                if (_recordMouse)
                {
                    _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc,
                        GetModuleHandle(Process.GetCurrentProcess().MainModule?.ModuleName), 0);
                    
                    if (_mouseHookId == IntPtr.Zero)
                        throw new InvalidOperationException("Failed to install mouse hook");
                }

                _isRecording = true;
                _isPaused = false;

                RecordingStarted?.Invoke(this, EventArgs.Empty);
                _logger.LogInformation("Input recording started successfully");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start input recording");
                await CleanupHooksAsync();
                throw;
            }
        }

        public async Task<IEnumerable<MacroEvent>> StopRecordingAsync()
        {
            if (!_isRecording)
                return new List<MacroEvent>();

            try
            {
                _logger.LogInformation("Stopping input recording...");

                _isRecording = false;
                _isPaused = false;
                _recordingStopwatch.Stop();

                await CleanupHooksAsync();

                var duration = _recordingStopwatch.Elapsed;
                var events = new List<MacroEvent>(_recordedEvents);

                RecordingStopped?.Invoke(this, duration);

                _logger.LogInformation("Input recording stopped. Recorded {EventCount} events in {Duration}ms", 
                    events.Count, duration.TotalMilliseconds);

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop input recording");
                throw;
            }
        }

        public void PauseRecording()
        {
            if (!_isRecording || _isPaused)
                return;

            _isPaused = true;
            _recordingStopwatch.Stop();
            _logger.LogDebug("Input recording paused");
        }

        public void ResumeRecording()
        {
            if (!_isRecording || !_isPaused)
                return;

            _isPaused = false;
            _recordingStopwatch.Start();
            _logger.LogDebug("Input recording resumed");
        }

        public void SetRecordingFilters(bool recordKeyboard, bool recordMouse, bool recordMouseMovement, bool recordDelays)
        {
            _recordKeyboard = recordKeyboard;
            _recordMouse = recordMouse;
            _recordMouseMovement = recordMouseMovement;
            _recordDelays = recordDelays;

            _logger.LogDebug("Recording filters updated: Keyboard={Keyboard}, Mouse={Mouse}, MouseMovement={MouseMovement}, Delays={Delays}",
                recordKeyboard, recordMouse, recordMouseMovement, recordDelays);
        }

        public void SetMinimumDelay(TimeSpan minimumDelay)
        {
            _minimumDelay = minimumDelay;
            _logger.LogDebug("Minimum delay set to {Delay}ms", minimumDelay.TotalMilliseconds);
        }

        public void SetMaxRecordingDuration(TimeSpan maxDuration)
        {
            _maxRecordingDuration = maxDuration;
            _logger.LogDebug("Maximum recording duration set to {Duration}ms", maxDuration.TotalMilliseconds);
        }

        public (bool Keyboard, bool Mouse, bool MouseMovement, bool Delays) GetRecordingFilters()
        {
            return (_recordKeyboard, _recordMouse, _recordMouseMovement, _recordDelays);
        }

        public void ClearCurrentSession()
        {
            _recordedEvents.Clear();
            _recordingStopwatch.Reset();
            _lastEventTime = DateTime.MinValue;
            _logger.LogDebug("Current recording session cleared");
        }

        public Dictionary<string, object> GetRecordingStats()
        {
            return new Dictionary<string, object>
            {
                ["IsRecording"] = _isRecording,
                ["IsPaused"] = _isPaused,
                ["RecordingStartTime"] = _recordingStartTime,
                ["ElapsedTime"] = _recordingStopwatch.Elapsed,
                ["RecordedEventCount"] = _recordedEvents.Count,
                ["RecordKeyboard"] = _recordKeyboard,
                ["RecordMouse"] = _recordMouse,
                ["RecordMouseMovement"] = _recordMouseMovement,
                ["RecordDelays"] = _recordDelays,
                ["MinimumDelay"] = _minimumDelay,
                ["MaxRecordingDuration"] = _maxRecordingDuration
            };
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isRecording && !_isPaused)
            {
                try
                {
                    // Check for maximum recording duration
                    if (_recordingStopwatch.Elapsed > _maxRecordingDuration)
                    {
                        _ = Task.Run(async () => await StopRecordingAsync());
                        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
                    }

                    var vkCode = Marshal.ReadInt32(lParam);
                    var inputType = wParam.ToInt32() == WM_KEYDOWN ? InputType.KeyDown : InputType.KeyUp;

                    var currentTime = _recordingStopwatch.Elapsed;
                    var macroEvent = MacroEvent.CreateKeyboardEvent(inputType, vkCode, currentTime);

                    RecordEvent(macroEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in keyboard hook callback");
                }
            }

            return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isRecording && !_isPaused)
            {
                try
                {
                    // Check for maximum recording duration
                    if (_recordingStopwatch.Elapsed > _maxRecordingDuration)
                    {
                        _ = Task.Run(async () => await StopRecordingAsync());
                        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
                    }

                    var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var currentTime = _recordingStopwatch.Elapsed;
                    var currentPosition = new Point(mouseStruct.x, mouseStruct.y);

                    MacroEvent? macroEvent = null;

                    switch (wParam.ToInt32())
                    {
                        case WM_LBUTTONDOWN:
                            macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseDown, 1, currentPosition.X, currentPosition.Y, currentTime);
                            break;
                        case WM_LBUTTONUP:
                            macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseUp, 1, currentPosition.X, currentPosition.Y, currentTime);
                            break;
                        case WM_RBUTTONDOWN:
                            macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseDown, 2, currentPosition.X, currentPosition.Y, currentTime);
                            break;
                        case WM_RBUTTONUP:
                            macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseUp, 2, currentPosition.X, currentPosition.Y, currentTime);
                            break;
                        case WM_MBUTTONDOWN:
                            macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseDown, 3, currentPosition.X, currentPosition.Y, currentTime);
                            break;
                        case WM_MBUTTONUP:
                            macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseUp, 3, currentPosition.X, currentPosition.Y, currentTime);
                            break;
                        case WM_MOUSEMOVE:
                            if (_recordMouseMovement && !currentPosition.Equals(_lastMousePosition))
                            {
                                macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseMove, null, currentPosition.X, currentPosition.Y, currentTime);
                                _lastMousePosition = currentPosition;
                            }
                            break;
                        case WM_MOUSEWHEEL:
                            var wheelDelta = (short)((mouseStruct.mouseData >> 16) & 0xFFFF);
                            macroEvent = MacroEvent.CreateMouseEvent(InputType.MouseWheel, null, currentPosition.X, currentPosition.Y, currentTime, wheelDelta);
                            break;
                    }

                    if (macroEvent != null)
                    {
                        RecordEvent(macroEvent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in mouse hook callback");
                }
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private void RecordEvent(MacroEvent macroEvent)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Check minimum delay filter
                if (_recordDelays && _lastEventTime != DateTime.MinValue)
                {
                    var timeSinceLastEvent = now - _lastEventTime;
                    if (timeSinceLastEvent < _minimumDelay)
                        return;
                }

                _recordedEvents.Add(macroEvent);
                _lastEventTime = now;

                EventRecorded?.Invoke(this, macroEvent);

                _logger.LogTrace("Recorded event: {Event}", macroEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record event");
            }
        }

        private async Task CleanupHooksAsync()
        {
            try
            {
                if (_keyboardHookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_keyboardHookId);
                    _keyboardHookId = IntPtr.Zero;
                }

                if (_mouseHookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHookId);
                    _mouseHookId = IntPtr.Zero;
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up hooks");
            }
        }

        #region Windows API

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        #endregion

        public void Dispose()
        {
            if (_isRecording)
            {
                _ = Task.Run(async () => await StopRecordingAsync());
            }
            else
            {
                _ = Task.Run(async () => await CleanupHooksAsync());
            }
        }
    }
}
