using Microsoft.Extensions.Logging;
using SimBlock.Core.Application.Interfaces;
using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using SimBlock.Core.Domain.Events;
using System.Collections.Concurrent;

namespace SimBlock.Core.Application.Services
{
    /// <summary>
    /// Main service for macro management and execution
    /// </summary>
    public class MacroService : IMacroService
    {
        private readonly IInputRecorder _inputRecorder;
        private readonly IInputPlayer _inputPlayer;
        private readonly IMacroStorage _storage;
        private readonly ILogger<MacroService> _logger;

        private readonly ConcurrentDictionary<string, Guid> _registeredHotkeys = new();
        private readonly Dictionary<string, object> _serviceStats = new();

        private Macro? _currentRecordingMacro;
        private Macro? _currentPlayingMacro;
        private bool _isInitialized;

        #region Events

        public event EventHandler<MacroRecordingStartedEvent>? RecordingStarted;
        public event EventHandler<MacroRecordingStoppedEvent>? RecordingStopped;
        public event EventHandler<MacroEventRecordedEvent>? EventRecorded;
        public event EventHandler<MacroPlaybackStartedEvent>? PlaybackStarted;
        public event EventHandler<MacroPlaybackStoppedEvent>? PlaybackStopped;
        public event EventHandler<MacroPlaybackPausedEvent>? PlaybackPaused;
        public event EventHandler<MacroPlaybackResumedEvent>? PlaybackResumed;
        public event EventHandler<MacroCollectionChangedEvent>? MacroCollectionChanged;
        public event EventHandler<MacroHotkeyTriggeredEvent>? HotkeyTriggered;

        #endregion

        #region Properties

        public bool IsRecording => _inputRecorder.IsRecording;
        public bool IsPlaying => _inputPlayer.IsPlaying;
        public Macro? CurrentRecordingMacro => _currentRecordingMacro;
        public Macro? CurrentPlayingMacro => _currentPlayingMacro;
        public bool IsInitialized => _isInitialized;

        #endregion

