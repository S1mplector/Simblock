using SimBlock.Core.Domain.Enums;
using System.Text.Json.Serialization;

namespace SimBlock.Core.Domain.Entities
{
    /// <summary>
    /// Represents a single event within a macro sequence
    /// </summary>
    public class MacroEvent
    {
        /// <summary>
        /// Unique identifier for this event
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Type of input event
        /// </summary>
        public InputType Type { get; private set; }

        /// <summary>
        /// Timestamp when this event occurred during recording (relative to macro start)
        /// </summary>
        public TimeSpan Timestamp { get; private set; }

        /// <summary>
        /// Key code for keyboard events (VK_* constants)
        /// </summary>
        public int? KeyCode { get; private set; }

        /// <summary>
        /// Mouse button for mouse events (Left, Right, Middle, etc.)
        /// </summary>
        public int? MouseButton { get; private set; }

        /// <summary>
        /// X coordinate for mouse events
        /// </summary>
        public int? X { get; private set; }

        /// <summary>
        /// Y coordinate for mouse events
        /// </summary>
        public int? Y { get; private set; }

        /// <summary>
        /// Wheel delta for mouse wheel events
        /// </summary>
        public int? WheelDelta { get; private set; }

        /// <summary>
        /// Text content for text input events
        /// </summary>
        public string? Text { get; private set; }

        /// <summary>
        /// Script content for script execution events
        /// </summary>
        public string? Script { get; private set; }

        /// <summary>
        /// Variable name for variable operations
        /// </summary>
        public string? VariableName { get; private set; }

        /// <summary>
        /// Variable value for variable operations
        /// </summary>
        public string? VariableValue { get; private set; }

        /// <summary>
        /// Condition expression for conditional events
        /// </summary>
        public string? Condition { get; private set; }

        /// <summary>
        /// Delay duration in milliseconds for delay events
        /// </summary>
        public int? DelayMs { get; private set; }

        /// <summary>
        /// Loop count for loop events
        /// </summary>
        public int? LoopCount { get; private set; }

        /// <summary>
        /// Additional metadata for the event
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Whether this event should be executed (can be disabled for debugging)
        /// </summary>
        public bool IsEnabled { get; private set; }

        /// <summary>
        /// Optional description or comment for this event
        /// </summary>
        public string? Description { get; private set; }

        [JsonConstructor]
        public MacroEvent(
            Guid id,
            InputType type,
            TimeSpan timestamp,
            int? keyCode = null,
            int? mouseButton = null,
            int? x = null,
            int? y = null,
            int? wheelDelta = null,
            string? text = null,
            string? script = null,
            string? variableName = null,
            string? variableValue = null,
            string? condition = null,
            int? delayMs = null,
            int? loopCount = null,
            Dictionary<string, object>? metadata = null,
            bool isEnabled = true,
            string? description = null)
        {
            Id = id;
            Type = type;
            Timestamp = timestamp;
            KeyCode = keyCode;
            MouseButton = mouseButton;
            X = x;
            Y = y;
            WheelDelta = wheelDelta;
            Text = text;
            Script = script;
            VariableName = variableName;
            VariableValue = variableValue;
            Condition = condition;
            DelayMs = delayMs;
            LoopCount = loopCount;
            Metadata = metadata ?? new Dictionary<string, object>();
            IsEnabled = isEnabled;
            Description = description;
        }

        /// <summary>
        /// Creates a keyboard event
        /// </summary>
        public static MacroEvent CreateKeyboardEvent(InputType type, int keyCode, TimeSpan timestamp, string? description = null)
        {
            if (type != InputType.KeyDown && type != InputType.KeyUp)
                throw new ArgumentException("Invalid input type for keyboard event", nameof(type));

            return new MacroEvent(
                id: Guid.NewGuid(),
                type: type,
                timestamp: timestamp,
                keyCode: keyCode,
                description: description);
        }

        /// <summary>
        /// Creates a mouse event
        /// </summary>
        public static MacroEvent CreateMouseEvent(InputType type, int? mouseButton, int x, int y, TimeSpan timestamp, int? wheelDelta = null, string? description = null)
        {
            if (type != InputType.MouseDown && type != InputType.MouseUp && type != InputType.MouseMove && type != InputType.MouseWheel)
                throw new ArgumentException("Invalid input type for mouse event", nameof(type));

            return new MacroEvent(
                id: Guid.NewGuid(),
                type: type,
                timestamp: timestamp,
                mouseButton: mouseButton,
                x: x,
                y: y,
                wheelDelta: wheelDelta,
                description: description);
        }

