using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;
using System.Windows.Forms;

namespace SimBlock.Core.Application.Services
{
    public sealed class MacroService : IMacroService, IDisposable
    {
        private readonly ILogger<MacroService> _logger;
        private readonly IKeyboardHookService _keyboardHookService;
        private readonly IMouseHookService _mouseHookService;
        private readonly string _storageDir;

        private DateTime _recordingStartUtc;
        private bool _disposed;

        public bool IsRecording { get; private set; }
        public Macro? CurrentRecording { get; private set; }

        public MacroService(
            ILogger<MacroService> logger,
            IKeyboardHookService keyboardHookService,
            IMouseHookService mouseHookService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyboardHookService = keyboardHookService ?? throw new ArgumentNullException(nameof(keyboardHookService));
            _mouseHookService = mouseHookService ?? throw new ArgumentNullException(nameof(mouseHookService));

            // Storage directory (e.g., %AppData%/SimBlock/Macros)
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _storageDir = Path.Combine(appData, "SimBlock", "Macros");
            Directory.CreateDirectory(_storageDir);

            // Subscribe for recording
            _keyboardHookService.KeyEvent += OnKeyEvent;
            _mouseHookService.MouseEvent += OnMouseEvent;
        }

        public void StartRecording(string name)
        {
            if (IsRecording)
            {
                _logger.LogWarning("StartRecording called while already recording");
                return;
            }

            CurrentRecording = new Macro { Name = name };
            _recordingStartUtc = DateTime.UtcNow;
            IsRecording = true;
            _logger.LogInformation("Macro recording started: {Name}", name);
        }

        public Macro StopRecording()
        {
            if (!IsRecording || CurrentRecording == null)
            {
                throw new InvalidOperationException("Not currently recording");
            }

            IsRecording = false;
            var macro = CurrentRecording;
            CurrentRecording = null;
            _logger.LogInformation("Macro recording stopped: {Name}, events: {Count}", macro.Name, macro.Events.Count);
            return macro;
        }

        public async Task SaveAsync(Macro macro)
        {
            var file = Path.Combine(_storageDir, Sanitize(macro.Name) + ".json");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(macro, options);
            await File.WriteAllTextAsync(file, json);
            _logger.LogInformation("Saved macro '{Name}' to {Path}", macro.Name, file);
        }

        public async Task<Macro?> LoadAsync(string name)
        {
            var file = Path.Combine(_storageDir, Sanitize(name) + ".json");
            if (!File.Exists(file)) return null;
            var json = await File.ReadAllTextAsync(file);
            var macro = JsonSerializer.Deserialize<Macro>(json);
            return macro;
        }

        public Task<IReadOnlyList<string>> ListAsync()
        {
            var files = Directory.Exists(_storageDir)
                ? Directory.GetFiles(_storageDir, "*.json").Select(Path.GetFileNameWithoutExtension).ToList()
                : new List<string>();
            return Task.FromResult((IReadOnlyList<string>)files);
        }

