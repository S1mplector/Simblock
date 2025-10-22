using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Interfaces;

namespace SimBlock.Tests.Macros
{
    internal sealed class FakeKeyboardHookService : IKeyboardHookService
    {
        public event EventHandler<KeyboardBlockState>? BlockStateChanged;
        public event EventHandler<int>? EmergencyUnlockAttempt;
        public event EventHandler<KeyboardHookEventArgs>? KeyEvent;

        public bool IsHookInstalled { get; private set; }
        public KeyboardBlockState CurrentState { get; } = new KeyboardBlockState();

        public int SetBlockingCalls { get; private set; }
        public bool? LastBlockingValue { get; private set; }

        public Task InstallHookAsync()
        {
            IsHookInstalled = true; return Task.CompletedTask;
        }
        public Task UninstallHookAsync()
        {
            IsHookInstalled = false; return Task.CompletedTask;
        }
        public Task SetBlockingAsync(bool shouldBlock, string? reason = null)
        {
            SetBlockingCalls++;
            LastBlockingValue = shouldBlock;
            CurrentState.SetBlocked(shouldBlock, reason);
            BlockStateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }
        public Task ToggleBlockingAsync(string? reason = null)
        {
            CurrentState.Toggle(reason);
            BlockStateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }
        public Task SetSimpleModeAsync(string? reason = null)
        {
            CurrentState.SetSimpleMode(reason); return Task.CompletedTask;
        }
        public Task SetAdvancedModeAsync(AdvancedKeyboardConfiguration config, string? reason = null)
        {
            CurrentState.SetAdvancedMode(config, reason); return Task.CompletedTask;
        }
        public Task SetSelectModeAsync(AdvancedKeyboardConfiguration config, string? reason = null)
        {
            CurrentState.SetSelectMode(config, reason); return Task.CompletedTask;
        }

        public void FireKey(KeyboardHookEventArgs e) => KeyEvent?.Invoke(this, e);
    }

    internal sealed class FakeMouseHookService : IMouseHookService
    {
        public event EventHandler<MouseBlockState>? BlockStateChanged;
        public event EventHandler<int>? EmergencyUnlockAttempt;
        public event EventHandler<MouseHookEventArgs>? MouseEvent;

        public bool IsHookInstalled { get; private set; }
        public MouseBlockState CurrentState { get; } = new MouseBlockState();

        public int SetBlockingCalls { get; private set; }
        public bool? LastBlockingValue { get; private set; }

        public Task InstallHookAsync()
        { IsHookInstalled = true; return Task.CompletedTask; }
        public Task UninstallHookAsync()
        { IsHookInstalled = false; return Task.CompletedTask; }
        public Task SetBlockingAsync(bool shouldBlock, string? reason = null)
        {
            SetBlockingCalls++;
            LastBlockingValue = shouldBlock;
            CurrentState.SetBlocked(shouldBlock, reason);
            BlockStateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }
        public Task ToggleBlockingAsync(string? reason = null)
        {
            CurrentState.Toggle(reason);
            BlockStateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }
        public Task SetSimpleModeAsync(string? reason = null)
        { CurrentState.SetSimpleMode(reason); return Task.CompletedTask; }
        public Task SetAdvancedModeAsync(AdvancedMouseConfiguration config, string? reason = null)
        { CurrentState.SetAdvancedMode(config, reason); return Task.CompletedTask; }
        public Task SetSelectModeAsync(AdvancedMouseConfiguration config, string? reason = null)
        { CurrentState.SetSelectMode(config, reason); return Task.CompletedTask; }

        public void FireMouse(MouseHookEventArgs e) => MouseEvent?.Invoke(this, e);
    }

    internal sealed class FakeMacroServiceForMapping : IMacroService
    {
        private readonly Dictionary<string, Macro> _store = new();
        public bool IsRecording { get; set; }
        public bool IsPlaying { get; set; }
        public Macro? CurrentRecording { get; private set; }

        public int PlayCalls { get; private set; }
        public string? LastPlayedName { get; private set; }

        public void StartRecording(string name, MacroRecordingDevices devices = MacroRecordingDevices.Both)
        { CurrentRecording = new Macro { Name = name }; }
        public Macro StopRecording()
        { var m = CurrentRecording ?? new Macro(); CurrentRecording = null; return m; }

        public Task SaveAsync(Macro macro)
        { _store[macro.Name] = macro; return Task.CompletedTask; }

        public Task<Macro?> LoadAsync(string name)
        { _store.TryGetValue(name, out var m); return Task.FromResult<Macro?>(m); }

        public Task<IReadOnlyList<string>> ListAsync()
        { return Task.FromResult((IReadOnlyList<string>)new List<string>(_store.Keys)); }
        public Task<IReadOnlyList<MacroInfo>> ListInfoAsync()
        { return Task.FromResult((IReadOnlyList<MacroInfo>)new List<MacroInfo>()); }

        public Task<bool> DeleteAsync(string name)
        { var r = _store.Remove(name); return Task.FromResult(r); }
        public Task<bool> RenameAsync(string oldName, string newName)
        {
            if (!_store.TryGetValue(oldName, out var m)) return Task.FromResult(false);
            _store.Remove(oldName); m.Name = newName; _store[newName] = m; return Task.FromResult(true);
        }
        public Task<bool> ExistsAsync(string name) => Task.FromResult(_store.ContainsKey(name));
        public bool ValidateName(string name, out string? errorMessage)
        { errorMessage = null; return !string.IsNullOrWhiteSpace(name); }

        public Task<bool> ImportAsync(string filePath, bool overwrite = false) => Task.FromResult(false);
        public Task<bool> ExportAsync(string name, string destinationPath, bool overwrite = false) => Task.FromResult(false);

        public Task PlayAsync(Macro macro)
        { PlayCalls++; LastPlayedName = macro.Name; return Task.CompletedTask; }
        public Task PlayAsync(Macro macro, System.Threading.CancellationToken cancellationToken, double speed = 1.0, int loops = 1)
        { PlayCalls++; LastPlayedName = macro.Name; return Task.CompletedTask; }
    }
}
