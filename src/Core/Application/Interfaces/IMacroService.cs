using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;
using SimBlock.Core.Domain.Events;

namespace SimBlock.Core.Application.Interfaces
{
    /// <summary>
    /// Main service interface for macro management and execution
    /// </summary>
    public interface IMacroService
    {
        #region Events

        /// <summary>
        /// Event raised when macro recording starts
        /// </summary>
        event EventHandler<MacroRecordingStartedEvent>? RecordingStarted;

        /// <summary>
        /// Event raised when macro recording stops
        /// </summary>
        event EventHandler<MacroRecordingStoppedEvent>? RecordingStopped;

        /// <summary>
        /// Event raised when a macro event is recorded
        /// </summary>
        event EventHandler<MacroEventRecordedEvent>? EventRecorded;

        /// <summary>
        /// Event raised when macro playback starts
        /// </summary>
        event EventHandler<MacroPlaybackStartedEvent>? PlaybackStarted;

        /// <summary>
        /// Event raised when macro playback stops
        /// </summary>
        event EventHandler<MacroPlaybackStoppedEvent>? PlaybackStopped;

        /// <summary>
        /// Event raised when macro playback is paused
        /// </summary>
        event EventHandler<MacroPlaybackPausedEvent>? PlaybackPaused;

        /// <summary>
        /// Event raised when macro playback is resumed
        /// </summary>
        event EventHandler<MacroPlaybackResumedEvent>? PlaybackResumed;

        /// <summary>
        /// Event raised when a macro is created, updated, or deleted
        /// </summary>
        event EventHandler<MacroCollectionChangedEvent>? MacroCollectionChanged;

        /// <summary>
        /// Event raised when a macro hotkey is triggered
        /// </summary>
        event EventHandler<MacroHotkeyTriggeredEvent>? HotkeyTriggered;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether macro recording is currently active
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// Gets whether macro playback is currently active
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Gets the currently recording macro
        /// </summary>
        Macro? CurrentRecordingMacro { get; }

        /// <summary>
        /// Gets the currently playing macro
        /// </summary>
        Macro? CurrentPlayingMacro { get; }

        /// <summary>
        /// Gets all available macros
        /// </summary>
        Task<IEnumerable<Macro>> GetAllMacrosAsync();

        #endregion

        #region Recording Operations

        /// <summary>
        /// Starts recording a new macro
        /// </summary>
        Task<Macro> StartRecordingAsync(string macroName, string? description = null, string? category = null);

        /// <summary>
        /// Stops the current recording session
        /// </summary>
        Task<Macro?> StopRecordingAsync();

        /// <summary>
        /// Pauses the current recording session
        /// </summary>
        void PauseRecording();

        /// <summary>
        /// Resumes the paused recording session
        /// </summary>
        void ResumeRecording();

        /// <summary>
        /// Cancels the current recording session without saving
        /// </summary>
        Task CancelRecordingAsync();

        /// <summary>
        /// Sets recording options
        /// </summary>
        void SetRecordingOptions(bool recordKeyboard, bool recordMouse, bool recordMouseMovement, bool recordDelays, TimeSpan minimumDelay);

        #endregion

        #region Playback Operations

        /// <summary>
        /// Plays a macro by ID
        /// </summary>
        Task PlayMacroAsync(Guid macroId, MacroExecutionMode? executionMode = null, int? repeatCount = null);

        /// <summary>
        /// Plays a macro
        /// </summary>
        Task PlayMacroAsync(Macro macro, MacroExecutionMode? executionMode = null, int? repeatCount = null);

        /// <summary>
        /// Stops the currently playing macro
        /// </summary>
        Task StopPlaybackAsync();

        /// <summary>
        /// Pauses the currently playing macro
        /// </summary>
        void PausePlayback();

        /// <summary>
        /// Resumes the paused macro
        /// </summary>
        void ResumePlayback();

        /// <summary>
        /// Sets playback options
        /// </summary>
        void SetPlaybackOptions(double speedMultiplier, bool respectTiming, TimeSpan? customDelay = null);

        #endregion

        #region Macro Management

        /// <summary>
        /// Creates a new empty macro
        /// </summary>
        Task<Macro> CreateMacroAsync(string name, string? description = null, string? category = null);