        public async Task PlayAsync(Macro macro)
        {
            if (macro == null || macro.Events.Count == 0)
            {
                _logger.LogWarning("PlayAsync: No events to play");
                return;
            }

            _logger.LogInformation("Playback started for macro '{Name}' with {Count} events", macro.Name, macro.Events.Count);
            try
            {
                // Ensure events are ordered by timestamp
                var ordered = macro.Events.OrderBy(e => e.TimestampMs).ToList();

                long prevTs = ordered[0].TimestampMs;
                int lastX = ordered[0].X ?? 0;
                int lastY = ordered[0].Y ?? 0;

                foreach (var ev in ordered)
                {
                    var delayMs = (int)Math.Max(0, ev.TimestampMs - prevTs);
                    if (delayMs > 0)
                        await Task.Delay(delayMs);

                    switch (ev.Device)
                    {
                        case MacroEventDevice.Keyboard:
                            if (ev.VirtualKeyCode.HasValue)
                            {
                                // Best-effort modifiers support: press before keydown, release after
                                if (ev.Type == MacroEventType.KeyDown)
                                {
                                    TrySendModifier(Keys.ControlKey, ev.Ctrl, true);
                                    TrySendModifier(Keys.Menu, ev.Alt, true); // Alt
                                    TrySendModifier(Keys.ShiftKey, ev.Shift, true);
                                    SendKey((ushort)ev.VirtualKeyCode.Value, true);
                                }
                                else if (ev.Type == MacroEventType.KeyUp)
                                {
                                    SendKey((ushort)ev.VirtualKeyCode.Value, false);
                                    TrySendModifier(Keys.ShiftKey, ev.Shift, false);
                                    TrySendModifier(Keys.Menu, ev.Alt, false);
                                    TrySendModifier(Keys.ControlKey, ev.Ctrl, false);
                                }
                            }
                            break;

                        case MacroEventDevice.Mouse:
                            switch (ev.Type)
                            {
                                case MacroEventType.MouseMove:
                                {
                                    if (ev.X.HasValue && ev.Y.HasValue)
                                    {
                                        int dx = ev.X.Value - lastX;
                                        int dy = ev.Y.Value - lastY;
                                        SendMouseMove(dx, dy);
                                        lastX = ev.X.Value;
                                        lastY = ev.Y.Value;
                                    }
                                    break;
                                }
                                case MacroEventType.MouseDown:
                                case MacroEventType.MouseUp:
                                {
                                    if (ev.Button.HasValue)
                                    {
                                        bool down = ev.Type == MacroEventType.MouseDown;
                                        SendMouseButton(ev.Button.Value, down);
                                    }
                                    break;
                                }
                                case MacroEventType.MouseWheel:
                                {
                                    int delta = ev.WheelDelta ?? 0;
                                    if (delta != 0)
                                        SendMouseWheel(delta);
                                    break;
                                }
                            }
                            break;
                    }

                    prevTs = ev.TimestampMs;
                }

                _logger.LogInformation("Playback finished for macro '{Name}'", macro.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playback error for macro '{Name}'", macro.Name);
            }
        }

        private void SendKey(ushort vk, bool down)
        {
            var input = new SimBlock.Infrastructure.Windows.NativeMethods.INPUT
            {
                type = SimBlock.Infrastructure.Windows.NativeMethods.INPUT_KEYBOARD,
                U = new SimBlock.Infrastructure.Windows.NativeMethods.InputUnion
                {
                    ki = new SimBlock.Infrastructure.Windows.NativeMethods.KEYBDINPUT
                    {
                        wVk = vk,
                        wScan = 0,
                        dwFlags = down ? 0u : SimBlock.Infrastructure.Windows.NativeMethods.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SimBlock.Infrastructure.Windows.NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<SimBlock.Infrastructure.Windows.NativeMethods.INPUT>());
        }

        private void TrySendModifier(System.Windows.Forms.Keys key, bool shouldPress, bool down)
        {
            if (!shouldPress) return;
            SendKey((ushort)key, down);
        }

        private void SendMouseMove(int dx, int dy)
        {
            var input = new SimBlock.Infrastructure.Windows.NativeMethods.INPUT
            {
                type = SimBlock.Infrastructure.Windows.NativeMethods.INPUT_MOUSE,
                U = new SimBlock.Infrastructure.Windows.NativeMethods.InputUnion
                {
                    mi = new SimBlock.Infrastructure.Windows.NativeMethods.MOUSEINPUT
                    {
                        dx = dx,
                        dy = dy,
                        mouseData = 0,
                        dwFlags = SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_MOVE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SimBlock.Infrastructure.Windows.NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<SimBlock.Infrastructure.Windows.NativeMethods.INPUT>());
        }

        private void SendMouseButton(int button, bool down)
        {
            uint flag = 0u;
            switch (button)
            {
                case 0: flag = down ? SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_LEFTDOWN : SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_LEFTUP; break;
                case 1: flag = down ? SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_RIGHTDOWN : SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_RIGHTUP; break;
                case 2: flag = down ? SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_MIDDLEDOWN : SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_MIDDLEUP; break;
                default: return; // ignore X buttons for now
            }

            var input = new SimBlock.Infrastructure.Windows.NativeMethods.INPUT
            {
                type = SimBlock.Infrastructure.Windows.NativeMethods.INPUT_MOUSE,
                U = new SimBlock.Infrastructure.Windows.NativeMethods.InputUnion
                {
                    mi = new SimBlock.Infrastructure.Windows.NativeMethods.MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = flag,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SimBlock.Infrastructure.Windows.NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<SimBlock.Infrastructure.Windows.NativeMethods.INPUT>());
        }

        private void SendMouseWheel(int delta)
        {
            var input = new SimBlock.Infrastructure.Windows.NativeMethods.INPUT
            {
                type = SimBlock.Infrastructure.Windows.NativeMethods.INPUT_MOUSE,
                U = new SimBlock.Infrastructure.Windows.NativeMethods.InputUnion
                {
                    mi = new SimBlock.Infrastructure.Windows.NativeMethods.MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = (uint)delta,
                        dwFlags = SimBlock.Infrastructure.Windows.NativeMethods.MOUSEEVENTF_WHEEL,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SimBlock.Infrastructure.Windows.NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<SimBlock.Infrastructure.Windows.NativeMethods.INPUT>());
        }

        private void OnKeyEvent(object? sender, KeyboardHookEventArgs e)
        {
            if (!IsRecording || CurrentRecording == null) return;
            try
            {
                var ts = (long)(e.Timestamp - _recordingStartUtc).TotalMilliseconds;
                CurrentRecording.Events.Add(new MacroEvent
                {
                    Device = MacroEventDevice.Keyboard,
                    Type = e.IsKeyDown ? MacroEventType.KeyDown : (e.IsKeyUp ? MacroEventType.KeyUp : MacroEventType.KeyDown),
                    TimestampMs = ts,
                    VirtualKeyCode = (int)e.VkCode,
                    Ctrl = e.Ctrl,
                    Alt = e.Alt,
                    Shift = e.Shift
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error recording keyboard event");
            }
        }

        private void OnMouseEvent(object? sender, MouseHookEventArgs e)
        {
            if (!IsRecording || CurrentRecording == null) return;
            try
            {
                var ts = (long)(e.Timestamp - _recordingStartUtc).TotalMilliseconds;
                var type = e.Message switch
                {
                    var m when m == SimBlock.Infrastructure.Windows.NativeMethods.WM_MOUSEMOVE => MacroEventType.MouseMove,
                    var m when m == SimBlock.Infrastructure.Windows.NativeMethods.WM_LBUTTONDOWN || m == SimBlock.Infrastructure.Windows.NativeMethods.WM_RBUTTONDOWN || m == SimBlock.Infrastructure.Windows.NativeMethods.WM_MBUTTONDOWN => MacroEventType.MouseDown,
                    var m when m == SimBlock.Infrastructure.Windows.NativeMethods.WM_LBUTTONUP || m == SimBlock.Infrastructure.Windows.NativeMethods.WM_RBUTTONUP || m == SimBlock.Infrastructure.Windows.NativeMethods.WM_MBUTTONUP => MacroEventType.MouseUp,
                    var m when m == SimBlock.Infrastructure.Windows.NativeMethods.WM_MOUSEWHEEL || m == SimBlock.Infrastructure.Windows.NativeMethods.WM_MOUSEHWHEEL => MacroEventType.MouseWheel,
                    _ => MacroEventType.MouseMove
                };

                int? button = null;
                if (e.LeftButton) button = 0;
                else if (e.RightButton) button = 1;
                else if (e.MiddleButton) button = 2;

                CurrentRecording.Events.Add(new MacroEvent
                {
                    Device = MacroEventDevice.Mouse,
                    Type = type,
                    TimestampMs = ts,
                    X = e.X,
                    Y = e.Y,
                    Button = button,
                    WheelDelta = e.WheelDelta
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error recording mouse event");
            }
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _keyboardHookService.KeyEvent -= OnKeyEvent;
                _mouseHookService.MouseEvent -= OnMouseEvent;
            }
            catch { /* ignore */ }
        }
    }
}
