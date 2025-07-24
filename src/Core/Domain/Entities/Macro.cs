using SimBlock.Core.Domain.Enums;
using System.Text.Json.Serialization;

namespace SimBlock.Core.Domain.Entities
{
    /// <summary>
    /// Represents a macro with a sequence of input events and configuration
    /// </summary>
    public class Macro
    {
        /// <summary>
        /// Unique identifier for this macro
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Display name of the macro
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Optional description of what this macro does
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Category or group this macro belongs to
        /// </summary>
        public string? Category { get; private set; }

        /// <summary>
        /// List of events that make up this macro
        /// </summary>
        public List<MacroEvent> Events { get; private set; }

        /// <summary>
        /// How this macro should be triggered
        /// </summary>
        public MacroTriggerType TriggerType { get; private set; }

        /// <summary>
        /// Hotkey combination for hotkey-triggered macros
        /// </summary>
        public string? HotkeyCombo { get; private set; }

        /// <summary>
        /// How this macro should be executed
        /// </summary>
        public MacroExecutionMode ExecutionMode { get; private set; }

        /// <summary>
        /// Number of times to repeat (for Repeat execution mode)
        /// </summary>
        public int RepeatCount { get; private set; }

        /// <summary>
        /// Interval between executions in milliseconds (for Interval execution mode)
        /// </summary>
        public int IntervalMs { get; private set; }

        /// <summary>
        /// Random interval range for RandomInterval execution mode (min, max in ms)
        /// </summary>
        public (int Min, int Max) RandomIntervalRange { get; private set; }

        /// <summary>
        /// Target window title or class name (empty for global)
        /// </summary>
        public string? TargetWindow { get; private set; }

        /// <summary>
        /// Target application process name
        /// </summary>
        public string? TargetApplication { get; private set; }

        /// <summary>
        /// Whether this macro is currently enabled
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Current status of the macro
        /// </summary>
        public MacroStatus Status { get; private set; }

        /// <summary>
        /// When this macro was created
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// When this macro was last modified
        /// </summary>
        public DateTime ModifiedAt { get; private set; }

        /// <summary>
        /// When this macro was last executed
        /// </summary>
        public DateTime? LastExecutedAt { get; private set; }

        /// <summary>
        /// Number of times this macro has been executed
        /// </summary>
        public int ExecutionCount { get; private set; }

        /// <summary>
        /// Total duration of the macro in milliseconds
        /// </summary>
        public TimeSpan Duration => Events.Count > 0 ? Events.Max(e => e.Timestamp) : TimeSpan.Zero;

        /// <summary>
        /// Variables defined within this macro
        /// </summary>
        public Dictionary<string, string> Variables { get; private set; }

        /// <summary>
        /// Additional metadata for the macro
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Tags for organizing and searching macros
        /// </summary>
        public HashSet<string> Tags { get; private set; }

        [JsonConstructor]
        public Macro(
            Guid id,
            string name,
            string? description = null,
            string? category = null,
            List<MacroEvent>? events = null,
            MacroTriggerType triggerType = MacroTriggerType.Manual,
            string? hotkeyCombo = null,
            MacroExecutionMode executionMode = MacroExecutionMode.Once,
            int repeatCount = 1,
            int intervalMs = 1000,
            (int Min, int Max) randomIntervalRange = default,
            string? targetWindow = null,
            string? targetApplication = null,
            bool isEnabled = true,
            MacroStatus status = MacroStatus.Idle,
            DateTime createdAt = default,
            DateTime modifiedAt = default,
            DateTime? lastExecutedAt = null,
            int executionCount = 0,
            Dictionary<string, string>? variables = null,
            Dictionary<string, object>? metadata = null,
            HashSet<string>? tags = null)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            Category = category;
            Events = events ?? new List<MacroEvent>();
            TriggerType = triggerType;
            HotkeyCombo = hotkeyCombo;
            ExecutionMode = executionMode;
            RepeatCount = Math.Max(1, repeatCount);
            IntervalMs = Math.Max(0, intervalMs);
            RandomIntervalRange = randomIntervalRange == default ? (1000, 5000) : randomIntervalRange;
            TargetWindow = targetWindow;
            TargetApplication = targetApplication;
            IsEnabled = isEnabled;
            Status = status;
            CreatedAt = createdAt == default ? DateTime.UtcNow : createdAt;
            ModifiedAt = modifiedAt == default ? DateTime.UtcNow : modifiedAt;
            LastExecutedAt = lastExecutedAt;
            ExecutionCount = Math.Max(0, executionCount);
            Variables = variables ?? new Dictionary<string, string>();
            Metadata = metadata ?? new Dictionary<string, object>();
            Tags = tags ?? new HashSet<string>();
        }

        /// <summary>
        /// Creates a new macro with the specified name
        /// </summary>
        public static Macro Create(string name, string? description = null, string? category = null)
        {
            return new Macro(
                id: Guid.NewGuid(),
                name: name,
                description: description,
                category: category);
        }

