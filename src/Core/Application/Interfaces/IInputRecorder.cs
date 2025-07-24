using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Events;

namespace SimBlock.Core.Application.Interfaces
{
    /// <summary>
    /// Interface for recording input events (keyboard and mouse)
    /// </summary>
    public interface IInputRecorder
    {
        /// <summary>
        /// Event raised when an input event is recorded
        /// </summary>
        event EventHandler<MacroEvent>? EventRecorded;

        /// <summary>
        /// Event raised when recording starts
        /// </summary>
        event EventHandler? RecordingStarted;

        /// <summary>
        /// Event raised when recording stops
        /// </summary>
        event EventHandler<TimeSpan>? RecordingStopped;

        /// <summary>
        /// Gets whether recording is currently active
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// Gets the current recording session start time
        /// </summary>
        DateTime? RecordingStartTime { get; }

        /// <summary>
        /// Gets the number of events recorded in the current session
        /// </summary>
        int RecordedEventCount { get; }

        /// <summary>
        /// Starts recording input events
        /// </summary>
        Task StartRecordingAsync();

        /// <summary>
        /// Stops recording input events
        /// </summary>
        Task<IEnumerable<MacroEvent>> StopRecordingAsync();

        /// <summary>
        /// Pauses recording (events are ignored but recording session continues)
        /// </summary>
        void PauseRecording();

        /// <summary>
        /// Resumes recording after pause
        /// </summary>
        void ResumeRecording();

        /// <summary>
        /// Gets whether recording is currently paused
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Sets the recording filters (which events to record)
        /// </summary>
        void SetRecordingFilters(bool recordKeyboard, bool recordMouse, bool recordMouseMovement, bool recordDelays);

        /// <summary>
        /// Sets the minimum delay between events to record
        /// </summary>
        void SetMinimumDelay(TimeSpan minimumDelay);

        /// <summary>
        /// Sets the maximum recording duration
        /// </summary>
        void SetMaxRecordingDuration(TimeSpan maxDuration);

        /// <summary>
        /// Gets the current recording filters
        /// </summary>
        (bool Keyboard, bool Mouse, bool MouseMovement, bool Delays) GetRecordingFilters();

        /// <summary>
        /// Clears the current recording session without stopping
        /// </summary>
        void ClearCurrentSession();

        /// <summary>
        /// Gets recording statistics for the current session
        /// </summary>
        Dictionary<string, object> GetRecordingStats();
    }
}
