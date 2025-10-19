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

namespace SimBlock.Core.Application.Services
{
    public sealed class MacroMappingService : IMacroMappingService, IDisposable
    {
        private readonly ILogger<MacroMappingService> _logger;
        private readonly IMacroService _macroService;
        private readonly IKeyboardHookService _keyboardHookService;
        private readonly IMouseHookService _mouseHookService;
        private readonly string _storageFile;
        private readonly object _sync = new object();

        private List<MacroBinding> _bindings = new List<MacroBinding>();
        private bool _enabled = true;
        private bool _disposed;
        private readonly Dictionary<string, DateTime> _lastFire = new();
        private const int TriggerDebounceMs = 200;

        public event EventHandler? BindingsChanged;

        public bool Enabled => _enabled;

        public MacroMappingService(
            ILogger<MacroMappingService> logger,
            IMacroService macroService,
            IKeyboardHookService keyboardHookService,
            IMouseHookService mouseHookService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _macroService = macroService ?? throw new ArgumentNullException(nameof(macroService));
            _keyboardHookService = keyboardHookService ?? throw new ArgumentNullException(nameof(keyboardHookService));
            _mouseHookService = mouseHookService ?? throw new ArgumentNullException(nameof(mouseHookService));

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "SimBlock");
            Directory.CreateDirectory(dir);
            _storageFile = Path.Combine(dir, "MacroBindings.json");

            LoadFromDisk();

            _keyboardHookService.KeyEvent += OnKeyEvent;
            _mouseHookService.MouseEvent += OnMouseEvent;
        }

        public void SetEnabled(bool enabled)
        {
            lock (_sync)
            {
                _enabled = enabled;
                SaveToDisk();
            }
        }

        public Task<IReadOnlyList<MacroBinding>> ListBindingsAsync()
        {
            lock (_sync)
            {
                return Task.FromResult((IReadOnlyList<MacroBinding>)_bindings.Select(Clone).ToList());
            }
        }

        public Task<bool> AddOrUpdateBindingAsync(MacroBinding binding)
        {
            if (binding == null) return Task.FromResult(false);
            if (binding.Trigger == null) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(binding.MacroName)) return Task.FromResult(false);

            lock (_sync)
            {
                // If an existing binding with same trigger exists, update macro name and enabled state
                var existing = _bindings.FirstOrDefault(b => TriggersEqual(b.Trigger, binding.Trigger));
                if (existing != null)
                {
                    existing.MacroName = binding.MacroName.Trim();
                    existing.Enabled = binding.Enabled;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(binding.Id))
                        binding.Id = Guid.NewGuid().ToString("N");
                    _bindings.Add(Clone(binding));
                }

