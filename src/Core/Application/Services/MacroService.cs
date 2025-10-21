using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
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
        private volatile bool _isPlaying;
        private MacroRecordingDevices _recordDevices;

        public bool IsRecording { get; private set; }
        public Macro? CurrentRecording { get; private set; }
        public bool IsPlaying => _isPlaying;

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

        public void StartRecording(string name, MacroRecordingDevices devices = MacroRecordingDevices.Both)
        {
            if (IsRecording)
            {
                _logger.LogWarning("StartRecording called while already recording");
                return;
            }

            CurrentRecording = new Macro { Name = name };
            _recordingStartUtc = DateTime.UtcNow;
            _recordDevices = devices;
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
            await PlayAsync(macro, CancellationToken.None, 1.0, 1);
        }

        public async Task PlayAsync(Macro macro, CancellationToken cancellationToken, double speed = 1.0, int loops = 1)
        {
            if (macro == null || macro.Events.Count == 0)
            {
                _logger.LogWarning("PlayAsync: No events to play");
                return;
            }

            if (_isPlaying)
            {
                _logger.LogWarning("Playback requested while another playback is running");
                return;
            }

            if (speed <= 0) speed = 1.0;
            if (loops < 1) loops = 1;

            _isPlaying = true;
            _logger.LogInformation("Playback started for macro '{Name}' with {Count} events (speed={Speed}x, loops={Loops})", macro.Name, macro.Events.Count, speed, loops);
            bool keyboardWasBlocked = false;
            bool mouseWasBlocked = false;
            try
            {
                // Suspend blocking while playing, remember prior states
                try
                {
                    keyboardWasBlocked = _keyboardHookService.CurrentState?.IsBlocked ?? false;
                    mouseWasBlocked = _mouseHookService.CurrentState?.IsBlocked ?? false;
                    if (keyboardWasBlocked)
                        await _keyboardHookService.SetBlockingAsync(false, "Macro playback");
                    if (mouseWasBlocked)
                        await _mouseHookService.SetBlockingAsync(false, "Macro playback");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to temporarily disable blocking before playback");
                }

                // Ensure events are ordered by timestamp
                var ordered = macro.Events.OrderBy(e => e.TimestampMs).ToList();

                for (int loop = 0; loop < loops && !cancellationToken.IsCancellationRequested; loop++)
                {
                    long prevTs = ordered[0].TimestampMs;
                    int lastX = ordered[0].X ?? 0;
                    int lastY = ordered[0].Y ?? 0;

                    foreach (var ev in ordered)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var rawDelay = Math.Max(0, ev.TimestampMs - prevTs);
                        var delayMs = (int)Math.Max(0, rawDelay / speed);
                        if (delayMs > 0)
                            await Task.Delay(delayMs, cancellationToken).ContinueWith(_ => { });

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
                }

                _logger.LogInformation("Playback finished for macro '{Name}'", macro.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playback error for macro '{Name}'", macro.Name);
            }
            finally
            {
                try
                {
                    if (keyboardWasBlocked)
                        await _keyboardHookService.SetBlockingAsync(true, "Restore after macro");
                }
                catch { }
                try
                {
                    if (mouseWasBlocked)
                        await _mouseHookService.SetBlockingAsync(true, "Restore after macro");
                }
                catch { }
                _isPlaying = false;
            }
        }

        public async Task<IReadOnlyList<MacroInfo>> ListInfoAsync()
        {
            var list = new List<MacroInfo>();
            if (!Directory.Exists(_storageDir)) return list;

            foreach (var file in Directory.GetFiles(_storageDir, "*.json"))
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(file)!;
                    var info = new FileInfo(file);
                    int eventCount = 0;
                    long? duration = null;

                    // Try to read basic stats
                    var json = await File.ReadAllTextAsync(file);
                    var macro = JsonSerializer.Deserialize<Macro>(json);
                    if (macro != null && macro.Events.Count > 0)
                    {
                        eventCount = macro.Events.Count;
                        var ordered = macro.Events.OrderBy(e => e.TimestampMs).ToList();
                        duration = Math.Max(0, ordered.Last().TimestampMs - ordered.First().TimestampMs);
                    }

                    list.Add(new MacroInfo
                    {
                        Name = name,
                        CreatedAtUtc = macro?.CreatedAtUtc ?? info.CreationTimeUtc,
                        LastModifiedUtc = info.LastWriteTimeUtc,
                        EventCount = eventCount,
                        DurationMs = duration
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error reading macro info for {File}", file);
                }
            }

            return list;
        }

        public Task<bool> ExistsAsync(string name)
        {
            var file = Path.Combine(_storageDir, Sanitize(name) + ".json");
            return Task.FromResult(File.Exists(file));
        }

        public bool ValidateName(string name, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Name cannot be empty.";
                return false;
            }
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                errorMessage = "Name contains invalid characters.";
                return false;
            }
            if (name.Length > 128)
            {
                errorMessage = "Name is too long.";
                return false;
            }
            return true;
        }

        public async Task<bool> DeleteAsync(string name)
        {
            try
            {
                var file = Path.Combine(_storageDir, Sanitize(name) + ".json");
                if (!File.Exists(file)) return false;
                File.Delete(file);
                await Task.CompletedTask;
                _logger.LogInformation("Deleted macro '{Name}'", name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete macro '{Name}'", name);
                return false;
            }
        }

        public async Task<bool> RenameAsync(string oldName, string newName)
        {
            try
            {
                var src = Path.Combine(_storageDir, Sanitize(oldName) + ".json");
                if (!File.Exists(src)) return false;
                if (!ValidateName(newName, out _)) return false;
                var dst = Path.Combine(_storageDir, Sanitize(newName) + ".json");
                if (!src.Equals(dst, StringComparison.OrdinalIgnoreCase) && File.Exists(dst)) return false;
                File.Move(src, dst, overwrite: true);
                await Task.CompletedTask;
                _logger.LogInformation("Renamed macro '{Old}' to '{New}'", oldName, newName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rename macro '{Old}' to '{New}'", oldName, newName);
                return false;
            }
        }

        public async Task<bool> ImportAsync(string filePath, bool overwrite = false)
        {
            try
            {
                if (!File.Exists(filePath)) return false;
                var json = await File.ReadAllTextAsync(filePath);
                var macro = JsonSerializer.Deserialize<Macro>(json);
                if (macro == null || string.IsNullOrWhiteSpace(macro.Name)) return false;
                var target = Path.Combine(_storageDir, Sanitize(macro.Name) + ".json");
                if (!overwrite && File.Exists(target)) return false;
                Directory.CreateDirectory(_storageDir);
                await File.WriteAllTextAsync(target, JsonSerializer.Serialize(macro, new JsonSerializerOptions { WriteIndented = true }));
                _logger.LogInformation("Imported macro '{Name}' from {File}", macro.Name, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import macro from {File}", filePath);
                return false;
            }
        }

        public async Task<bool> ExportAsync(string name, string destinationPath, bool overwrite = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(destinationPath)) return false;
                var src = Path.Combine(_storageDir, Sanitize(name) + ".json");
                if (!File.Exists(src)) return false;
                var dir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                if (File.Exists(destinationPath) && !overwrite) return false;
                File.Copy(src, destinationPath, overwrite: true);
                await Task.CompletedTask;
                _logger.LogInformation("Exported macro '{Name}' to {Dest}", name, destinationPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export macro '{Name}' to {Dest}", name, destinationPath);
                return false;
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
            if ((_recordDevices & MacroRecordingDevices.Keyboard) == 0) return;
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
            if ((_recordDevices & MacroRecordingDevices.Mouse) == 0) return;
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
