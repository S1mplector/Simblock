using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;

namespace SimBlock.Core.Application.Interfaces
{
    /// <summary>
    /// Interface for playing back recorded input events
    /// </summary>
    public interface IInputPlayer
    {
        /// <summary>
        /// Event raised when playback starts
        /// </summary>
        event EventHandler<Macro>? PlaybackStarted;

        /// <summary>
        /// Event raised when playback stops
        /// </summary>
        event EventHandler<(Macro Macro, bool Success, string? Error)>? PlaybackStopped;

        /// <summary>
        /// Event raised when playback is paused
        /// </summary>
        event EventHandler<Macro>? PlaybackPaused;

        /// <summary>
        /// Event raised when playback is resumed
        /// </summary>
        event EventHandler<Macro>? PlaybackResumed;

        /// <summary>
        /// Event raised when an individual event is executed
        /// </summary>
        event EventHandler<(MacroEvent Event, int Index, bool Success)>? EventExecuted;

        /// <summary>
        /// Event raised to report playback progress
        /// </summary>
        event EventHandler<(int Current, int Total, double Percentage)>? ProgressChanged;

        /// <summary>
        /// Gets whether playback is currently active
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Gets whether playback is currently paused
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Gets the currently playing macro
        /// </summary>
        Macro? CurrentMacro { get; }

        /// <summary>
        /// Gets the current event index being played
        /// </summary>
        int CurrentEventIndex { get; }

        /// <summary>
        /// Gets the current execution iteration (for repeat/loop modes)
        /// </summary>
        int CurrentIteration { get; }

        /// <summary>
        /// Plays a macro with the specified execution mode
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
        /// Plays a single macro event
        /// </summary>
        Task<bool> PlayEventAsync(MacroEvent macroEvent);

        /// <summary>
        /// Sets the playback speed multiplier (1.0 = normal speed, 0.5 = half speed, 2.0 = double speed)
        /// </summary>
        void SetPlaybackSpeed(double speedMultiplier);

        /// <summary>
        /// Gets the current playback speed multiplier
        /// </summary>
        double GetPlaybackSpeed();

        /// <summary>
        /// Sets whether to respect original timing delays between events
        /// </summary>
        void SetRespectTiming(bool respectTiming);

        /// <summary>
        /// Sets a custom delay between events (overrides original timing if respectTiming is false)
        /// </summary>
        void SetCustomDelay(TimeSpan delay);

        /// <summary>
        /// Sets the target window for playback (null for global)
        /// </summary>
        void SetTargetWindow(IntPtr windowHandle);

        /// <summary>
        /// Gets playback statistics for the current session
        /// </summary>
        Dictionary<string, object> GetPlaybackStats();

        /// <summary>
        /// Validates that a macro can be played
        /// </summary>
        Task<(bool CanPlay, string? Reason)> ValidateMacroAsync(Macro macro);

        /// <summary>
        /// Gets the estimated duration for playing a macro
        /// </summary>
        TimeSpan GetEstimatedDuration(Macro macro, MacroExecutionMode? executionMode = null, int? repeatCount = null);
    }
}