        /// <summary>
        /// Gets a macro by ID
        /// </summary>
        Task<Macro?> GetMacroAsync(Guid id);

        /// <summary>
        /// Updates an existing macro
        /// </summary>
        Task UpdateMacroAsync(Macro macro);

        /// <summary>
        /// Deletes a macro
        /// </summary>
        Task DeleteMacroAsync(Guid id);

        /// <summary>
        /// Duplicates a macro
        /// </summary>
        Task<Macro> DuplicateMacroAsync(Guid id, string? newName = null);

        /// <summary>
        /// Gets macros by category
        /// </summary>
        Task<IEnumerable<Macro>> GetMacrosByCategoryAsync(string category);

        /// <summary>
        /// Gets macros by tags
        /// </summary>
        Task<IEnumerable<Macro>> GetMacrosByTagsAsync(IEnumerable<string> tags);

        /// <summary>
        /// Searches macros by name or description
        /// </summary>
        Task<IEnumerable<Macro>> SearchMacrosAsync(string searchTerm);

        /// <summary>
        /// Gets all macro categories
        /// </summary>
        Task<IEnumerable<string>> GetCategoriesAsync();

        /// <summary>
        /// Gets all macro tags
        /// </summary>
        Task<IEnumerable<string>> GetTagsAsync();

        #endregion

        #region Hotkey Management

        /// <summary>
        /// Registers a hotkey for a macro
        /// </summary>
        Task RegisterHotkeyAsync(Guid macroId, string hotkeyCombo);

        /// <summary>
        /// Unregisters a hotkey for a macro
        /// </summary>
        Task UnregisterHotkeyAsync(Guid macroId);

        /// <summary>
        /// Gets all registered hotkeys
        /// </summary>
        Task<Dictionary<string, Guid>> GetRegisteredHotkeysAsync();

        /// <summary>
        /// Checks if a hotkey combination is available
        /// </summary>
        Task<bool> IsHotkeyAvailableAsync(string hotkeyCombo);

        #endregion

        #region Import/Export

        /// <summary>
        /// Exports macros to a JSON string
        /// </summary>
        Task<string> ExportMacrosAsync(IEnumerable<Guid> macroIds);

        /// <summary>
        /// Imports macros from a JSON string
        /// </summary>
        Task<IEnumerable<Macro>> ImportMacrosAsync(string jsonData, bool overwriteExisting = false);

        /// <summary>
        /// Creates a backup of all macros
        /// </summary>
        Task<string> CreateBackupAsync();

        /// <summary>
        /// Restores macros from a backup
        /// </summary>
        Task RestoreFromBackupAsync(string backupData, bool clearExisting = false);

        #endregion

        #region Statistics and Analytics

        /// <summary>
        /// Gets macro execution statistics
        /// </summary>
        Task<Dictionary<string, object>> GetMacroStatsAsync(Guid macroId);

        /// <summary>
        /// Gets overall macro service statistics
        /// </summary>
        Task<Dictionary<string, object>> GetServiceStatsAsync();

        /// <summary>
        /// Gets the most frequently used macros
        /// </summary>
        Task<IEnumerable<Macro>> GetMostUsedMacrosAsync(int count = 10);

        /// <summary>
        /// Gets recently executed macros
        /// </summary>
        Task<IEnumerable<Macro>> GetRecentlyUsedMacrosAsync(int count = 10);

        #endregion

        #region Validation and Testing

        /// <summary>
        /// Validates a macro configuration
        /// </summary>
        Task<(bool IsValid, List<string> Errors)> ValidateMacroAsync(Macro macro);

        /// <summary>
        /// Tests a macro without actually executing it
        /// </summary>
        Task<(bool CanExecute, string? Reason)> TestMacroAsync(Macro macro);

        /// <summary>
        /// Gets the estimated execution time for a macro
        /// </summary>
        TimeSpan GetEstimatedExecutionTime(Macro macro, MacroExecutionMode? executionMode = null, int? repeatCount = null);

        #endregion

        #region Service Management

        /// <summary>
        /// Initializes the macro service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Shuts down the macro service
        /// </summary>
        Task ShutdownAsync();

        /// <summary>
        /// Gets the current service status
        /// </summary>
        bool IsInitialized { get; }

        #endregion
    }
}