        /// <summary>
        /// Updates the macro's basic information
        /// </summary>
        public void UpdateInfo(string? name = null, string? description = null, string? category = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
                Name = name;
            
            Description = description;
            Category = category;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the trigger configuration for this macro
        /// </summary>
        public void SetTrigger(MacroTriggerType triggerType, string? hotkeyCombo = null, string? targetWindow = null, string? targetApplication = null)
        {
            TriggerType = triggerType;
            HotkeyCombo = hotkeyCombo;
            TargetWindow = targetWindow;
            TargetApplication = targetApplication;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the execution configuration for this macro
        /// </summary>
        public void SetExecution(MacroExecutionMode executionMode, int repeatCount = 1, int intervalMs = 1000, (int Min, int Max)? randomIntervalRange = null)
        {
            ExecutionMode = executionMode;
            RepeatCount = Math.Max(1, repeatCount);
            IntervalMs = Math.Max(0, intervalMs);
            if (randomIntervalRange.HasValue)
                RandomIntervalRange = randomIntervalRange.Value;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Adds an event to the macro
        /// </summary>
        public void AddEvent(MacroEvent macroEvent)
        {
            if (macroEvent == null)
                throw new ArgumentNullException(nameof(macroEvent));

            Events.Add(macroEvent);
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Inserts an event at a specific position
        /// </summary>
        public void InsertEvent(int index, MacroEvent macroEvent)
        {
            if (macroEvent == null)
                throw new ArgumentNullException(nameof(macroEvent));

            Events.Insert(index, macroEvent);
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Removes an event from the macro
        /// </summary>
        public bool RemoveEvent(MacroEvent macroEvent)
        {
            var removed = Events.Remove(macroEvent);
            if (removed)
                ModifiedAt = DateTime.UtcNow;
            return removed;
        }

        /// <summary>
        /// Removes an event by its ID
        /// </summary>
        public bool RemoveEvent(Guid eventId)
        {
            var eventToRemove = Events.FirstOrDefault(e => e.Id == eventId);
            if (eventToRemove != null)
            {
                Events.Remove(eventToRemove);
                ModifiedAt = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all events from the macro
        /// </summary>
        public void ClearEvents()
        {
            Events.Clear();
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Updates the macro's status
        /// </summary>
        public void SetStatus(MacroStatus status)
        {
            Status = status;
            
            if (status == MacroStatus.Playing)
            {
                LastExecutedAt = DateTime.UtcNow;
                ExecutionCount++;
            }
        }

        /// <summary>
        /// Enables or disables the macro
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets a variable value
        /// </summary>
        public void SetVariable(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Variable name cannot be empty", nameof(name));

            Variables[name] = value ?? string.Empty;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a variable value
        /// </summary>
        public string? GetVariable(string name)
        {
            return Variables.TryGetValue(name, out var value) ? value : null;
        }

        /// <summary>
        /// Removes a variable
        /// </summary>
        public bool RemoveVariable(string name)
        {
            var removed = Variables.Remove(name);
            if (removed)
                ModifiedAt = DateTime.UtcNow;
            return removed;
        }

        /// <summary>
        /// Adds or updates metadata
        /// </summary>
        public void SetMetadata(string key, object value)
        {
            Metadata[key] = value;
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets metadata value by key
        /// </summary>
        public T? GetMetadata<T>(string key)
        {
            if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Adds a tag to the macro
        /// </summary>
        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                Tags.Add(tag.Trim().ToLowerInvariant());
                ModifiedAt = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Removes a tag from the macro
        /// </summary>
        public bool RemoveTag(string tag)
        {
            var removed = Tags.Remove(tag?.Trim().ToLowerInvariant() ?? string.Empty);
            if (removed)
                ModifiedAt = DateTime.UtcNow;
            return removed;
        }

        /// <summary>
        /// Sorts events by timestamp
        /// </summary>
        public void SortEventsByTimestamp()
        {
            Events.Sort((e1, e2) => e1.Timestamp.CompareTo(e2.Timestamp));
            ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Validates the macro configuration
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Macro name is required");

            if (TriggerType == MacroTriggerType.Hotkey && string.IsNullOrWhiteSpace(HotkeyCombo))
                errors.Add("Hotkey combination is required for hotkey-triggered macros");

            if (ExecutionMode == MacroExecutionMode.Repeat && RepeatCount <= 0)
                errors.Add("Repeat count must be greater than 0 for repeat execution mode");

            if (ExecutionMode == MacroExecutionMode.Interval && IntervalMs <= 0)
                errors.Add("Interval must be greater than 0 for interval execution mode");

            if (ExecutionMode == MacroExecutionMode.RandomInterval && 
                (RandomIntervalRange.Min <= 0 || RandomIntervalRange.Max <= RandomIntervalRange.Min))
                errors.Add("Random interval range must have valid min/max values");

            if (Events.Count == 0)
                errors.Add("Macro must contain at least one event");

            return errors;
        }

        /// <summary>
        /// Creates a deep copy of this macro
        /// </summary>
        public Macro Clone(string? newName = null)
        {
            var clonedEvents = Events.Select(e => e.Clone()).ToList();
            
            return new Macro(
                id: Guid.NewGuid(),
                name: newName ?? $"{Name} (Copy)",
                description: Description,
                category: Category,
                events: clonedEvents,
                triggerType: TriggerType,
                hotkeyCombo: HotkeyCombo,
                executionMode: ExecutionMode,
                repeatCount: RepeatCount,
                intervalMs: IntervalMs,
                randomIntervalRange: RandomIntervalRange,
                targetWindow: TargetWindow,
                targetApplication: TargetApplication,
                isEnabled: IsEnabled,
                status: MacroStatus.Idle,
                variables: new Dictionary<string, string>(Variables),
                metadata: new Dictionary<string, object>(Metadata),
                tags: new HashSet<string>(Tags));
        }

        public override string ToString()
        {
            return $"{Name} ({Events.Count} events, {Status})";
        }
    }
}