        public MacroService(
            IInputRecorder inputRecorder,
            IInputPlayer inputPlayer,
            IMacroStorage storage,
            ILogger<MacroService> logger)
        {
            _inputRecorder = inputRecorder ?? throw new ArgumentNullException(nameof(inputRecorder));
            _inputPlayer = inputPlayer ?? throw new ArgumentNullException(nameof(inputPlayer));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SubscribeToEvents();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            try
            {
                _logger.LogInformation("Initializing MacroService...");

                _serviceStats["StartTime"] = DateTime.UtcNow;
                _serviceStats["TotalMacrosExecuted"] = 0;
                _serviceStats["TotalRecordingSessions"] = 0;
                _serviceStats["TotalPlaybackSessions"] = 0;

                var macros = await _storage.GetAllMacrosAsync();
                foreach (var macro in macros.Where(m => m.TriggerType == MacroTriggerType.Hotkey && !string.IsNullOrEmpty(m.HotkeyCombo)))
                {
                    _registeredHotkeys.TryAdd(macro.HotkeyCombo!, macro.Id);
                }

                _isInitialized = true;
                _logger.LogInformation("MacroService initialized successfully. Loaded {MacroCount} macros.", macros.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MacroService");
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            if (!_isInitialized)
                return;

            try
            {
                _logger.LogInformation("Shutting down MacroService...");

                if (IsRecording)
                    await CancelRecordingAsync();

                if (IsPlaying)
                    await StopPlaybackAsync();

                _registeredHotkeys.Clear();
                _isInitialized = false;
                _logger.LogInformation("MacroService shut down successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MacroService shutdown");
            }
        }

        public async Task<Macro> StartRecordingAsync(string macroName, string? description = null, string? category = null)
        {
            ThrowIfNotInitialized();

            if (IsRecording)
                throw new InvalidOperationException("Recording is already in progress");

            if (IsPlaying)
                throw new InvalidOperationException("Cannot start recording while macro playback is active");

            try
            {
                _logger.LogInformation("Starting macro recording: {MacroName}", macroName);

                _currentRecordingMacro = Macro.Create(macroName, description, category);
                _currentRecordingMacro.SetStatus(MacroStatus.Recording);

                await _inputRecorder.StartRecordingAsync();

                var recordingStartedEvent = new MacroRecordingStartedEvent(_currentRecordingMacro);
                RecordingStarted?.Invoke(this, recordingStartedEvent);

                UpdateServiceStats("TotalRecordingSessions", (int)_serviceStats["TotalRecordingSessions"] + 1);

                return _currentRecordingMacro;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start macro recording");
                _currentRecordingMacro = null;
                throw;
            }
        }

        public async Task<Macro?> StopRecordingAsync()
        {
            ThrowIfNotInitialized();

            if (!IsRecording || _currentRecordingMacro == null)
                return null;

            try
            {
                _logger.LogInformation("Stopping macro recording: {MacroName}", _currentRecordingMacro.Name);

                var recordedEvents = await _inputRecorder.StopRecordingAsync();
                
                foreach (var macroEvent in recordedEvents)
                {
                    _currentRecordingMacro.AddEvent(macroEvent);
                }

                _currentRecordingMacro.SortEventsByTimestamp();
                _currentRecordingMacro.SetStatus(MacroStatus.Idle);

                await _storage.SaveMacroAsync(_currentRecordingMacro);

                var recordingStoppedEvent = new MacroRecordingStoppedEvent(
                    _currentRecordingMacro, 
                    _currentRecordingMacro.Events.Count, 
                    _currentRecordingMacro.Duration);
                
                RecordingStopped?.Invoke(this, recordingStoppedEvent);

                var collectionChangedEvent = new MacroCollectionChangedEvent("Added", _currentRecordingMacro.Id, _currentRecordingMacro.Name);
                MacroCollectionChanged?.Invoke(this, collectionChangedEvent);

                var result = _currentRecordingMacro;
                _currentRecordingMacro = null;

                _logger.LogInformation("Macro recording completed: {MacroName} with {EventCount} events", result.Name, result.Events.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop macro recording");
                throw;
            }
        }

        public void PauseRecording()
        {
            ThrowIfNotInitialized();
            _inputRecorder.PauseRecording();
        }

        public void ResumeRecording()
        {
            ThrowIfNotInitialized();
            _inputRecorder.ResumeRecording();
        }

        public async Task CancelRecordingAsync()
        {
            ThrowIfNotInitialized();

            if (!IsRecording || _currentRecordingMacro == null)
                return;

            try
            {
                await _inputRecorder.StopRecordingAsync();
                _currentRecordingMacro = null;
                _logger.LogInformation("Macro recording canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel macro recording");
                throw;
            }
        }

        public void SetRecordingOptions(bool recordKeyboard, bool recordMouse, bool recordMouseMovement, bool recordDelays, TimeSpan minimumDelay)
        {
            ThrowIfNotInitialized();
            _inputRecorder.SetRecordingFilters(recordKeyboard, recordMouse, recordMouseMovement, recordDelays);
            _inputRecorder.SetMinimumDelay(minimumDelay);
        }

        public async Task PlayMacroAsync(Guid macroId, MacroExecutionMode? executionMode = null, int? repeatCount = null)
        {
            var macro = await GetMacroAsync(macroId);
            if (macro == null)
                throw new ArgumentException($"Macro with ID {macroId} not found", nameof(macroId));

            await PlayMacroAsync(macro, executionMode, repeatCount);
        }

        public async Task PlayMacroAsync(Macro macro, MacroExecutionMode? executionMode = null, int? repeatCount = null)
        {
            ThrowIfNotInitialized();

            if (macro == null)
                throw new ArgumentNullException(nameof(macro));

            if (IsPlaying)
                throw new InvalidOperationException("Another macro is already playing");

            if (IsRecording)
                throw new InvalidOperationException("Cannot play macro while recording is active");

            if (!macro.IsEnabled)
                throw new InvalidOperationException("Cannot play disabled macro");

            try
            {
                _logger.LogInformation("Starting macro playback: {MacroName}", macro.Name);

                _currentPlayingMacro = macro;
                macro.SetStatus(MacroStatus.Playing);

                var effectiveExecutionMode = executionMode ?? macro.ExecutionMode;
                var effectiveRepeatCount = repeatCount ?? macro.RepeatCount;

                var playbackStartedEvent = new MacroPlaybackStartedEvent(macro, effectiveExecutionMode, effectiveRepeatCount);
                PlaybackStarted?.Invoke(this, playbackStartedEvent);

                await _inputPlayer.PlayMacroAsync(macro, effectiveExecutionMode, effectiveRepeatCount);

                UpdateServiceStats("TotalMacrosExecuted", (int)_serviceStats["TotalMacrosExecuted"] + 1);
                UpdateServiceStats("TotalPlaybackSessions", (int)_serviceStats["TotalPlaybackSessions"] + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play macro: {MacroName}", macro.Name);
                
                if (_currentPlayingMacro != null)
                {
                    _currentPlayingMacro.SetStatus(MacroStatus.Failed);
                    var playbackStoppedEvent = new MacroPlaybackStoppedEvent(_currentPlayingMacro, false, ex.Message, ex);
                    PlaybackStopped?.Invoke(this, playbackStoppedEvent);
                    _currentPlayingMacro = null;
                }
                
                throw;
            }
        }

        public async Task StopPlaybackAsync()
        {
            ThrowIfNotInitialized();

            if (!IsPlaying || _currentPlayingMacro == null)
                return;

            try
            {
                await _inputPlayer.StopPlaybackAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop macro playback");
                throw;
            }
        }

        public void PausePlayback()
        {
            ThrowIfNotInitialized();
            _inputPlayer.PausePlayback();
            
            if (_currentPlayingMacro != null)
            {
                _currentPlayingMacro.SetStatus(MacroStatus.Paused);
                var pausedEvent = new MacroPlaybackPausedEvent(_currentPlayingMacro, _inputPlayer.CurrentEventIndex);
                PlaybackPaused?.Invoke(this, pausedEvent);
            }
        }

        public void ResumePlayback()
        {
            ThrowIfNotInitialized();
            _inputPlayer.ResumePlayback();
            
            if (_currentPlayingMacro != null)
            {
                _currentPlayingMacro.SetStatus(MacroStatus.Playing);
                var resumedEvent = new MacroPlaybackResumedEvent(_currentPlayingMacro, _inputPlayer.CurrentEventIndex);
                PlaybackResumed?.Invoke(this, resumedEvent);
            }
        }

        public void SetPlaybackOptions(double speedMultiplier, bool respectTiming, TimeSpan? customDelay = null)
        {
            ThrowIfNotInitialized();
            _inputPlayer.SetPlaybackSpeed(speedMultiplier);
            _inputPlayer.SetRespectTiming(respectTiming);
            
            if (customDelay.HasValue)
                _inputPlayer.SetCustomDelay(customDelay.Value);
        }

        // Macro Management Methods
        public async Task<Macro> CreateMacroAsync(string name, string? description = null, string? category = null)
        {
            ThrowIfNotInitialized();

            try
            {
                var macro = Macro.Create(name, description, category);
                await _storage.SaveMacroAsync(macro);

                var collectionChangedEvent = new MacroCollectionChangedEvent("Added", macro.Id, macro.Name);
                MacroCollectionChanged?.Invoke(this, collectionChangedEvent);

                return macro;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create macro: {MacroName}", name);
                throw;
            }
        }

        public async Task<Macro?> GetMacroAsync(Guid id)
        {
            ThrowIfNotInitialized();
            return await _storage.GetMacroByIdAsync(id);
        }

        public async Task<IEnumerable<Macro>> GetAllMacrosAsync()
        {
            ThrowIfNotInitialized();
            return await _storage.GetAllMacrosAsync();
        }

        public async Task UpdateMacroAsync(Macro macro)
        {
            ThrowIfNotInitialized();

            if (macro == null)
                throw new ArgumentNullException(nameof(macro));

            try
            {
                await _storage.SaveMacroAsync(macro);

                var collectionChangedEvent = new MacroCollectionChangedEvent("Updated", macro.Id, macro.Name);
                MacroCollectionChanged?.Invoke(this, collectionChangedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update macro: {MacroName}", macro.Name);
                throw;
            }
        }

        public async Task DeleteMacroAsync(Guid id)
        {
            ThrowIfNotInitialized();

            try
            {
                var macro = await GetMacroAsync(id);
                if (macro == null)
                    return;

                if (macro.TriggerType == MacroTriggerType.Hotkey && !string.IsNullOrEmpty(macro.HotkeyCombo))
                {
                    await UnregisterHotkeyAsync(id);
                }

                await _storage.DeleteMacroAsync(id);

                var collectionChangedEvent = new MacroCollectionChangedEvent("Deleted", id, macro.Name);
                MacroCollectionChanged?.Invoke(this, collectionChangedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete macro with ID: {MacroId}", id);
                throw;
            }
        }

        public async Task<Macro> DuplicateMacroAsync(Guid id, string? newName = null)
        {
            ThrowIfNotInitialized();

            var originalMacro = await GetMacroAsync(id);
            if (originalMacro == null)
                throw new ArgumentException($"Macro with ID {id} not found", nameof(id));

            try
            {
                var duplicatedMacro = originalMacro.Clone(newName);
                await _storage.SaveMacroAsync(duplicatedMacro);

                var collectionChangedEvent = new MacroCollectionChangedEvent("Added", duplicatedMacro.Id, duplicatedMacro.Name);
                MacroCollectionChanged?.Invoke(this, collectionChangedEvent);

                return duplicatedMacro;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to duplicate macro: {MacroName}", originalMacro.Name);
                throw;
            }
        }

        public async Task<IEnumerable<Macro>> GetMacrosByCategoryAsync(string category)
        {
            ThrowIfNotInitialized();
            return await _storage.GetMacrosByCategoryAsync(category);
        }

        public async Task<IEnumerable<Macro>> GetMacrosByTagsAsync(IEnumerable<string> tags)
        {
            ThrowIfNotInitialized();
            return await _storage.GetMacrosByTagsAsync(tags);
        }

        public async Task<IEnumerable<Macro>> SearchMacrosAsync(string searchTerm)
        {
            ThrowIfNotInitialized();
            return await _storage.SearchMacrosAsync(searchTerm);
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            ThrowIfNotInitialized();
            var macros = await GetAllMacrosAsync();
            return macros.Where(m => !string.IsNullOrEmpty(m.Category))
                        .Select(m => m.Category!)
                        .Distinct()
                        .OrderBy(c => c);
        }

        public async Task<IEnumerable<string>> GetTagsAsync()
        {
            ThrowIfNotInitialized();
            var macros = await GetAllMacrosAsync();
            return macros.SelectMany(m => m.Tags)
                        .Distinct()
                        .OrderBy(t => t);
        }

        // Hotkey Management
        public async Task RegisterHotkeyAsync(Guid macroId, string hotkeyCombo)
        {
            ThrowIfNotInitialized();

            if (string.IsNullOrWhiteSpace(hotkeyCombo))
                throw new ArgumentException("Hotkey combination cannot be empty", nameof(hotkeyCombo));

            var macro = await GetMacroAsync(macroId);
            if (macro == null)
                throw new ArgumentException($"Macro with ID {macroId} not found", nameof(macroId));

            if (_registeredHotkeys.ContainsKey(hotkeyCombo))
                throw new InvalidOperationException($"Hotkey '{hotkeyCombo}' is already registered");

            try
            {
                macro.SetTrigger(MacroTriggerType.Hotkey, hotkeyCombo);
                await UpdateMacroAsync(macro);

                _registeredHotkeys.TryAdd(hotkeyCombo, macroId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register hotkey '{HotkeyCombo}' for macro: {MacroName}", hotkeyCombo, macro.Name);
                throw;
            }
        }

        public async Task UnregisterHotkeyAsync(Guid macroId)
        {
            ThrowIfNotInitialized();

            var macro = await GetMacroAsync(macroId);
            if (macro == null)
                return;

            if (macro.TriggerType != MacroTriggerType.Hotkey || string.IsNullOrEmpty(macro.HotkeyCombo))
                return;

            try
            {
                _registeredHotkeys.TryRemove(macro.HotkeyCombo, out _);
                
                macro.SetTrigger(MacroTriggerType.Manual);
                await UpdateMacroAsync(macro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister hotkey for macro: {MacroName}", macro.Name);
                throw;
            }
        }

        public async Task<Dictionary<string, Guid>> GetRegisteredHotkeysAsync()
        {
            ThrowIfNotInitialized();
            return await Task.FromResult(new Dictionary<string, Guid>(_registeredHotkeys));
        }

        public async Task<bool> IsHotkeyAvailableAsync(string hotkeyCombo)
        {
            ThrowIfNotInitialized();
            return await Task.FromResult(!_registeredHotkeys.ContainsKey(hotkeyCombo));
        }

        // Import/Export
        public async Task<string> ExportMacrosAsync(IEnumerable<Guid> macroIds)
        {
            ThrowIfNotInitialized();
            return await _storage.ExportMacrosAsync(macroIds);
        }

        public async Task<IEnumerable<Macro>> ImportMacrosAsync(string jsonData, bool overwriteExisting = false)
        {
            ThrowIfNotInitialized();

            try
            {
                var importedMacros = await _storage.ImportMacrosAsync(jsonData);
                var result = new List<Macro>();

                foreach (var macro in importedMacros)
                {
                    var existingMacro = await GetMacroAsync(macro.Id);
                    
                    if (existingMacro != null && !overwriteExisting)
                    {
                        var newMacro = macro.Clone($"{macro.Name} (Imported)");
                        await _storage.SaveMacroAsync(newMacro);
                        result.Add(newMacro);
                    }
                    else
                    {
                        await _storage.SaveMacroAsync(macro);
                        result.Add(macro);
                    }
                }

                var collectionChangedEvent = new MacroCollectionChangedEvent("Imported", null, $"{result.Count} macros");
                MacroCollectionChanged?.Invoke(this, collectionChangedEvent);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import macros");
                throw;
            }
        }

        public async Task<string> CreateBackupAsync()
        {
            ThrowIfNotInitialized();
            return await _storage.CreateBackupAsync();
        }

        public async Task RestoreFromBackupAsync(string backupData, bool clearExisting = false)
        {
            ThrowIfNotInitialized();

            try
            {
                if (clearExisting)
                {
                    var existingMacros = await GetAllMacrosAsync();
                    foreach (var macro in existingMacros)
                    {
                        await DeleteMacroAsync(macro.Id);
                    }
                }

                await _storage.RestoreFromBackupAsync(backupData);

                var collectionChangedEvent = new MacroCollectionChangedEvent("Restored", null, "Backup restored");
                MacroCollectionChanged?.Invoke(this, collectionChangedEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore from backup");
                throw;
            }
        }

        // Statistics and Analytics
        public async Task<Dictionary<string, object>> GetMacroStatsAsync(Guid macroId)
        {
            ThrowIfNotInitialized();

            var macro = await GetMacroAsync(macroId);
            if (macro == null)
                return new Dictionary<string, object>();

            return new Dictionary<string, object>
            {
                ["Id"] = macro.Id,
                ["Name"] = macro.Name,
                ["EventCount"] = macro.Events.Count,
                ["Duration"] = macro.Duration,
                ["ExecutionCount"] = macro.ExecutionCount,
                ["LastExecutedAt"] = macro.LastExecutedAt,
                ["CreatedAt"] = macro.CreatedAt,
                ["ModifiedAt"] = macro.ModifiedAt,
                ["IsEnabled"] = macro.IsEnabled,
                ["Status"] = macro.Status,
                ["TriggerType"] = macro.TriggerType,
                ["ExecutionMode"] = macro.ExecutionMode
            };
        }

        public async Task<Dictionary<string, object>> GetServiceStatsAsync()
        {
            ThrowIfNotInitialized();

            var macros = await GetAllMacrosAsync();
            var macroList = macros.ToList();

            var stats = new Dictionary<string, object>(_serviceStats)
            {
                ["TotalMacros"] = macroList.Count,
                ["EnabledMacros"] = macroList.Count(m => m.IsEnabled),
                ["DisabledMacros"] = macroList.Count(m => !m.IsEnabled),
                ["TotalEvents"] = macroList.Sum(m => m.Events.Count),
                ["AverageEventsPerMacro"] = macroList.Count > 0 ? macroList.Average(m => m.Events.Count) : 0,
                ["TotalExecutions"] = macroList.Sum(m => m.ExecutionCount),
                ["RegisteredHotkeys"] = _registeredHotkeys.Count,
                ["Categories"] = (await GetCategoriesAsync()).Count(),
                ["Tags"] = (await GetTagsAsync()).Count(),
                ["IsRecording"] = IsRecording,
                ["IsPlaying"] = IsPlaying
            };

            return stats;
        }

        public async Task<IEnumerable<Macro>> GetMostUsedMacrosAsync(int count = 10)
        {
            ThrowIfNotInitialized();
            var macros = await GetAllMacrosAsync();
            return macros.OrderByDescending(m => m.ExecutionCount)
                        .Take(count);
        }

        public async Task<IEnumerable<Macro>> GetRecentlyUsedMacrosAsync(int count = 10)
        {
            ThrowIfNotInitialized();
            var macros = await GetAllMacrosAsync();
            return macros.Where(m => m.LastExecutedAt.HasValue)
                        .OrderByDescending(m => m.LastExecutedAt)
                        .Take(count);
        }

        // Validation and Testing
        public async Task<(bool IsValid, List<string> Errors)> ValidateMacroAsync(Macro macro)
        {
            ThrowIfNotInitialized();

            if (macro == null)
                return (false, new List<string> { "Macro cannot be null" });

            var errors = macro.Validate();

            if (macro.TriggerType == MacroTriggerType.Hotkey && !string.IsNullOrEmpty(macro.HotkeyCombo))
            {
                var isAvailable = await IsHotkeyAvailableAsync(macro.HotkeyCombo);
                if (!isAvailable)
                {
                    var existingMacro = await GetMacroAsync(_registeredHotkeys[macro.HotkeyCombo]);
                    if (existingMacro?.Id != macro.Id)
                    {
                        errors.Add($"Hotkey '{macro.HotkeyCombo}' is already registered to another macro");
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        public async Task<(bool CanExecute, string? Reason)> TestMacroAsync(Macro macro)
        {
            ThrowIfNotInitialized();

            if (macro == null)
                return (false, "Macro cannot be null");

            var (isValid, errors) = await ValidateMacroAsync(macro);
            if (!isValid)
                return (false, string.Join("; ", errors));

            return await _inputPlayer.ValidateMacroAsync(macro);
        }

        public TimeSpan GetEstimatedExecutionTime(Macro macro, MacroExecutionMode? executionMode = null, int? repeatCount = null)
        {
            ThrowIfNotInitialized();

            if (macro == null)
                return TimeSpan.Zero;

            return _inputPlayer.GetEstimatedDuration(macro, executionMode, repeatCount);
        }

        private void SubscribeToEvents()
        {
            _inputRecorder.EventRecorded += OnEventRecorded;
            _inputPlayer.PlaybackStarted += OnPlaybackStarted;
            _inputPlayer.PlaybackStopped += OnPlaybackStopped;
        }

        private void OnEventRecorded(object? sender, MacroEvent macroEvent)
        {
            if (_currentRecordingMacro != null)
            {
                var eventRecordedEvent = new MacroEventRecordedEvent(_currentRecordingMacro.Id, macroEvent);
                EventRecorded?.Invoke(this, eventRecordedEvent);
            }
        }

        private void OnPlaybackStarted(object? sender, Macro macro)
        {
            // Event is already handled in PlayMacroAsync
        }

        private void OnPlaybackStopped(object? sender, (Macro Macro, bool Success, string? Error) args)
        {
            if (_currentPlayingMacro != null)
            {
                _currentPlayingMacro.SetStatus(args.Success ? MacroStatus.Completed : MacroStatus.Failed);
                var playbackStoppedEvent = new MacroPlaybackStoppedEvent(_currentPlayingMacro, args.Success, args.Error);
                PlaybackStopped?.Invoke(this, playbackStoppedEvent);
                _currentPlayingMacro = null;
            }
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("MacroService is not initialized. Call InitializeAsync() first.");
        }

        private void UpdateServiceStats(string key, object value)
        {
            _serviceStats[key] = value;
        }
    }
}