                SaveToDisk();
            }

            BindingsChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        public Task<bool> RemoveBindingAsync(string bindingId)
        {
            if (string.IsNullOrWhiteSpace(bindingId)) return Task.FromResult(false);
            lock (_sync)
            {
                var removed = _bindings.RemoveAll(b => string.Equals(b.Id, bindingId, StringComparison.OrdinalIgnoreCase)) > 0;
                if (removed)
                {
                    SaveToDisk();
                }
                if (removed) BindingsChanged?.Invoke(this, EventArgs.Empty);
                return Task.FromResult(removed);
            }
        }

        public Task<bool> EnableBindingAsync(string bindingId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(bindingId)) return Task.FromResult(false);
            lock (_sync)
            {
                var b = _bindings.FirstOrDefault(x => string.Equals(x.Id, bindingId, StringComparison.OrdinalIgnoreCase));
                if (b == null) return Task.FromResult(false);
                b.Enabled = enabled;
                SaveToDisk();
            }
            BindingsChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        public Task<bool> RemoveBindingsForMacroAsync(string macroName)
        {
            if (string.IsNullOrWhiteSpace(macroName)) return Task.FromResult(false);
            lock (_sync)
            {
                var removed = _bindings.RemoveAll(b => string.Equals(b.MacroName, macroName, StringComparison.OrdinalIgnoreCase)) > 0;
                if (removed)
                {
                    SaveToDisk();
                    BindingsChanged?.Invoke(this, EventArgs.Empty);
                }
                return Task.FromResult(removed);
            }
        }

        public Task<bool> ClearAllBindingsAsync()
        {
            lock (_sync)
            {
                _bindings.Clear();
                SaveToDisk();
            }
            BindingsChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(true);
        }

        private void OnKeyEvent(object? sender, KeyboardHookEventArgs e)
        {
            try
            {
                if (!_enabled) return;
                if (_macroService.IsRecording || _macroService.IsPlaying) return;

                MacroBinding? match = null;
                lock (_sync)
                {
                    foreach (var b in _bindings)
                    {
                        if (!b.Enabled) continue;
                        if (b.Trigger.Device != MacroTriggerDevice.Keyboard) continue;
                        if (!b.Trigger.VirtualKeyCode.HasValue) continue;
                        if (b.Trigger.VirtualKeyCode.Value != (int)e.VkCode) continue;
                        if (b.Trigger.Ctrl != e.Ctrl) continue;
                        if (b.Trigger.Alt != e.Alt) continue;
                        if (b.Trigger.Shift != e.Shift) continue;
                        var isDown = e.IsKeyDown;
                        var isUp = e.IsKeyUp;
                        var wantDown = b.Trigger.OnKeyDown;
                        if ((wantDown && isDown) || (!wantDown && isUp))
                        {
                            match = b;
                            break;
                        }
                    }
                }

                if (match != null && !Debounced(match.Id))
                {
                    _ = TriggerAsync(match);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "MacroMapping: error processing key event");
            }
        }

        private void OnMouseEvent(object? sender, MouseHookEventArgs e)
        {
            try
            {
                if (!_enabled) return;
                if (_macroService.IsRecording || _macroService.IsPlaying) return;

                MacroBinding? match = null;
                lock (_sync)
                {
                    foreach (var b in _bindings)
                    {
                        if (!b.Enabled) continue;
                        if (b.Trigger.Device != MacroTriggerDevice.Mouse) continue;
                        if (!b.Trigger.Button.HasValue) continue;

                        bool isDown = e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_LBUTTONDOWN ||
                                      e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_RBUTTONDOWN ||
                                      e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_MBUTTONDOWN;
                        bool isUp = e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_LBUTTONUP ||
                                    e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_RBUTTONUP ||
                                    e.Message == SimBlock.Infrastructure.Windows.NativeMethods.WM_MBUTTONUP;

                        int? button = null;
                        if (e.LeftButton) button = 0;
                        else if (e.RightButton) button = 1;
                        else if (e.MiddleButton) button = 2;

                        if (!button.HasValue) continue;
                        if (b.Trigger.Button.Value != button.Value) continue;
                        bool wantDown = b.Trigger.OnButtonDown;
                        if ((wantDown && isDown) || (!wantDown && isUp))
                        {
                            match = b;
                            break;
                        }
                    }
                }

                if (match != null && !Debounced(match.Id))
                {
                    _ = TriggerAsync(match);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "MacroMapping: error processing mouse event");
            }
        }

        private bool Debounced(string bindingId)
        {
            lock (_sync)
            {
                var now = DateTime.UtcNow;
                if (_lastFire.TryGetValue(bindingId, out var last))
                {
                    if ((now - last).TotalMilliseconds < TriggerDebounceMs)
                        return true;
                }
                _lastFire[bindingId] = now;
                return false;
            }
        }

        private async Task TriggerAsync(MacroBinding binding)
        {
            try
            {
                var macro = await _macroService.LoadAsync(binding.MacroName);
                if (macro == null)
                {
                    _logger.LogWarning("MacroMapping: macro '{Name}' not found for binding {Id}", binding.MacroName, binding.Id);
                    return;
                }
                _logger.LogInformation("MacroMapping: triggering macro '{Name}' via binding {Id}", binding.MacroName, binding.Id);
                await _macroService.PlayAsync(macro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MacroMapping: error triggering macro '{Name}'", binding.MacroName);
            }
        }

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_storageFile)) return;
                var json = File.ReadAllText(_storageFile);
                var data = JsonSerializer.Deserialize<BindingsFile>(json);
                if (data != null)
                {
                    _enabled = data.Enabled;
                    _bindings = data.Bindings ?? new List<MacroBinding>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MacroMapping: failed to load bindings, starting empty");
                _bindings = new List<MacroBinding>();
            }
        }

        private void SaveToDisk()
        {
            try
            {
                var data = new BindingsFile
                {
                    Enabled = _enabled,
                    Bindings = _bindings
                };
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_storageFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MacroMapping: failed to save bindings");
            }
        }

        private static bool TriggersEqual(MacroTrigger a, MacroTrigger b)
        {
            if (a.Device != b.Device) return false;
            if (a.Device == MacroTriggerDevice.Keyboard)
            {
                return a.VirtualKeyCode == b.VirtualKeyCode && a.Ctrl == b.Ctrl && a.Alt == b.Alt && a.Shift == b.Shift && a.OnKeyDown == b.OnKeyDown;
            }
            else
            {
                return a.Button == b.Button && a.OnButtonDown == b.OnButtonDown;
            }
        }

        private static MacroBinding Clone(MacroBinding b)
        {
            return new MacroBinding
            {
                Id = b.Id,
                MacroName = b.MacroName,
                Enabled = b.Enabled,
                Trigger = new MacroTrigger
                {
                    Device = b.Trigger.Device,
                    VirtualKeyCode = b.Trigger.VirtualKeyCode,
                    Ctrl = b.Trigger.Ctrl,
                    Alt = b.Trigger.Alt,
                    Shift = b.Trigger.Shift,
                    OnKeyDown = b.Trigger.OnKeyDown,
                    Button = b.Trigger.Button,
                    OnButtonDown = b.Trigger.OnButtonDown
                }
            };
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
            catch { }
        }

        private sealed class BindingsFile
        {
            public bool Enabled { get; set; } = true;
            public List<MacroBinding>? Bindings { get; set; }
        }
    }
}