        /// <summary>
        /// Creates a delay event
        /// </summary>
        public static MacroEvent CreateDelayEvent(int delayMs, TimeSpan timestamp, string? description = null)
        {
            return new MacroEvent(
                id: Guid.NewGuid(),
                type: InputType.Delay,
                timestamp: timestamp,
                delayMs: delayMs,
                description: description);
        }

        /// <summary>
        /// Creates a text input event
        /// </summary>
        public static MacroEvent CreateTextInputEvent(string text, TimeSpan timestamp, string? description = null)
        {
            return new MacroEvent(
                id: Guid.NewGuid(),
                type: InputType.TextInput,
                timestamp: timestamp,
                text: text,
                description: description);
        }

        /// <summary>
        /// Creates a script execution event
        /// </summary>
        public static MacroEvent CreateScriptEvent(string script, TimeSpan timestamp, string? description = null)
        {
            return new MacroEvent(
                id: Guid.NewGuid(),
                type: InputType.Script,
                timestamp: timestamp,
                script: script,
                description: description);
        }

        /// <summary>
        /// Creates a variable assignment event
        /// </summary>
        public static MacroEvent CreateVariableEvent(string variableName, string variableValue, TimeSpan timestamp, string? description = null)
        {
            return new MacroEvent(
                id: Guid.NewGuid(),
                type: InputType.VariableSet,
                timestamp: timestamp,
                variableName: variableName,
                variableValue: variableValue,
                description: description);
        }

        /// <summary>
        /// Updates the enabled state of this event
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        /// <summary>
        /// Updates the description of this event
        /// </summary>
        public void SetDescription(string? description)
        {
            Description = description;
        }

        /// <summary>
        /// Updates the timestamp of this event
        /// </summary>
        public void SetTimestamp(TimeSpan timestamp)
        {
            Timestamp = timestamp;
        }

        /// <summary>
        /// Adds or updates metadata for this event
        /// </summary>
        public void SetMetadata(string key, object value)
        {
            Metadata[key] = value;
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
        /// Creates a deep copy of this macro event
        /// </summary>
        public MacroEvent Clone()
        {
            return new MacroEvent(
                id: Guid.NewGuid(), // New ID for the clone
                type: Type,
                timestamp: Timestamp,
                keyCode: KeyCode,
                mouseButton: MouseButton,
                x: X,
                y: Y,
                wheelDelta: WheelDelta,
                text: Text,
                script: Script,
                variableName: VariableName,
                variableValue: VariableValue,
                condition: Condition,
                delayMs: DelayMs,
                loopCount: LoopCount,
                metadata: new Dictionary<string, object>(Metadata),
                isEnabled: IsEnabled,
                description: Description);
        }

        public override string ToString()
        {
            return Type switch
            {
                InputType.KeyDown => $"KeyDown: {KeyCode} at {Timestamp.TotalMilliseconds}ms",
                InputType.KeyUp => $"KeyUp: {KeyCode} at {Timestamp.TotalMilliseconds}ms",
                InputType.MouseDown => $"MouseDown: Button {MouseButton} at ({X}, {Y}) at {Timestamp.TotalMilliseconds}ms",
                InputType.MouseUp => $"MouseUp: Button {MouseButton} at ({X}, {Y}) at {Timestamp.TotalMilliseconds}ms",
                InputType.MouseMove => $"MouseMove: to ({X}, {Y}) at {Timestamp.TotalMilliseconds}ms",
                InputType.MouseWheel => $"MouseWheel: {WheelDelta} at ({X}, {Y}) at {Timestamp.TotalMilliseconds}ms",
                InputType.Delay => $"Delay: {DelayMs}ms at {Timestamp.TotalMilliseconds}ms",
                InputType.TextInput => $"TextInput: '{Text}' at {Timestamp.TotalMilliseconds}ms",
                InputType.Script => $"Script: '{Script?.Substring(0, Math.Min(50, Script?.Length ?? 0))}...' at {Timestamp.TotalMilliseconds}ms",
                InputType.VariableSet => $"Variable: {VariableName} = {VariableValue} at {Timestamp.TotalMilliseconds}ms",
                _ => $"{Type} at {Timestamp.TotalMilliseconds}ms"
            };
        }
    }
}
