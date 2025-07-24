using SimBlock.Core.Domain.Entities;
using SimBlock.Core.Domain.Enums;

namespace SimBlock.Core.Domain.Events
{
    /// <summary>
    /// Base class for all macro-related events
    /// </summary>
    public abstract class MacroEventBase
    {
        public Guid MacroId { get; }
        public DateTime Timestamp { get; }

        protected MacroEventBase(Guid macroId)
        {
            MacroId = macroId;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event raised when macro recording starts
    /// </summary>
    public class MacroRecordingStartedEvent : MacroEventBase
    {
        public Macro Macro { get; }

        public MacroRecordingStartedEvent(Macro macro) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
        }
    }

    /// <summary>
    /// Event raised when macro recording stops
    /// </summary>
    public class MacroRecordingStoppedEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public int EventCount { get; }
        public TimeSpan Duration { get; }

        public MacroRecordingStoppedEvent(Macro macro, int eventCount, TimeSpan duration) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            EventCount = eventCount;
            Duration = duration;
        }
    }

    /// <summary>
    /// Event raised when a macro event is recorded
    /// </summary>
    public class MacroEventRecordedEvent : MacroEventBase
    {
        public MacroEvent Event { get; }

        public MacroEventRecordedEvent(Guid macroId, MacroEvent macroEvent) : base(macroId)
        {
            Event = macroEvent ?? throw new ArgumentNullException(nameof(macroEvent));
        }
    }

    /// <summary>
    /// Event raised when macro playback starts
    /// </summary>
    public class MacroPlaybackStartedEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public MacroExecutionMode ExecutionMode { get; }
        public int RepeatCount { get; }

        public MacroPlaybackStartedEvent(Macro macro, MacroExecutionMode executionMode, int repeatCount) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            ExecutionMode = executionMode;
            RepeatCount = repeatCount;
        }
    }

    /// <summary>
    /// Event raised when macro playback stops
    /// </summary>
    public class MacroPlaybackStoppedEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public bool WasSuccessful { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        public MacroPlaybackStoppedEvent(Macro macro, bool wasSuccessful, string? errorMessage = null, Exception? exception = null) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            WasSuccessful = wasSuccessful;
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    /// <summary>
    /// Event raised when macro playback is paused
    /// </summary>
    public class MacroPlaybackPausedEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public int CurrentEventIndex { get; }

        public MacroPlaybackPausedEvent(Macro macro, int currentEventIndex) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            CurrentEventIndex = currentEventIndex;
        }
    }

    /// <summary>
    /// Event raised when macro playback is resumed
    /// </summary>
    public class MacroPlaybackResumedEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public int CurrentEventIndex { get; }

        public MacroPlaybackResumedEvent(Macro macro, int currentEventIndex) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            CurrentEventIndex = currentEventIndex;
        }
    }

    /// <summary>
    /// Event raised when a macro event is executed during playback
    /// </summary>
    public class MacroEventExecutedEvent : MacroEventBase
    {
        public MacroEvent Event { get; }
        public int EventIndex { get; }
        public bool WasSuccessful { get; }
        public string? ErrorMessage { get; }

        public MacroEventExecutedEvent(Guid macroId, MacroEvent macroEvent, int eventIndex, bool wasSuccessful, string? errorMessage = null) : base(macroId)
        {
            Event = macroEvent ?? throw new ArgumentNullException(nameof(macroEvent));
            EventIndex = eventIndex;
            WasSuccessful = wasSuccessful;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Event raised when a macro is created
    /// </summary>
    public class MacroCreatedEvent : MacroEventBase
    {
        public Macro Macro { get; }

        public MacroCreatedEvent(Macro macro) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
        }
    }

    /// <summary>
    /// Event raised when a macro is updated
    /// </summary>
    public class MacroUpdatedEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public string[] ChangedProperties { get; }

        public MacroUpdatedEvent(Macro macro, params string[] changedProperties) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            ChangedProperties = changedProperties ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Event raised when a macro is deleted
    /// </summary>
    public class MacroDeletedEvent : MacroEventBase
    {
        public string MacroName { get; }

        public MacroDeletedEvent(Guid macroId, string macroName) : base(macroId)
        {
            MacroName = macroName ?? throw new ArgumentNullException(nameof(macroName));
        }
    }

    /// <summary>
    /// Event raised when a macro is enabled or disabled
    /// </summary>
    public class MacroEnabledChangedEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public bool IsEnabled { get; }

        public MacroEnabledChangedEvent(Macro macro, bool isEnabled) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            IsEnabled = isEnabled;
        }
    }

    /// <summary>
    /// Event raised when a macro's hotkey is triggered
    /// </summary>
    public class MacroHotkeyTriggeredEvent : MacroEventBase
    {
        public Macro Macro { get; }
        public string HotkeyCombo { get; }

        public MacroHotkeyTriggeredEvent(Macro macro, string hotkeyCombo) : base(macro.Id)
        {
            Macro = macro ?? throw new ArgumentNullException(nameof(macro));
            HotkeyCombo = hotkeyCombo ?? throw new ArgumentNullException(nameof(hotkeyCombo));
        }
    }

    /// <summary>
    /// Event raised when macro collection changes (macros added, removed, etc.)
    /// </summary>
    public class MacroCollectionChangedEvent
    {
        public string ChangeType { get; }
        public Guid? MacroId { get; }
        public string? MacroName { get; }
        public DateTime Timestamp { get; }

        public MacroCollectionChangedEvent(string changeType, Guid? macroId = null, string? macroName = null)
        {
            ChangeType = changeType ?? throw new ArgumentNullException(nameof(changeType));
            MacroId = macroId;
            MacroName = macroName;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event raised when macro execution statistics are updated
    /// </summary>
    public class MacroExecutionStatsUpdatedEvent : MacroEventBase
    {
        public int ExecutionCount { get; }
        public DateTime LastExecutedAt { get; }
        public TimeSpan AverageExecutionTime { get; }

        public MacroExecutionStatsUpdatedEvent(Guid macroId, int executionCount, DateTime lastExecutedAt, TimeSpan averageExecutionTime) : base(macroId)
        {
            ExecutionCount = executionCount;
            LastExecutedAt = lastExecutedAt;
            AverageExecutionTime = averageExecutionTime;
        }
    }
}
